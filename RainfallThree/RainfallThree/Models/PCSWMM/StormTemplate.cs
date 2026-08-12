namespace RainfallThree.Models.PCSWMM
{
    public class StormTemplate
    {
        public List<string> HeaderLines { get; set; } = new();

        public List<TemplatePoint> Points { get; set; } = new();
    }
}
