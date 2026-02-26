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

                if (line.StartsWith("#"))
                    continue;

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

        // Optional: downsample each storm if too many points (>50)
        foreach (var storm in results)
        {
            if (storm.EventData.Count > 50)
            {
                int step = storm.EventData.Count / 50;
                storm.EventData = storm.EventData
                    .Where((x, idx) => idx % step == 0)
                    .ToList();
            }
        }

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

        int left = 0;
        double cumulative = 0;

        for (int right = 0; right < data.Count; right++)
        {
            cumulative += data[right].Rainfall;

            while (left < right &&
                  (data[right].Time - data[left].Time).TotalMinutes > targetDurationMinutes)
            {
                cumulative -= data[left].Rainfall;
                left++;
            }

            double duration = (data[right].Time - data[left].Time).TotalMinutes;

            if (Math.Abs(duration - targetDurationMinutes) <= 1 &&
                Math.Abs(cumulative - targetDepth) < 0.01)
            {
                var eventData = data.Skip(left).Take(right - left + 1).ToList();

                storms.Add(new StormResult
                {
                    StartTime = data[left].Time,
                    EndTime = data[right].Time,
                    TotalRainfall = cumulative,
                    EventData = eventData
                });
            }
        }

        return storms
            .OrderBy(s => s.StartTime)
            .ToList();
    }
}