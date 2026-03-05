using Microsoft.AspNetCore.Mvc;
using RainfallThree.Models;
using System.Globalization;

public class StormController : Controller
{
    public IActionResult Index()
    {
        return View(new StormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(StormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Message = "Please correct the form errors.";
            return View(model);
        }

        if (model.UploadedFile == null || model.UploadedFile.Length == 0)
        {
            model.Message = "Please upload a valid CSV file.";
            return View(model);
        }

        var rainfallData = new List<RainfallRecord>();

        using (var reader = new StreamReader(model.UploadedFile.OpenReadStream()))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Extract station info from header lines
                if (line.StartsWith("#"))
                {
                    if (line.Contains("Station name"))
                    {
                        // Get text after '=' and trim whitespace
                        var stationValue = line.Split('=').Last().Trim();

                        // Remove any trailing commas
                        stationValue = stationValue.TrimEnd(',');

                        model.StationName = stationValue;
                    }

                    if (line.Contains("Latitude"))
                    {
                        var latPart = line.Split('=').Last().Trim().TrimEnd(',');
                        if (double.TryParse(latPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                            model.Latitude = lat;
                    }

                    if (line.Contains("Longitude"))
                    {
                        var lonPart = line.Split('=').Last().Trim().TrimEnd(',');
                        if (double.TryParse(lonPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                            model.Longitude = lon;
                    }

                    continue;
                }

                var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 3)
                    continue;

                if (DateTime.TryParse(parts[1].Trim(),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime time)
                    && double.TryParse(parts[2].Trim(),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double rainfall))
                {
                    rainfallData.Add(new RainfallRecord
                    {
                        Time = time,
                        Rainfall = rainfall
                    });
                }
            }
        }

        if (rainfallData.Count == 0)
        {
            model.Message = "No valid rainfall records found in file.";
            return View(model);
        }

        rainfallData = rainfallData.OrderBy(r => r.Time).ToList();

        // Find storms matching user-entered depth and duration
        var results = FindAllStorms(rainfallData, model.Depth, model.DurationMinutes);

        if (results.Count == 0)
        {
            model.Message = $"File loaded ({rainfallData.Count} records). No storm events found matching your criteria.";
        }
        else
        {
            model.Results = results;
            model.Message = $"File loaded ({rainfallData.Count} records).";
        }

        return View(model);
    }

    private List<StormResult> FindAllStorms(List<RainfallRecord> data, double targetDepth, int targetDurationMinutes)
    {
        var storms = new List<StormResult>();
        if (data == null || data.Count == 0)
            return storms;

        // 15% buffer rounded to 2 decimal places
        double minDepth = Math.Round(targetDepth * 0.85, 2);
        double maxDepth = Math.Round(targetDepth * 1.15, 2);

        for (int i = 0; i < data.Count; i++)
        {
            double cumulative = data[i].Rainfall;
            int totalMinutes = 0;

            var eventData = new List<RainfallRecord> { data[i] };

            for (int j = i + 1; j < data.Count; j++)
            {
                var diff = (data[j].Time - data[j - 1].Time).TotalMinutes;

                // gap is not exactly 5 minutes → stop this storm
                if (diff != 5)
                    break;

                totalMinutes += 5;
                cumulative += data[j].Rainfall;
                eventData.Add(data[j]);

                // Stop if duration exceeds target
                if (totalMinutes > targetDurationMinutes)
                    break;

                double roundedCumulative = Math.Round(cumulative, 2);

                // Duration must match EXACTLY
                if (totalMinutes == targetDurationMinutes &&
                    roundedCumulative >= minDepth &&
                    roundedCumulative <= maxDepth)
                {
                    storms.Add(new StormResult
                    {
                        StartTime = data[i].Time,
                        EndTime = data[j].Time,
                        TotalRainfall = roundedCumulative,
                        EventData = new List<RainfallRecord>(eventData)
                    });

                    break; // move to next starting point
                }
            }
        }

        return storms.OrderBy(s => s.StartTime).ToList();
    }
}