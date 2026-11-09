namespace RaceDayMVC.Models.Api
{
    // These classes mirror the JSON the Part 2 API sends and receives.
    // The MVC project holds data shapes only; all rules and storage live in the API.

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? OrganiserAccessCode { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? ParticipantId { get; set; }
        public string? ProfilePictureName { get; set; }
    }

    public class EventRequest
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal Distance { get; set; }
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

        // Flags computed by the API. The MVC never works these out itself.
        public bool IsUpcoming { get; set; }
        public bool HasTakenPlace { get; set; }
        public bool IsOwner { get; set; }
        public bool CanEnrol { get; set; }
        public EntryDto? MyEntry { get; set; }
    }

    public class EnrolRequest
    {
        public int CategoryId { get; set; }
    }

    public class EntryStatusRequest
    {
        public string Status { get; set; } = string.Empty;
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
        public bool CanWithdraw { get; set; }
    }

    public class ResultRequest
    {
        public int EntryId { get; set; }
        public TimeSpan? FinishTime { get; set; }
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

    public class ProfileDto
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePictureName { get; set; }
        public int? ParticipantId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
    }

    /// <summary>Filter values sent to GET api/Events.</summary>
    public class EventQuery
    {
        public string? Search { get; set; }
        public string? EventType { get; set; }
        public decimal? MinDistance { get; set; }
        public decimal? MaxDistance { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool Upcoming { get; set; }
        public bool Mine { get; set; }
    }
}
