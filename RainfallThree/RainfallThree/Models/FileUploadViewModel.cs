using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RainfallThree.Models
{
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "Please upload a CSV file.")]
        public IFormFile UploadedFile { get; set; }

        public string? StationName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? Message { get; set; }
        public bool FileLoaded { get; set; }
    }
}