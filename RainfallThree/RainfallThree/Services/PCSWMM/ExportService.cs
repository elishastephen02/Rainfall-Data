using RainfallThree.Models;
using RainfallThree.Models.PCSWMM;
using System.Globalization;

namespace RainfallThree.Services.PCSWMM
{
    public class ExportService : ExportServiceI
    {
        private const string GeneratedFolderName = "Generated_SWMM_Storms";

        public async Task<string> GenerateAsync(ExportRequest request, IProgress<ExportProgress>? progress = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Results == null || !request.Results.Any())
            {
                throw new InvalidOperationException(
                    "No rainfall results were supplied.");
            }

            if (request.SelectedDurations == null ||
                !request.SelectedDurations.Any())
            {
                throw new InvalidOperationException(
                    "No rainfall durations were selected.");
            }

            if (string.IsNullOrWhiteSpace(request.StormType))
            {
                throw new InvalidOperationException(
                    "Storm type was not supplied.");
            }

            if (string.IsNullOrWhiteSpace(request.ModelFolderPath))
            {
                throw new InvalidOperationException(
                    "The PCSWMM model folder was not supplied.");
            }
            
            //Path entered by the user.
            string modelFolderPath = request.ModelFolderPath.Trim();

            modelFolderPath = modelFolderPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            //This is the location where the user will eventually
            string userGeneratedStormsPath = Path.Combine(
                modelFolderPath,
                GeneratedFolderName);

            List<string> templates = request.StormType switch
            {
                "Type_1" => new List<string>
                {
                    "South_Africa_SCS_Type_1"
                },

                "Type_2" => new List<string>
                {
                    "South_Africa_SCS_Type_2"
                },

                "Both" => new List<string>
                {
                    "South_Africa_SCS_Type_1",
                    "South_Africa_SCS_Type_2"
                },

                _ => throw new InvalidOperationException(
                    $"Unknown storm type: {request.StormType}")
            };

            string outputFolder = Path.Combine(
                AppContext.BaseDirectory,
                GeneratedFolderName);

            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            Directory.CreateDirectory(outputFolder);

            HashSet<string> selectedDurations =
                request.SelectedDurations
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToHashSet();

            string[] validDurations =
            {
                "5",
                "10",
                "15",
                "30",
                "60",
                "120",
                "1440",
                "4320",
                "10080"
            };

            foreach (string duration in selectedDurations)
            {
                if (!validDurations.Contains(duration))
                {
                    throw new InvalidOperationException(
                        $"Invalid rainfall duration: {duration} minutes.");
                }
            }

            int totalFiles = 0;

            foreach (RainfallSheet rainfall in request.Results)
            {
                foreach (string duration in selectedDurations)
                {
                    double? rainfallDepth =
                        GetRainfallDepth(
                            rainfall,
                            duration);

                    if (rainfallDepth.HasValue)
                    {
                        totalFiles += templates.Count;
                    }
                }
            }

            if (totalFiles == 0)
            {
                throw new InvalidOperationException(
                    "No files could be generated from the selected rainfall data.");
            }

            TemplateReaderService reader = new();

            FileGeneratorService generator = new();

            List<GeneratedRainfallFile> generatedFiles = new();

            int currentFile = 0;

