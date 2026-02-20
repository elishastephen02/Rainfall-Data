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
        public int? ReturnPeriod { get; set; }
        public List<SelectListItem> ReturnPeriodOptions { get; set; } = new();

        // Dropdown selection
        public string? SelectedDuration { get; set; }

        // Dropdown options
        public List<SelectListItem> DurationOptions { get; set; } = new()
        {
            new SelectListItem { Value = "", Text = "All" },
            new SelectListItem { Value = "5", Text = "5 min" },
            new SelectListItem { Value = "10", Text = "10 min" },
            new SelectListItem { Value = "15", Text = "15 min" },
            new SelectListItem { Value = "30", Text = "30 min" },
            new SelectListItem { Value = "60", Text = "60 min" },
            new SelectListItem { Value = "120", Text = "120 min" },
            new SelectListItem { Value = "1440", Text = "1440 min (1 day)" },
            new SelectListItem { Value = "4320", Text = "4320 min (3 days)" },
            new SelectListItem { Value = "10080", Text = "10080 min (7 days)" }
        };

        public List<RainfallSheet>? Results { get; set; }
    }
}
