namespace RaceDayAPI.Models
{
    public class RaceRoute
    {
        public int RaceRouteId { get; set; } // Primary key 
        public int EventId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public string TerrainType { get; set; } = string.Empty;

        // Relationships
        public Event Event { get; set; } = null!;
    }
}