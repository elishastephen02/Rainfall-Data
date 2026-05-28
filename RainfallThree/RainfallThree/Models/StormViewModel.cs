using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProjNet.CoordinateSystems;
using System.ComponentModel.DataAnnotations;
using System.Reflection.PortableExecutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RainfallThree.Models
{
    public class StormViewModel
    {
        [Required]
        public IFormFile UploadedFile { get; set; }

        [Required]
        public double Depth { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        public List<StormResult> Results { get; set; } = new();

        public string? StationName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int TotalDays { get; set; }

        public string? Message { get; set; }

        public List<DailyRainfallPoint> DailyTotals { get; set; } = new();
    }
    public class DailyRainfallPoint
    {
        public string? Date { get; set; }   // "yyyy-MM-dd"
        public double Total { get; set; }
    }
}