using Microsoft.AspNetCore.Mvc.Rendering;

namespace RainfallThree.Models
{
    public class SearchRainfallViewModel
    {
        // Enterable fields
        public int? Index { get; set; }
        public int? LATDEG { get; set; }
        public int? LATMIN { get; set; }
        public int? LONGDEG { get; set; }
        public int? LONGMIN { get; set; }
        public List<int> SelectedReturnPeriods { get; set; } = new();

        public List<SelectListItem> ReturnPeriodOptions { get; set; } = new();

        // Dropdown selection
        public List<string> SelectedDurations { get; set; } = new();

        // Dropdown options
        public List<SelectListItem> DurationOptions { get; set; } = new()
        {
            new SelectListItem { Value = "", Text = "All" },
            new SelectListItem { Value = "5", Text = "5 Min" },
            new SelectListItem { Value = "10", Text = "10 Min" },
            new SelectListItem { Value = "15", Text = "15 Min" },
            new SelectListItem { Value = "30", Text = "30 Min" },
            new SelectListItem { Value = "60", Text = "1 Hour" },
            new SelectListItem { Value = "120", Text = "2 Hours" },
            new SelectListItem { Value = "1440", Text = "1 day" },
            new SelectListItem { Value = "4320", Text = "3 days" },
            new SelectListItem { Value = "10080", Text = "7 days" }
        };

        public string? Address { get; set; }
        public double? Lat { get; set; }
        public double? Long { get; set; }

        public List<RainfallSheet>? Results { get; set; }

        public string? PolygonGeoJson { get; set; }
        public IFormFile AreaFile { get; set; }
        public List<RainfallSummaryViewModel>? Summary { get; set; }

        public double? TotalRainfall { get; set; }

        public string ResultsJson { get; set; }
    }
}
