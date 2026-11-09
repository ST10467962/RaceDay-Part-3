namespace RaceDayAPI.Models
{
    public class Result
    {
        public int ResultId { get; set; } // Primary key
        public int EntryId { get; set; }
        public int EventId { get; set; }
        public int ParticipantId { get; set; }
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
        public DateTime RecordedAt { get; set; }

        // Relationships
        public Entry Entry { get; set; } = null!;
        public Event Event { get; set; } = null!;
        public Participant Participant { get; set; } = null!;
    }
}