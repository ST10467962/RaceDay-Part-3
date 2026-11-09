using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models.DTOs
{
    // ---------- Events ----------

    /// <summary>Body used by the MVC app to create or update an event.</summary>
    public class EventRequest
    {
        [Required, StringLength(150)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required, StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        [Range(0.1, 1000)]
        public decimal Distance { get; set; }

        [Required, StringLength(50)]
        public string EventType { get; set; } = string.Empty;
    }

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public decimal EntryFee { get; set; }
        public string AgeGroup { get; set; } = string.Empty;
        public int EnrolmentCount { get; set; }
    }

    public class EventDto
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int EnrolmentCount { get; set; }
        public int PendingCount { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();

        // Server-computed flags. The MVC renders these and does not re-derive them.
        public bool IsUpcoming { get; set; }      // event date is today or later
        public bool HasTakenPlace { get; set; }   // event date is today or earlier; results may be captured
        public bool IsOwner { get; set; }         // the signed-in user organises this event

        // Set only by GET api/Events/{id} for a signed-in participant.
        public bool CanEnrol { get; set; }        // upcoming, has categories, and no active enrolment of theirs
        public EntryDto? MyEntry { get; set; }    // the participant's active (not cancelled) enrolment, if any
    }

    // ---------- Entries (enrolments) ----------

    public class EnrolRequest
    {
        [Required]
        public int CategoryId { get; set; }
    }

    public class EntryStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;   // Pending, Confirmed, Cancelled
    }

    public class EntryDto
    {
        public int EntryId { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string? ParticipantEmail { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventLocation { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public bool PaymentStatus { get; set; }
        public string Status { get; set; } = string.Empty;

        /// <summary>True when the enrolment is not already cancelled and the event has not taken place.</summary>
        public bool CanWithdraw { get; set; }
    }

    // ---------- Results ----------

    public class ResultRequest
    {
        public int EntryId { get; set; }

        /// <summary>Null (or omitted) means this entry has no result.</summary>
        public TimeSpan? FinishTime { get; set; }

        /// <summary>Required whenever FinishTime is set. 1 is the winner.</summary>
        public int? Position { get; set; }
    }

    public class ResultDto
    {
        public int ResultId { get; set; }
        public int EntryId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
        public int TotalFinishers { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    // ---------- Profile ----------

    public class ProfileDto
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePictureName { get; set; }

        // Participant-only fields (null for organisers)
        public int? ParticipantId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [StringLength(30)]
        public string Gender { get; set; } = string.Empty;

        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [StringLength(200)]
        public string EmergencyContact { get; set; } = string.Empty;
    }
}
