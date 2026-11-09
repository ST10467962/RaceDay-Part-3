namespace RaceDayAPI.Models
{
    public class Entry
    {
        public int EntryId { get; set; } // Primary key
        public int ParticipantId { get; set; }
        public int CategoryId { get; set; }
        public DateTime EntryDate { get; set; }
        public bool PaymentStatus { get; set; }
        public string Status { get; set; } = "Pending";   // Pending, Confirmed or Cancelled

        // Relationships
        public Participant Participant { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
}