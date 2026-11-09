using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize(Role = "Organiser")]
    public class ManageEventsController : Controller
    {
        private readonly ApiClient _api;

        public ManageEventsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _api.GetEventsAsync(new EventQuery { Mine = true });
            return View(events);
        }

        // ---------- Create ----------

        [HttpGet]
        public IActionResult Create() => View("Form", new EventFormViewModel());

        [HttpPost]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            ValidateBanner(model);
            if (!ModelState.IsValid) return View("Form", model);

            try
            {
                var created = await _api.CreateEventAsync(ToRequest(model));

                if (ImageRules.HasFile(model.BannerImage))
                {
                    try
                    {
                        await _api.UploadBannerAsync(created.EventId, model.BannerImage!);
                    }
                    catch (ApiException ex) when (ex.IsUserFacing)
                    {
                        TempData["Error"] = $"The event was saved, but the banner wasn't: {ex.UserMessage}";
                        return RedirectToAction(nameof(Edit), new { id = created.EventId });
                    }
                }

                TempData["Success"] = $"\"{created.EventName}\" is saved. Add categories so participants can enrol.";
                return RedirectToAction("Index", "ManageCategories", new { eventId = created.EventId });
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                ModelState.AddModelError(string.Empty, ex.UserMessage);
                return View("Form", model);
            }
        }

        // ---------- Edit ----------

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _api.GetEventAsync(id);
            if (!ev.IsOwner) return RedirectToAction("AccessDenied", "Account");

            return View("Form", new EventFormViewModel
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Distance = ev.Distance,
                EventType = ev.EventType,
                StartTime = ViewHelpers.TimeOfDay(ev.StartTime),
                EndTime = ViewHelpers.TimeOfDay(ev.EndTime),
                ExistingBannerName = ev.BannerImageName
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EventFormViewModel model)
        {
            var id = model.EventId;
            ValidateBanner(model);

            if (!ModelState.IsValid)
            {
                await RestoreBannerNameAsync(model);
                return View("Form", model);
            }

            try
            {
                await _api.UpdateEventAsync(id, ToRequest(model));

                if (ImageRules.HasFile(model.BannerImage))
                    await _api.UploadBannerAsync(id, model.BannerImage!);

                TempData["Success"] = "Your changes to the event are saved.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                ModelState.AddModelError(string.Empty, ex.UserMessage);
                await RestoreBannerNameAsync(model);
                return View("Form", model);
            }
        }

        // ---------- Delete ----------

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _api.DeleteEventAsync(id);
                TempData["Success"] = "The event and its enrolments have been deleted.";
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                TempData["Error"] = ex.UserMessage;
            }

            return RedirectToAction(nameof(Index));
        }

        // ---------- helpers ----------

        private void ValidateBanner(EventFormViewModel model)
        {
            // Quick check so the form can explain a bad file before anything is sent. The API applies the same limits.
            var problem = ImageRules.Validate(model.BannerImage);
            if (problem != null)
                ModelState.AddModelError(nameof(model.BannerImage), problem);
        }

        /// <summary>The existing banner name isn't posted back with the form, so fetch it to redisplay the preview.</summary>
        private async Task RestoreBannerNameAsync(EventFormViewModel model)
        {
            if (model.IsNew) return;
            try
            {
                var ev = await _api.GetEventAsync(model.EventId);
                model.ExistingBannerName = ev.BannerImageName;
            }
            catch (ApiException)
            {
                // The preview is optional; the form can still be shown without it.
            }
        }

        private static EventRequest ToRequest(EventFormViewModel model) => new()
        {
            EventName = model.EventName.Trim(),
            Description = model.Description.Trim(),
            EventDate = model.EventDate!.Value.Date,
            Location = model.Location.Trim(),
            Distance = model.Distance!.Value,
            EventType = model.EventType,
            StartTime = ParseTime(model.StartTime),
            EndTime = ParseTime(model.EndTime)
        };

        private static TimeSpan? ParseTime(string? value) =>
            !string.IsNullOrWhiteSpace(value) && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var time)
                ? time
                : null;
    }
}
