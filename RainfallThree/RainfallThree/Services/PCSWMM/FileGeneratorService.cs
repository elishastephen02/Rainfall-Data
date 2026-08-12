using RainfallThree.Models;
using RainfallThree.Models.PCSWMM;
using System.Globalization;

namespace RainfallThree.Services.PCSWMM
{
    public class FileGeneratorService
    {
        public void Generate(StormTemplate template, RainfallSheet rainfall, double rainfallDepth, string duration, string templateName, string outputFile)
        {
            using var writer = new StreamWriter(outputFile);

            writer.WriteLine(";;EPA SWMM Time Series Data");
            writer.WriteLine($";;{templateName.Replace("_", " ")} design storm");
            writer.WriteLine($";;Station: {rainfall.Index}");
            writer.WriteLine($";;Return Period: {rainfall.ReturnPeriod} year");
            writer.WriteLine($";;Total rainfall = {rainfallDepth:0.##} mm");
            writer.WriteLine($";;Rain interval = {duration} minutes");
            writer.WriteLine(";;Rain units = mm/hr");

            foreach (var point in template.Points)
            {
                double newValue = point.Value * rainfallDepth;

                writer.WriteLine(
                    $"\t{point.Time}\t{newValue.ToString("0.######", CultureInfo.InvariantCulture)}");
            }
        }
    }
}