            foreach (RainfallSheet rainfall in request.Results)
            {
                foreach (string templateName in templates)
                {
                    string templatePath = Path.Combine(
                        AppContext.BaseDirectory,
                        "Templates",
                        $"{templateName}.dat");

                    if (!File.Exists(templatePath))
                    {
                        throw new FileNotFoundException(
                            $"PCSWMM template was not found: {templatePath}");
                    }

                    StormTemplate template =
                        reader.Read(templatePath);

                    foreach (string duration in selectedDurations)
                    {
                        double? rainfallDepth =
                            GetRainfallDepth(
                                rainfall,
                                duration);

                        if (!rainfallDepth.HasValue)
                        {
                            continue;
                        }

                        string stormTypeName =
                            GetShortStormTypeName(templateName);

                        string rainfallText =
                            rainfallDepth.Value.ToString(
                                "0.##",
                                CultureInfo.InvariantCulture);

                        string fileName =
                            $"{rainfall.Index}_" +
                            $"{stormTypeName}_" +
                            $"{rainfallText}mm_" +
                            $"{rainfall.ReturnPeriod}yr.dat";

                        string outputPath = Path.Combine(
                            outputFolder,
                            fileName);

                        generator.Generate(
                            template,
                            rainfall,
                            rainfallDepth.Value,
                            duration,
                            stormTypeName,
                            outputPath);

                        generatedFiles.Add(
                            new GeneratedRainfallFile
                            {
                                FileName = fileName,

                                FilePath = outputPath,

                                Name =
                                    Path.GetFileNameWithoutExtension(
                                        fileName)
                            });

                        currentFile++;

                        progress?.Report(
                            new ExportProgress
                            {
                                TotalFiles = totalFiles,

                                FilesCompleted = currentFile,

                                Percentage =
                                    (int)Math.Round(
                                        (double)currentFile /
                                        totalFiles *
                                        100),

                                CurrentFile = fileName
                            });
                    }
                }
            }

            //Create: SWMM_Raingages_List.txt
            CreateRainGagesList(
                outputFolder,
                generatedFiles);

            //Create: SWMM_Timeseries_List.txt
            CreateTimeSeriesList(
                outputFolder,
                generatedFiles,
                userGeneratedStormsPath);

            await Task.CompletedTask;

            //Return the temporary server-side folder.
            return outputFolder;
        }

        private double? GetRainfallDepth(
            RainfallSheet rainfall,
            string duration)
        {
            return duration switch
            {
                "5" => rainfall._5Min,

                "10" => rainfall._10Min,

                "15" => rainfall._15Min,

                "30" => rainfall._30Min,

                "60" => rainfall._60Min,

                "120" => rainfall._120Min,

                "1440" => rainfall._1440Min,

                "4320" => rainfall._4320Min,

                "10080" => rainfall._10080Min,

                _ => null
            };
        }

        private string GetShortStormTypeName(
            string templateName)
        {
            return templateName switch
            {
                "South_Africa_SCS_Type_1" =>
                    "SCS_Type1",

                "South_Africa_SCS_Type_2" =>
                    "SCS_Type2",

                _ => throw new InvalidOperationException(
                    $"Unknown template name: {templateName}")
            };
        }

        private void CreateRainGagesList(string outputFolder,List<GeneratedRainfallFile> generatedFiles)
        {
            string outputPath = Path.Combine(
                outputFolder,
                "SWMM_Raingages_List.txt");

            using StreamWriter writer =
                new StreamWriter(outputPath);

            foreach (GeneratedRainfallFile file in generatedFiles)
            {
                writer.WriteLine(
                    $"{file.Name} " +
                    $"INTENSITY 0:05 1.0 TIMESERIES " +
                    $"{file.Name}");
            }
        }

        private void CreateTimeSeriesList(string outputFolder, List<GeneratedRainfallFile> generatedFiles, string userGeneratedStormsPath)
        {
            string outputPath = Path.Combine(
                outputFolder,
                "SWMM_Timeseries_List.txt");

            using StreamWriter writer =
                new StreamWriter(outputPath);

            foreach (GeneratedRainfallFile file in generatedFiles)
            {
                // path on users pc
                string userFilePath = Path.Combine(
                    userGeneratedStormsPath,
                    file.FileName);

                writer.WriteLine(
                    $"{file.Name} FILE \"{userFilePath}\"");
                writer.WriteLine();
                writer.WriteLine();
            }
        }

        private class GeneratedRainfallFile
        {
            public string FileName { get; set; } =
                string.Empty;

            public string FilePath { get; set; } =
                string.Empty;

            public string Name { get; set; } =
                string.Empty;
        }
    }
}