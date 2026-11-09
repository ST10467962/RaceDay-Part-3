using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    /// <summary>Age and distance categories for an organiser's events.</summary>
    [SessionAuthorize(Role = "Organiser")]
    public class ManageCategoriesController : Controller
    {
        private readonly ApiClient _api;

        public ManageCategoriesController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(int? eventId)
        {
            var events = await _api.GetEventsAsync(new EventQuery { Mine = true });
            var selected = events.FirstOrDefault(e => e.EventId == eventId) ?? events.FirstOrDefault();

            return View(new CategoryListViewModel
            {
                Events = events,
                Selected = selected,
                Categories = selected == null ? new List<CategoryDto>() : selected.Categories
            });
        }

        // ---------- Create ----------

        [HttpGet]
        public async Task<IActionResult> Create(int eventId)
        {
            var ev = await _api.GetEventAsync(eventId);
            if (!ev.IsOwner) return RedirectToAction("AccessDenied", "Account");

            return View("Form", new CategoryFormViewModel { EventId = ev.EventId, EventName = ev.EventName });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            var ev = await _api.GetEventAsync(model.EventId);
            if (!ev.IsOwner) return RedirectToAction("AccessDenied", "Account");
            model.EventName = ev.EventName;

            if (!ModelState.IsValid) return View("Form", model);

            try
            {
                await _api.CreateCategoryAsync(ToDto(model));
                TempData["Success"] = $"\"{model.CategoryName}\" added.";
                return RedirectToAction(nameof(Index), new { eventId = model.EventId });
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
            var category = await _api.GetCategoryAsync(id);
            var ev = await _api.GetEventAsync(category.EventId);
            if (!ev.IsOwner) return RedirectToAction("AccessDenied", "Account");

            return View("Form", new CategoryFormViewModel
            {
                CategoryId = category.CategoryId,
                EventId = category.EventId,
                EventName = ev.EventName,
                CategoryName = category.CategoryName,
                Distance = category.Distance,
                AgeGroup = category.AgeGroup,
                EntryFee = category.EntryFee
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CategoryFormViewModel model)
        {
            var ev = await _api.GetEventAsync(model.EventId);
            if (!ev.IsOwner) return RedirectToAction("AccessDenied", "Account");
            model.EventName = ev.EventName;

            if (!ModelState.IsValid) return View("Form", model);

            try
            {
                await _api.UpdateCategoryAsync(ToDto(model));
                TempData["Success"] = "Category updated.";
                return RedirectToAction(nameof(Index), new { eventId = model.EventId });
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                ModelState.AddModelError(string.Empty, ex.UserMessage);
                return View("Form", model);
            }
        }

        // ---------- Delete ----------

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int eventId)
        {
            try
            {
                await _api.DeleteCategoryAsync(id);
                TempData["Success"] = "Category deleted.";
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                TempData["Error"] = ex.UserMessage;
            }

            return RedirectToAction(nameof(Index), new { eventId });
        }

        private static CategoryDto ToDto(CategoryFormViewModel model) => new()
        {
            CategoryId = model.CategoryId,
            EventId = model.EventId,
            CategoryName = model.CategoryName.Trim(),
            Distance = model.Distance!.Value,
            AgeGroup = model.AgeGroup.Trim(),
            EntryFee = model.EntryFee!.Value
        };
    }
}
