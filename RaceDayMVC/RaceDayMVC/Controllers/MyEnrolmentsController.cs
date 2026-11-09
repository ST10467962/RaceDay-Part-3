using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize(Role = "Participant")]
    public class MyEnrolmentsController : Controller
    {
        private readonly ApiClient _api;

        public MyEnrolmentsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var entries = await _api.GetEntriesAsync();
            return View(entries.OrderByDescending(e => e.EventDate).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Withdraw(int entryId)
        {
            try
            {
                await _api.UpdateEntryStatusAsync(entryId, "Cancelled");
                TempData["Success"] = "Your enrolment has been cancelled.";
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                TempData["Error"] = ex.UserMessage;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
