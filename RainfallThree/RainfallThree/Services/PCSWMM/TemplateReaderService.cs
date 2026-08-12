using RainfallThree.Models.PCSWMM;
using System.Globalization;

namespace RainfallThree.Services.PCSWMM
{
    public class TemplateReaderService
    {
        public StormTemplate Read(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            StormTemplate template = new StormTemplate();

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith(";;"))
                {
                    template.HeaderLines.Add(line);
                    continue;
                }

                var parts = line.Split(
                    (char[])null!,
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    continue;

                template.Points.Add(new TemplatePoint
                {
                    Time = parts[0],

                    Value = double.Parse(
                        parts[1],
                        CultureInfo.InvariantCulture)
                });
            }

            return template;
        }
    }
}
