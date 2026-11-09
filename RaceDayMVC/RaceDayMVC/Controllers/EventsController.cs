using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    /// <summary>Event details (everyone who is logged in) and enrolment (participants only).</summary>
    [SessionAuthorize]
    public class EventsController : Controller
    {
        private readonly ApiClient _api;

        public EventsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Details(int id)
        {
            // The API works out whether this participant can enrol and what their current entry is.
            var ev = await _api.GetEventAsync(id);

            return View(new EventDetailsViewModel
            {
                Event = ev,
                MyEntry = ev.MyEntry,
                CanEnrol = ev.CanEnrol
            });
        }

        [HttpPost]
        [SessionAuthorize(Role = "Participant")]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            try
            {
                await _api.EnrolAsync(categoryId);
                TempData["Success"] = "You're enrolled. Your place stays pending until the organiser confirms it.";
                return RedirectToAction("Index", "MyEnrolments");
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                TempData["Error"] = ex.UserMessage;
                return RedirectToAction(nameof(Details), new { id = eventId });
            }
        }
    }
}
