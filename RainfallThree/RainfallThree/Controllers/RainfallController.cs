using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RainfallThree.Data;
using RainfallThree.Models;
using System.Text.Json;
using System.IO.Compression;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
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
            .Select(r => new SelectListItem { Value = r.ToString(), Text = r + " Years" })
            .ToList();

        model.ReturnPeriodOptions.Insert(0, new SelectListItem { Value = "", Text = "All" });
        model.DurationOptions = BuildDurationOptions();

        return View(model);
    }

    [HttpPost]
    public IActionResult Search(SearchRainfallViewModel model)
    {
        // Reload dropdowns
        model.ReturnPeriodOptions = _context.RainfallSheets
            .Select(r => r.ReturnPeriod)
            .Distinct()
            .OrderBy(r => r)
            .Select(r => new SelectListItem { Value = r.ToString(), Text = r + " Years" })
            .ToList();

        model.ReturnPeriodOptions.Insert(0, new SelectListItem { Value = "", Text = "All" });
        model.DurationOptions = BuildDurationOptions();

        // -------------------------------------------------------
        // Two base datasets:
        //   allData      = NO ReturnPeriod filter
        //                  → summary table always shows all return periods
        //   filteredData = ReturnPeriod filter applied
        //                  → results table respects the user's selection
        // -------------------------------------------------------
        var allData = _context.RainfallSheets.ToList();

        var filteredDataQuery = _context.RainfallSheets.AsQueryable();
        if (model.ReturnPeriod.HasValue)
            filteredDataQuery = filteredDataQuery.Where(x => x.ReturnPeriod == model.ReturnPeriod);
        var filteredData = filteredDataQuery.ToList();

        // Handle uploaded area file
        if (model.AreaFile != null)
        {
            try { model.PolygonGeoJson = ConvertToGeoJson(model.AreaFile); }
            catch (Exception ex) { ModelState.AddModelError("", "Invalid file: " + ex.Message); }
        }

        // -------------------------------------------------------
        // Parse polygon once — shared by both results and summary
        // -------------------------------------------------------
        List<(double lat, double lon)> parsedPolygon = new();

        if (!string.IsNullOrEmpty(model.PolygonGeoJson))
        {
            var geo = JsonDocument.Parse(model.PolygonGeoJson);
            var root = geo.RootElement;

            if (root.TryGetProperty("features", out var features))
            {
                var geometry = features[0].GetProperty("geometry");
                if (geometry.GetProperty("type").GetString() != "Polygon")
                    throw new Exception("Only Polygon is supported");
                parsedPolygon = geometry.GetProperty("coordinates")[0]
                    .EnumerateArray()
                    .Select(c => (lat: c[1].GetDouble(), lon: c[0].GetDouble()))
                    .ToList();
            }
            else if (root.TryGetProperty("geometry", out var geometry))
            {
                parsedPolygon = geometry.GetProperty("coordinates")[0]
                    .EnumerateArray()
                    .Select(c => (lat: c[1].GetDouble(), lon: c[0].GetDouble()))
                    .ToList();
            }
            else if (root.GetProperty("type").GetString() == "Polygon")
            {
                parsedPolygon = root.GetProperty("coordinates")[0]
                    .EnumerateArray()
                    .Select(c => (lat: c[1].GetDouble(), lon: c[0].GetDouble()))
                    .ToList();
            }
            else
            {
                throw new Exception("Invalid GeoJSON format");
            }

            if (parsedPolygon.Count == 0)
                throw new Exception("Polygon has no coordinates");

            Console.WriteLine("Polygon loaded successfully:");
            foreach (var p in parsedPolygon.Take(5))
                Console.WriteLine($"Lat: {p.lat}, Lon: {p.lon}");
        }

        // -------------------------------------------------------
        // Results dataSet — respects ReturnPeriod + spatial filter
        // -------------------------------------------------------
        List<RainfallSheet> dataSet;

        if (parsedPolygon.Any())
        {
            dataSet = filteredData
                .Select(r => { var (lat, lon) = Normalize(r); return new { Row = r, Lat = lat, Lon = lon }; })
                .Where(x => IsPointInPolygon(x.Lat, x.Lon, parsedPolygon))
                .Select(x => x.Row)
                .ToList();
        }
        else if (model.LATDEG.HasValue && model.LONGDEG.HasValue)
        {
            dataSet = filteredData
                .Where(r =>
                    r.Latdeg == model.LATDEG &&
                    r.Latmin == (model.LATMIN ?? 0) &&
                    r.Longdeg == model.LONGDEG &&
                    r.Longmin == (model.LONGMIN ?? 0))
                .ToList();
        }
        else
        {
            dataSet = filteredData.Take(500).ToList();
        }

        // Filter by SelectedDuration (removes rows where that column is null)
        if (!string.IsNullOrEmpty(model.SelectedDuration))
        {
            dataSet = model.SelectedDuration switch
            {
                "5" => dataSet.Where(x => x._5Min.HasValue).ToList(),
                "10" => dataSet.Where(x => x._10Min.HasValue).ToList(),
                "15" => dataSet.Where(x => x._15Min.HasValue).ToList(),
                "30" => dataSet.Where(x => x._30Min.HasValue).ToList(),
                "60" => dataSet.Where(x => x._60Min.HasValue).ToList(),
                "120" => dataSet.Where(x => x._120Min.HasValue).ToList(),
                "1440" => dataSet.Where(x => x._1440Min.HasValue).ToList(),
                "4320" => dataSet.Where(x => x._4320Min.HasValue).ToList(),
                "10080" => dataSet.Where(x => x._10080Min.HasValue).ToList(),
                _ => dataSet
            };
        }

        model.Results = dataSet.Any() ? dataSet : new List<RainfallSheet>();

        model.TotalRainfall = dataSet.Any()
            ? dataSet.Sum(r =>
                (r._5Min ?? 0) + (r._10Min ?? 0) + (r._15Min ?? 0) +
                (r._30Min ?? 0) + (r._60Min ?? 0) + (r._120Min ?? 0) +
                (r._1440Min ?? 0) + (r._4320Min ?? 0) + (r._10080Min ?? 0))
            : 0;

        Console.WriteLine($"Total points in dataset: {dataSet.Count}");
        foreach (var group in dataSet.GroupBy(x => x.ReturnPeriod))
        {
            Console.WriteLine($"RP {group.Key} count: {group.Count()}");
            foreach (var item in group.Take(5))
                Console.WriteLine($"5min:{item._5Min} 10min:{item._10Min} 15min:{item._15Min} 30min:{item._30Min}");
        }

        // -------------------------------------------------------
        // Summary dataSet — NO ReturnPeriod filter, same spatial filter
        // All return periods always appear regardless of selection.
        // -------------------------------------------------------
        List<RainfallSheet> summarySet;

        if (parsedPolygon.Any())
        {
            summarySet = allData
                .Select(r => { var (lat, lon) = Normalize(r); return new { Row = r, Lat = lat, Lon = lon }; })
                .Where(x => IsPointInPolygon(x.Lat, x.Lon, parsedPolygon))
                .Select(x => x.Row)
                .ToList();
        }
        else if (model.LATDEG.HasValue && model.LONGDEG.HasValue)
        {
            summarySet = allData
                .Where(r =>
                    r.Latdeg == model.LATDEG &&
                    r.Latmin == (model.LATMIN ?? 0) &&
                    r.Longdeg == model.LONGDEG &&
                    r.Longmin == (model.LONGMIN ?? 0))
                .ToList();
        }
        else
        {
            summarySet = allData.Take(500).ToList();
        }

        model.Summary = summarySet
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

        // Preload map marker from first result if no coords entered
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
        if (string.IsNullOrEmpty(model.ResultsJson))
            return BadRequest("No data to download");

        var dataSet = JsonSerializer.Deserialize<List<RainfallSheet>>(
            model.ResultsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var builder = new System.Text.StringBuilder();

        if (string.IsNullOrEmpty(model.SelectedDuration))
            builder.AppendLine("Index,LatDeg,LatMin,LongDeg,LongMin,ReturnPeriod,5Min,10Min,15Min,30Min,60Min,120Min,1440Min,4320Min,10080Min,SourceSheet");
        else
            builder.AppendLine($"Index,LatDeg,LatMin,LongDeg,LongMin,ReturnPeriod,{model.SelectedDuration}Min,SourceSheet");

        foreach (var item in dataSet)
        {
            if (string.IsNullOrEmpty(model.SelectedDuration))
            {
                builder.AppendLine(
                    $"{item.Index},{item.Latdeg},{item.Latmin},{item.Longdeg},{item.Longmin},{item.ReturnPeriod}," +
                    $"{item._5Min?.ToString("0.00")},{item._10Min?.ToString("0.00")},{item._15Min?.ToString("0.00")}," +
                    $"{item._30Min?.ToString("0.00")},{item._60Min?.ToString("0.00")},{item._120Min?.ToString("0.00")}," +
                    $"{item._1440Min?.ToString("0.00")},{item._4320Min?.ToString("0.00")},{item._10080Min?.ToString("0.00")}," +
                    $"{item.SourceSheet}");
            }
            else
            {
                string durationValue = model.SelectedDuration switch
                {
                    "5" => item._5Min?.ToString("0.00"),
                    "10" => item._10Min?.ToString("0.00"),
                    "15" => item._15Min?.ToString("0.00"),
                    "30" => item._30Min?.ToString("0.00"),
                    "60" => item._60Min?.ToString("0.00"),
                    "120" => item._120Min?.ToString("0.00"),
                    "1440" => item._1440Min?.ToString("0.00"),
                    "4320" => item._4320Min?.ToString("0.00"),
                    "10080" => item._10080Min?.ToString("0.00"),
                    _ => ""
                };

                builder.AppendLine(
                    $"{item.Index},{item.Latdeg},{item.Latmin},{item.Longdeg},{item.Longmin}," +
                    $"{item.ReturnPeriod},{durationValue},{item.SourceSheet}");
            }
        }

        var fileName = $"RainfallResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", fileName);
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

            if (intersect) inside = !inside;
        }

        return inside;
    }

    private (double lat, double lon) Normalize(RainfallSheet r)
    {
        var lat = Math.Round(-(r.Latdeg + (r.Latmin / 60.0)), 6);
        var lon = Math.Round(r.Longdeg + (r.Longmin / 60.0), 6);
        return (lat, lon);
    }

    private List<SelectListItem> BuildDurationOptions() => new()
    {
        new SelectListItem { Value = "",      Text = "All" },
        new SelectListItem { Value = "5",     Text = "5 Min" },
        new SelectListItem { Value = "10",    Text = "10 Min" },
        new SelectListItem { Value = "15",    Text = "15 Min" },
        new SelectListItem { Value = "30",    Text = "30 Min" },
        new SelectListItem { Value = "60",    Text = "60 Min" },
        new SelectListItem { Value = "120",   Text = "120 Min" },
        new SelectListItem { Value = "1440",  Text = "1440 Min" },
        new SelectListItem { Value = "4320",  Text = "4320 Min" },
        new SelectListItem { Value = "10080", Text = "10080 Min" },
    };

    private string ConvertToGeoJson(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        using var stream = file.OpenReadStream();

        if (ext == ".geojson" || ext == ".json")
        {
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeProp))
            {
                var type = typeProp.GetString();
                if (type == "Feature" || type == "FeatureCollection" || type == "Polygon")
                    return text;
            }

            if (root.TryGetProperty("features", out var features))
            {
                var first = features[0];
                if (first.TryGetProperty("geometry", out var geom) &&
                    geom.TryGetProperty("rings", out var rings))
                {
                    var points = rings[0].EnumerateArray()
                        .Select(p =>
                        {
                            var (lat, lon) = ConvertToWGS84(p[0].GetDouble(), p[1].GetDouble());
                            return new[] { lon, lat };
                        })
                        .ToList();

                    return JsonSerializer.Serialize(new
                    {
                        type = "Feature",
                        geometry = new { type = "Polygon", coordinates = new[] { points } }
                    });
                }
            }

            throw new Exception("Unsupported JSON format (not GeoJSON or ESRI)");
        }

        if (ext == ".kml")
        {
            var doc = XDocument.Load(stream);
            XNamespace ns = "http://www.opengis.net/kml/2.2";
            var coords = doc.Descendants(ns + "coordinates").FirstOrDefault()?.Value
                ?? throw new Exception("Invalid KML file");

            var points = coords.Trim().Split(' ')
                .Where(x => x.Contains(","))
                .Select(x => { var p = x.Split(','); return new[] { double.Parse(p[0]), double.Parse(p[1]) }; })
                .ToList();

            return JsonSerializer.Serialize(new
            {
                type = "Feature",
                geometry = new { type = "Polygon", coordinates = new[] { points } }
            });
        }

        if (ext == ".kmz")
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var kmlEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".kml"))
                ?? throw new Exception("KMZ does not contain a KML file");

            using var kmlStream = kmlEntry.Open();
            var doc = XDocument.Load(kmlStream);
            XNamespace ns = "http://www.opengis.net/kml/2.2";
            var coords = doc.Descendants(ns + "coordinates").FirstOrDefault()?.Value
                ?? throw new Exception("Invalid KMZ/KML file");

            var points = coords.Trim().Split(' ')
                .Where(x => x.Contains(","))
                .Select(x => { var p = x.Split(','); return new[] { double.Parse(p[0]), double.Parse(p[1]) }; })
                .ToList();

            return JsonSerializer.Serialize(new
            {
                type = "Feature",
                geometry = new { type = "Polygon", coordinates = new[] { points } }
            });
        }

        throw new Exception("Unsupported file type");
    }

    [HttpPost]
    public IActionResult UploadAreaFile(IFormFile file)
    {
        if (file == null)
            return BadRequest("No file uploaded");

        return Content(ConvertToGeoJson(file), "application/json");
    }

    private (double lat, double lon) ConvertToWGS84(double x, double y)
    {
        var csFactory = new CoordinateSystemFactory();
        var ctFactory = new CoordinateTransformationFactory();

        var source = csFactory.CreateFromWkt(
            @"PROJCS[""South Africa WG31"",
        GEOGCS[""GCS_WGS_1984"",
        DATUM[""WGS_1984"",
        SPHEROID[""WGS_1984"",6378137,298.257223563]],
        PRIMEM[""Greenwich"",0],
        UNIT[""Degree"",0.0174532925199433]],
        PROJECTION[""Transverse_Mercator""],
        PARAMETER[""latitude_of_origin"",0],
        PARAMETER[""central_meridian"",31],
        PARAMETER[""scale_factor"",1],
        PARAMETER[""false_easting"",0],
        PARAMETER[""false_northing"",0],
        UNIT[""Meter"",1]]");

        var target = GeographicCoordinateSystem.WGS84;
        var transform = ctFactory.CreateFromCoordinateSystems(source, target);
        var result = transform.MathTransform.Transform(new[] { x, y });

        return (lat: result[1], lon: result[0]);
    }
}