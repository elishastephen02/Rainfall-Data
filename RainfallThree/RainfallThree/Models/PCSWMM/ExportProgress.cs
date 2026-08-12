namespace RainfallThree.Models.PCSWMM
{
    public class ExportProgress
    {
        public int TotalFiles { get; set; }

        public int FilesCompleted { get; set; }

        public int Percentage { get; set; }

        public string CurrentFile { get; set; } = "";
    }
}
