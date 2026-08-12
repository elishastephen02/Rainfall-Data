namespace RainfallThree.Models.PCSWMM
{
    public class ExportRequest
    {
        public string StormType { get; set; } = "";
        public List<RainfallSheet> Results { get; set; } = new();
        public List<string> SelectedDurations { get; set; } = new();
        public string OutputFolder { get; set; } = string.Empty;
        public string ModelFolderPath { get; set; } = string.Empty;
    }
}
