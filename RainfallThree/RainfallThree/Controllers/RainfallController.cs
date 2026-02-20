using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RainfallThree.Data;
using RainfallThree.Models;

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
        // Reload dropdown
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

        // Get results (limit to 500)
        model.Results = query.Take(500).ToList();

        // ✅ Only preload marker if user didn't enter any coordinates
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

        // ===== HEADER =====
        if (string.IsNullOrEmpty(model.SelectedDuration))
        {
            builder.AppendLine("Index,LatDeg,LatMin,LongDeg,LongMin,ReturnPeriod,5Min,10Min,15Min,30Min,60Min,120Min,1440Min,4320Min,10080Min,SourceSheet");
        }
        else
        {
            builder.AppendLine($"Index,LatDeg,LatMin,LongDeg,LongMin,ReturnPeriod,{model.SelectedDuration}Min,SourceSheet");
        }

     // ===== DATA =====
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

}
