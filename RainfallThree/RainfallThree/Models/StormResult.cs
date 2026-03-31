namespace RainfallThree.Models
{
    public class StormResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double TotalRainfall { get; set; }
        public List<RainfallRecord> EventData { get; set; }

        // midpoint of storm
        public DateTime MidTime => StartTime + TimeSpan.FromTicks((EndTime - StartTime).Ticks / 2);

        //return period
        public string ReturnPeriod { get; set; }
    }
}
