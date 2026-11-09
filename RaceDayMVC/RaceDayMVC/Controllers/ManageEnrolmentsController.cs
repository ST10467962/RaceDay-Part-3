using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    /// <summary>Who has enrolled for an event, in which category, and whether their place is confirmed.</summary>
    [SessionAuthorize(Role = "Organiser")]
    public class ManageEnrolmentsController : Controller
    {
        private readonly ApiClient _api;

        public ManageEnrolmentsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(int? eventId, string status = "All")
        {
            var events = await _api.GetEventsAsync(new EventQuery { Mine = true });
            var selected = events.FirstOrDefault(e => e.EventId == eventId) ?? events.FirstOrDefault();

            if (status != "All" && !FormOptions.EntryStatuses.Contains(status))
                status = "All";

            return View(new EnrolmentListViewModel
            {
                Events = events,
                Selected = selected,
                StatusFilter = status,
                Entries = selected == null
                    ? new List<EntryDto>()
                    : (await _api.GetEntriesAsync(selected.EventId)).OrderBy(e => e.ParticipantName).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> SetStatus(int entryId, string status, int eventId, string filter = "All")
        {
            if (!FormOptions.EntryStatuses.Contains(status))
            {
                TempData["Error"] = "That isn't a valid status.";
                return RedirectToAction(nameof(Index), new { eventId, status = filter });
            }

            try
            {
                await _api.UpdateEntryStatusAsync(entryId, status);
                TempData["Success"] = status switch
                {
                    "Confirmed" => "Enrolment confirmed.",
                    "Cancelled" => "Enrolment cancelled.",
                    _ => "Enrolment set back to pending."
                };
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                TempData["Error"] = ex.UserMessage;
            }

            return RedirectToAction(nameof(Index), new { eventId, status = filter });
        }
    }
}
