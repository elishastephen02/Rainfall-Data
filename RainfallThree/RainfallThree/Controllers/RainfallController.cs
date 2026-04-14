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

        // default results
        model.Results = rawData;

        //area data -> summary table
        var allData = _context.RainfallSheets.ToList();
        List<RainfallSheet> dataSet = new();

        if (model.AreaFile != null)
        {
            try
            {
                model.PolygonGeoJson = ConvertToGeoJson(model.AreaFile);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Invalid file: " + ex.Message);
            }
        }

        // polygon
        if (!string.IsNullOrEmpty(model.PolygonGeoJson))
        {
            var geo = JsonDocument.Parse(model.PolygonGeoJson);
            var root = geo.RootElement;

            List<(double lat, double lon)> polygon = new();

            // FeatureCollection
            if (root.TryGetProperty("features", out var features))
            {
                var firstFeature = features[0];
                var geometry = firstFeature.GetProperty("geometry");

                if (geometry.GetProperty("type").GetString() != "Polygon")
                    throw new Exception("Only Polygon is supported");

                var coords = geometry.GetProperty("coordinates")[0];

                polygon = coords.EnumerateArray()
                    .Select(c => (
                        lat: c[1].GetDouble(),
                        lon: c[0].GetDouble()
                    ))
                    .ToList();
            }

            // Single Feature
            else if (root.TryGetProperty("geometry", out var geometry))
            {
                var coords = geometry.GetProperty("coordinates")[0];

                polygon = coords.EnumerateArray()
                    .Select(c => (
                        lat: c[1].GetDouble(),
                        lon: c[0].GetDouble()
                    ))
                    .ToList();
            }

            //Raw Polygon
            else if (root.GetProperty("type").GetString() == "Polygon")
            {
                var coords = root.GetProperty("coordinates")[0];

                polygon = coords.EnumerateArray()
                    .Select(c => (
                        lat: c[1].GetDouble(),
                        lon: c[0].GetDouble()
                    ))
                    .ToList();
            }
            else
            {
                throw new Exception("Invalid GeoJSON format");
            }

            if (polygon.Count == 0)
                throw new Exception("Polygon has no coordinates");

            Console.WriteLine("Polygon loaded successfully:");
            foreach (var p in polygon.Take(5))
                Console.WriteLine($"Lat: {p.lat}, Lon: {p.lon}");

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
        }
        //point, address or manual input
        else if (model.LATDEG.HasValue && model.LONGDEG.HasValue)
        {
            var lat = model.LATDEG.Value + (model.LATMIN ?? 0) / 60.0;
            var lon = model.LONGDEG.Value + (model.LONGMIN ?? 0) / 60.0;

            dataSet = allData
                .Where(r =>
                    r.Latdeg == model.LATDEG &&
                    r.Latmin == (model.LATMIN ?? 0) &&
                    r.Longdeg == model.LONGDEG &&
                    r.Longmin == (model.LONGMIN ?? 0)
                )
                .ToList();
        }
        //default
        else
        {
            dataSet = allData.Take(500).ToList();
        }

        if(dataSet != null && dataSet.Any())
        {
            model.Results = dataSet;
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

    // conversion
    private string ConvertToGeoJson(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();

        using var stream = file.OpenReadStream();

        //geojson
        // geojson OR esri json
        if (ext == ".geojson" || ext == ".json")
        {
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();

            var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // ✅ CASE 1: Valid GeoJSON (already correct)
            if (root.TryGetProperty("type", out var typeProp))
            {
                var type = typeProp.GetString();

                if (type == "Feature" || type == "FeatureCollection" || type == "Polygon")
                {
                    return text;
                }
            }

            // ✅ CASE 2: ESRI JSON (YOUR FILE)
            if (root.TryGetProperty("features", out var features))
            {
                var first = features[0];

                if (first.TryGetProperty("geometry", out var geom) &&
                    geom.TryGetProperty("rings", out var rings))
                {
                    var points = rings[0]
                        .EnumerateArray()
                        .Select(p =>
                        {
                            var x = p[0].GetDouble();
                            var y = p[1].GetDouble();

                            var (lat, lon) = ConvertToWGS84(x, y);

                            return new[] { lon, lat }; // GeoJSON = [lon, lat]
                        })
                        .ToList();

                    var geoJson = new
                    {
                        type = "Feature",
                        geometry = new
                        {
                            type = "Polygon",
                            coordinates = new[] { points }
                        }
                    };

                    return JsonSerializer.Serialize(geoJson);
                }
            }

            throw new Exception("Unsupported JSON format (not GeoJSON or ESRI)");
        }

        //kml -> geojson
        if (ext == ".kml")
        {
            var doc = XDocument.Load(stream);

            XNamespace ns = "http://www.opengis.net/kml/2.2";

            var coords = doc.Descendants(ns + "coordinates")
                .FirstOrDefault()?.Value;

            if (coords == null)
                throw new Exception("Invalid KML file");

            var points = coords.Trim()
                .Split(' ')
                .Where(x => x.Contains(","))
                .Select(x =>
                {
                    var parts = x.Split(',');
                    return new[] { double.Parse(parts[0]), double.Parse(parts[1]) };
                })
                .ToList();

            var geoJson = new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Polygon",
                    coordinates = new[] { points }
                }
            };

            return JsonSerializer.Serialize(geoJson);
        }

        //kmz -> convert kml -> parse
        if (ext == ".kmz")
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var kmlEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".kml"));

            if (kmlEntry == null)
                throw new Exception("KMZ does not contain a KML file");

            using var kmlStream = kmlEntry.Open();
            var doc = XDocument.Load(kmlStream);

            XNamespace ns = "http://www.opengis.net/kml/2.2";

            var coords = doc.Descendants(ns + "coordinates")
                .FirstOrDefault()?.Value;

            if (coords == null)
                throw new Exception("Invalid KMZ/KML file");

            var points = coords.Trim()
                .Split(' ')
                .Where(x => x.Contains(","))
                .Select(x =>
                {
                    var parts = x.Split(',');
                    return new[] { double.Parse(parts[0]), double.Parse(parts[1]) };
                })
                .ToList();

            var geoJson = new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Polygon",
                    coordinates = new[] { points }
                }
            };

            return JsonSerializer.Serialize(geoJson);
        }

        throw new Exception("Unsupported file type");
    }

    [HttpPost]
    public IActionResult UploadAreaFile(IFormFile file)
    {
        if (file == null)
            return BadRequest("No file uploaded");

        var geoJson = ConvertToGeoJson(file);

        return Content(geoJson, "application/json");
    }
    private (double lat, double lon) ConvertToWGS84(double x, double y)
    {
        // Source: South Africa WG31 (approx)
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
        UNIT[""Meter"",1]]"
        );

        var target = GeographicCoordinateSystem.WGS84;

        var transform = ctFactory.CreateFromCoordinateSystems(source, target);

        var result = transform.MathTransform.Transform(new[] { x, y });

        return (lat: result[1], lon: result[0]);
    }
}