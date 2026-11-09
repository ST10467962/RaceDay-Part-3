using System.ComponentModel.DataAnnotations;
using RaceDayMVC.Models.Api;

namespace RaceDayMVC.Models.ViewModels
{
    public static class FormOptions
    {
        public static readonly string[] EventTypes =
        {
            "Road Run", "Trail Run", "Fun Run", "Half Marathon", "Marathon",
            "Ultra", "Cycling", "Triathlon", "Walk", "Other"
        };

        public static readonly string[] Genders = { "Female", "Male", "Non-binary", "Prefer not to say" };

        public static readonly string[] EntryStatuses = { "Pending", "Confirmed", "Cancelled" };
    }

    public class ErrorViewModel
    {
        public int StatusCode { get; set; } = 500;
        public string Title { get; set; } = "Something went wrong";
        public string Message { get; set; } = "Please try again.";
    }

    // ---------- Account ----------

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Enter your first name.")]
        [StringLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your last name.")]
        [StringLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Choose a password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password needs at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type your password again.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The two passwords don't match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose how you'll use RaceDay.")]
        public string Role { get; set; } = "Participant";

        [Display(Name = "Organiser access code")]
        public string? OrganiserAccessCode { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        // Presence checks only. The API decides whether the access code and date of birth are acceptable.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role != "Participant" && Role != "Organiser")
            {
                yield return new ValidationResult("Choose Participant or Organiser.", new[] { nameof(Role) });
            }

            if (Role == "Organiser" && string.IsNullOrWhiteSpace(OrganiserAccessCode))
            {
                yield return new ValidationResult("Enter the organiser access code.", new[] { nameof(OrganiserAccessCode) });
            }

            if (Role == "Participant" && !DateOfBirth.HasValue)
            {
                yield return new ValidationResult("Enter your date of birth.", new[] { nameof(DateOfBirth) });
            }
        }
    }

    // ---------- Events (participant side) ----------

    public class EventListViewModel
    {
        public List<EventDto> Events { get; set; } = new();
        public EventQuery Filter { get; set; } = new();
        public bool HasFilter =>
            !string.IsNullOrWhiteSpace(Filter.Search) || !string.IsNullOrWhiteSpace(Filter.EventType) ||
            Filter.MinDistance.HasValue || Filter.MaxDistance.HasValue ||
            Filter.DateFrom.HasValue || Filter.DateTo.HasValue;
    }

    public class EventDetailsViewModel
    {
        public EventDto Event { get; set; } = new();

        /// <summary>The logged-in participant's active (not cancelled) enrolment in this event, if any.</summary>
        public EntryDto? MyEntry { get; set; }

        public bool CanEnrol { get; set; }
    }

    // ---------- Participant pages ----------

    public class MyResultsViewModel
    {
        public List<ResultDto> Results { get; set; } = new();
        public int Podiums => Results.Count(r => r.Position is >= 1 and <= 3);
    }

    public class ProfileViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string? ProfilePictureName { get; set; }
        public string Initials { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your first name.")]
        [StringLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your last name.")]
        [StringLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Enter your date of birth.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(200)]
        [Display(Name = "Emergency contact")]
        public string? EmergencyContact { get; set; }

        [Display(Name = "Profile picture")]
        public IFormFile? Picture { get; set; }
    }

    // ---------- Organiser pages ----------

    public class DashboardViewModel
    {
        public List<EventDto> Events { get; set; } = new();

        public int TotalEvents => Events.Count;
        public List<EventDto> Upcoming => Events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).ToList();
        public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
        public int PendingEnrolments => Events.Sum(e => e.PendingCount);
        public int MaxEnrolments => Events.Count == 0 ? 0 : Events.Max(e => e.EnrolmentCount);
    }

    public class EventFormViewModel : IValidatableObject
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Enter a name for the event.")]
        [StringLength(150)]
        [Display(Name = "Event name")]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Describe the event for participants.")]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose the event date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime? EventDate { get; set; }

        [Required(ErrorMessage = "Enter where the event takes place.")]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the distance in kilometres.")]
        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? Distance { get; set; }

        [Required(ErrorMessage = "Choose an event type.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = string.Empty;

        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Use the format HH:mm.")]
        [Display(Name = "Start time")]
        public string? StartTime { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Use the format HH:mm.")]
        [Display(Name = "End time")]
        public string? EndTime { get; set; }

        [Display(Name = "Banner image")]
        public IFormFile? BannerImage { get; set; }

        public string? ExistingBannerName { get; set; }

        public bool IsNew => EventId == 0;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(StartTime) && !string.IsNullOrEmpty(EndTime) &&
                TimeSpan.TryParse(StartTime, out var start) && TimeSpan.TryParse(EndTime, out var end) && end <= start)
            {
                yield return new ValidationResult("The end time must be after the start time.", new[] { nameof(EndTime) });
            }
        }
    }

    public class CategoryFormViewModel
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a name for the category.")]
        [StringLength(100)]
        [Display(Name = "Category name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the category distance in kilometres.")]
        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? Distance { get; set; }

        [Required(ErrorMessage = "Enter an age group, for example 18-34 or Open.")]
        [StringLength(50)]
        [Display(Name = "Age group")]
        public string AgeGroup { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the entry fee. Use 0 for a free category.")]
        [Range(0, 100000, ErrorMessage = "The entry fee can't be negative.")]
        [Display(Name = "Entry fee (R)")]
        public decimal? EntryFee { get; set; }

        public bool IsNew => CategoryId == 0;
    }

    /// <summary>Used by every organiser page that works on one event at a time.</summary>
    public class EventPickerViewModel
    {
        public List<EventDto> Events { get; set; } = new();
        public EventDto? Selected { get; set; }
    }

    public class CategoryListViewModel : EventPickerViewModel
    {
        public List<CategoryDto> Categories { get; set; } = new();
    }

    public class EnrolmentListViewModel : EventPickerViewModel
    {
        public List<EntryDto> Entries { get; set; } = new();
        public string StatusFilter { get; set; } = "All";

        public int CountFor(string status) =>
            status == "All" ? Entries.Count : Entries.Count(e => e.Status == status);

        public IEnumerable<EntryDto> Visible =>
            StatusFilter == "All" ? Entries : Entries.Where(e => e.Status == StatusFilter);
    }

    public class ResultRowViewModel
    {
        public int EntryId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Finish time")]
        public string? FinishTime { get; set; }

        public int? Position { get; set; }
    }

    public class ResultsEntryViewModel : EventPickerViewModel
    {
        public int EventId { get; set; }
        public bool EventHasHappened { get; set; }
        public int TotalFinishers { get; set; }
        public List<ResultRowViewModel> Rows { get; set; } = new();
    }
}
