using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RainfallThree.Data;
using RainfallThree.Models;
using System.Text.Json;
using static RainfallThree.Models.RainfallSummaryViewModel;

public class RainfallController : Controller
{
    private readonly ApplicationDbContext _context;

    public RainfallController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Search()
    {
        var model = new SearchRainfallViewModel();

        model.ReturnPeriodOptions = _context.RainfallSheets
            .Select(r => r.ReturnPeriod)
            .Distinct()
            .OrderBy(r => r)
            .Select(r => new SelectListItem
            {
                Value = r.ToString(),
                Text = r + " Years"
            })
            .ToList();

        //Add "All"
        model.ReturnPeriodOptions.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "All"
        });

        return View(model);

    }

    [HttpPost]
    public IActionResult Search(SearchRainfallViewModel model)
    {
        //reload dropdown
        model.ReturnPeriodOptions = _context.RainfallSheets
            .Select(r => r.ReturnPeriod)
            .Distinct()
            .OrderBy(r => r)
            .Select(r => new SelectListItem
            {
                Value = r.ToString(),
                Text = r + " Years"
            })
            .ToList();

        model.ReturnPeriodOptions.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "All"
        });

        var query = _context.RainfallSheets.AsQueryable();

        // Apply filters only if user has entered a value
        if (model.Index.HasValue)
            query = query.Where(x => x.Index == model.Index);

        if (model.LATDEG.HasValue)
            query = query.Where(x => x.Latdeg == model.LATDEG);

        if (model.LATMIN.HasValue)
            query = query.Where(x => x.Latmin == model.LATMIN);

        if (model.LONGDEG.HasValue)
            query = query.Where(x => x.Longdeg == model.LONGDEG);

        if (model.LONGMIN.HasValue)
            query = query.Where(x => x.Longmin == model.LONGMIN);

        if (model.ReturnPeriod.HasValue)
            query = query.Where(x => x.ReturnPeriod == model.ReturnPeriod);

        //results table
        var rawData = query.Take(500).ToList();
        model.Results = rawData;

        //area data -> summary table
        var allData = _context.RainfallSheets.ToList();
        List<RainfallSheet> dataSet = new();

        // polygon
        if (!string.IsNullOrEmpty(model.PolygonGeoJson))
        {
            var geo = JsonDocument.Parse(model.PolygonGeoJson);
            var root = geo.RootElement;

            JsonElement coords;

            if (root.TryGetProperty("geometry", out var geometry))
            {
                coords = geometry.GetProperty("coordinates")[0];
            }
            else if (root.TryGetProperty("features", out var features))
            {
                coords = features[0]
                    .GetProperty("geometry")
                    .GetProperty("coordinates")[0];
            }
            else
            {
                throw new Exception("Invalid GeoJSON");
            }

            var polygon = coords.EnumerateArray()
                .Select(c => (
                    lat: c[1].GetDouble(),
                    lon: c[0].GetDouble()
                ))
                .ToList();

            var minLat = polygon.Min(p => p.lat);
            var maxLat = polygon.Max(p => p.lat);
            var minLon = polygon.Min(p => p.lon);
            var maxLon = polygon.Max(p => p.lon);

            dataSet = allData
                .Select(r =>
                {
                    var (lat, lon) = Normalize(r);

                    return new
                    {
                        Row = r,
                        Lat = lat,
                        Lon = lon
                    };
                })
                .Where(x => IsPointInPolygon(x.Lat, x.Lon, polygon))
                .Select(x => x.Row)
                .ToList();

            Console.WriteLine("Polygon Points:");
            foreach (var p in polygon)
            {
                Console.WriteLine($"Lat: {p.lat}, Lon: {p.lon}");
            }

            Console.WriteLine("Matched DB Points:");
            foreach (var r in dataSet.Take(10))
            {
                var lat = r.Latdeg + (r.Latmin / 60.0);
                var lon = r.Longdeg + (r.Longmin / 60.0);

                Console.WriteLine($"DB -> Lat: {lat}, Lon: {lon}, RP: {r.ReturnPeriod}");
            }
        }
        //point, address or manual input
        else if (model.LATDEG.HasValue && model.LONGDEG.HasValue)
        {
            var lat = model.LATDEG.Value + (model.LATMIN ?? 0) / 60.0;
            var lon = model.LONGDEG.Value + (model.LONGMIN ?? 0) / 60.0;

            dataSet = allData
                .OrderBy(r =>
                    Math.Pow(((r.Latdeg + r.Latmin / 60.0) - lat), 2) +
                    Math.Pow((r.Longdeg + r.Longmin / 60.0 - lon), 2)
                )
                .Take(50)
                .ToList();
        }
        //default
        else
        {
            dataSet = allData.Take(500).ToList();
        }

        // total rainfall for area
        model.TotalRainfall = dataSet.Any()
            ? dataSet.Sum(r =>
                (r._5Min ?? 0) +
                (r._10Min ?? 0) +
                (r._15Min ?? 0) +
                (r._30Min ?? 0) +
                (r._60Min ?? 0) +
                (r._120Min ?? 0) +
                (r._1440Min ?? 0) +
                (r._4320Min ?? 0) +
                (r._10080Min ?? 0)
            )
            : 0;

        Console.WriteLine($"Total points in polygon: {dataSet.Count}");

        foreach (var group in dataSet.GroupBy(x => x.ReturnPeriod))
        {
            Console.WriteLine($"RP {group.Key} count: {group.Count()}");

            foreach (var item in group.Take(5))
            {
                Console.WriteLine($"5min: {item._5Min}");
                Console.WriteLine($"10min: {item._10Min}");
                Console.WriteLine($"15min: {item._15Min}");
                Console.WriteLine($"30min: {item._30Min}");
                Console.WriteLine($"60min: {item._60Min}");
                Console.WriteLine($"120min: {item._120Min}");
                Console.WriteLine($"1440min: {item._1440Min}");
                Console.WriteLine($"4320min: {item._4320Min}");
                Console.WriteLine($"10080min: {item._10080Min}");
            }

        }

        // summary table based on area
        model.Summary = dataSet
         .GroupBy(r => r.ReturnPeriod)
         .Select(g => new RainfallSummaryViewModel
         {
             ReturnPeriod = g.Key,

             Min5 = g.Min(x => x._5Min ?? 0),
             Max5 = g.Max(x => x._5Min ?? 0),
             Avg5 = g.Average(x => x._5Min ?? 0),

             Min10 = g.Min(x => x._10Min ?? 0),
             Max10 = g.Max(x => x._10Min ?? 0),
             Avg10 = g.Average(x => x._10Min ?? 0),

             Min15 = g.Min(x => x._15Min ?? 0),
             Max15 = g.Max(x => x._15Min ?? 0),
             Avg15 = g.Average(x => x._15Min ?? 0),

             Min30 = g.Min(x => x._30Min ?? 0),
             Max30 = g.Max(x => x._30Min ?? 0),
             Avg30 = g.Average(x => x._30Min ?? 0),

             Min60 = g.Min(x => x._60Min ?? 0),
             Max60 = g.Max(x => x._60Min ?? 0),
             Avg60 = g.Average(x => x._60Min ?? 0),

             Min120 = g.Min(x => x._120Min ?? 0),
             Max120 = g.Max(x => x._120Min ?? 0),
             Avg120 = g.Average(x => x._120Min ?? 0),

             Min1440 = g.Min(x => x._1440Min ?? 0),
             Max1440 = g.Max(x => x._1440Min ?? 0),
             Avg1440 = g.Average(x => x._1440Min ?? 0),

             Min4320 = g.Min(x => x._4320Min ?? 0),
             Max4320 = g.Max(x => x._4320Min ?? 0),
             Avg4320 = g.Average(x => x._4320Min ?? 0),

             Min10080 = g.Min(x => x._10080Min ?? 0),
             Max10080 = g.Max(x => x._10080Min ?? 0),
             Avg10080 = g.Average(x => x._10080Min ?? 0),
         })
         .OrderBy(x => x.ReturnPeriod)
         .ToList();

        // preload map
        if ((!model.LATDEG.HasValue || !model.LONGDEG.HasValue) && model.Results.Any())
        {
            var first = model.Results.First();
            model.LATDEG ??= first.Latdeg;
            model.LATMIN ??= first.Latmin;
            model.LONGDEG ??= first.Longdeg;
            model.LONGMIN ??= first.Longmin;
        }

        return View(model);
    }

    [HttpPost]
    public IActionResult Download(SearchRainfallViewModel model)
    {
        var query = _context.RainfallSheets.AsQueryable();

        if (model.Index.HasValue)
            query = query.Where(x => x.Index == model.Index);

        if (model.LATDEG.HasValue)
            query = query.Where(x => x.Latdeg == model.LATDEG);

        if (model.LATMIN.HasValue)
            query = query.Where(x => x.Latmin == model.LATMIN);

        if (model.LONGDEG.HasValue)
            query = query.Where(x => x.Longdeg == model.LONGDEG);

        if (model.LONGMIN.HasValue)
            query = query.Where(x => x.Longmin == model.LONGMIN);

        if (model.ReturnPeriod.HasValue)
        {
            query = query.Where(x => x.ReturnPeriod == model.ReturnPeriod.Value);
        }

        var results = query.Take(500).ToList();

        var builder = new System.Text.StringBuilder();

        // HEADER
        if (string.IsNullOrEmpty(model.SelectedDuration))
        {
            builder.AppendLine("Index,LatDeg,LatMin,LongDeg,LongMin,ReturnPeriod,5Min,10Min,15Min,30Min,60Min,120Min,1440Min,4320Min,10080Min,SourceSheet");
        }
        else
        {
            builder.AppendLine($"Index,LatDeg,LatMin,LongDeg,LongMin,ReturnPeriod,{model.SelectedDuration}Min,SourceSheet");
        }

        // DATA 
        foreach (var item in results)
        {
            if (string.IsNullOrEmpty(model.SelectedDuration))
            {
                builder.AppendLine(
                    $"{item.Index}," +
                    $"{item.Latdeg}," +
                    $"{item.Latmin}," +
                    $"{item.Longdeg}," +
                    $"{item.Longmin}," +
                    $"{item.ReturnPeriod}," +
                    $"{item._5Min?.ToString("0.00")}," +
                    $"{item._10Min?.ToString("0.00")}," +
                    $"{item._15Min?.ToString("0.00")}," +
                    $"{item._30Min?.ToString("0.00")}," +
                    $"{item._60Min?.ToString("0.00")}," +
                    $"{item._120Min?.ToString("0.00")}," +
                    $"{item._1440Min?.ToString("0.00")}," +
                    $"{item._4320Min?.ToString("0.00")}," +
                    $"{item._10080Min?.ToString("0.00")}," +
                    $"{item.SourceSheet}"
                );
            }
            else
            {
                string durationValue = "";

                if (model.SelectedDuration == "5") durationValue = item._5Min?.ToString("0.00");
                else if (model.SelectedDuration == "10") durationValue = item._10Min?.ToString("0.00");
                else if (model.SelectedDuration == "15") durationValue = item._15Min?.ToString("0.00");
                else if (model.SelectedDuration == "30") durationValue = item._30Min?.ToString("0.00");
                else if (model.SelectedDuration == "60") durationValue = item._60Min?.ToString("0.00");
                else if (model.SelectedDuration == "120") durationValue = item._120Min?.ToString("0.00");
                else if (model.SelectedDuration == "1440") durationValue = item._1440Min?.ToString("0.00");
                else if (model.SelectedDuration == "4320") durationValue = item._4320Min?.ToString("0.00");
                else if (model.SelectedDuration == "10080") durationValue = item._10080Min?.ToString("0.00");

                builder.AppendLine(
                    $"{item.Index}," +
                    $"{item.Latdeg}," +
                    $"{item.Latmin}," +
                    $"{item.Longdeg}," +
                    $"{item.Longmin}," +
                    $"{item.ReturnPeriod}," +
                    $"{durationValue}," +
                    $"{item.SourceSheet}"
                );
            }
        }

        var fileName = $"RainfallResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(
            System.Text.Encoding.UTF8.GetBytes(builder.ToString()),
            "text/csv",
            fileName
        );
    }

    private bool IsPointInPolygon(double lat, double lon, List<(double lat, double lon)> polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var xi = polygon[i].lon;
            var yi = polygon[i].lat;
            var xj = polygon[j].lon;
            var yj = polygon[j].lat;

            bool intersect = ((yi > lat) != (yj > lat)) &&
                             (lon < (xj - xi) * (lat - yi) / (yj - yi + 0.0000001) + xi);

            if (intersect)
                inside = !inside;
        }

        return inside;
    }

    private (double lat, double lon) Normalize(RainfallSheet r)
    {
        var lat = -(r.Latdeg + (r.Latmin / 60.0));
        var lon = r.Longdeg + (r.Longmin / 60.0);

        // round to remove floating noise
        lat = Math.Round(lat, 6);
        lon = Math.Round(lon, 6);

        return (lat, lon);
    }


}