using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

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

        public string? Message { get; set; }
    }
}
