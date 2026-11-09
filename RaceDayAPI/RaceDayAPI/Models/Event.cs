namespace RaceDayAPI.Models
{
    public class Event
    {
        public int EventId { get; set; } // Primary Key
        public int OrganiserId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public DateTime CreatedAt { get; set; }

        // Part 3 additions
        public decimal Distance { get; set; }                  // headline distance in km
        public string EventType { get; set; } = string.Empty;  // e.g. "Road Run", "Trail Run"
        public string? BannerImageName { get; set; }           // blob name in Azure Blob Storage

        // Relationships
        public User Organiser { get; set; } = null!;  // FK to User
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<RaceRoute> Routes { get; set; } = new List<RaceRoute>();
    }
}