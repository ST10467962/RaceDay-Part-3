using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    /// <summary>Capture finish times and finishing positions once an event has taken place.</summary>
    [SessionAuthorize(Role = "Organiser")]
    public class ManageResultsController : Controller
    {
        private static readonly string[] TimeFormats = { @"h\:mm\:ss", @"hh\:mm\:ss" };

        private readonly ApiClient _api;

        public ManageResultsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(int? eventId)
        {
            var events = await _api.GetEventsAsync(new EventQuery { Mine = true });
            var selected = events.FirstOrDefault(e => e.EventId == eventId) ?? events.FirstOrDefault();

            return View(await BuildModelAsync(events, selected, null));
        }

        [HttpPost]
        public async Task<IActionResult> Save(int eventId, List<ResultRowViewModel> rows)
        {
            var events = await _api.GetEventsAsync(new EventQuery { Mine = true });
            var selected = events.FirstOrDefault(e => e.EventId == eventId);
            if (selected == null) return RedirectToAction("AccessDenied", "Account");

            rows ??= new List<ResultRowViewModel>();

            var requests = new List<ResultRequest>();
            for (var i = 0; i < rows.Count; i++)
            {
                var text = rows[i].FinishTime?.Trim();

                if (string.IsNullOrEmpty(text))
                {
                    // No time means no result for this entry.
                    requests.Add(new ResultRequest { EntryId = rows[i].EntryId });
                    continue;
                }

                if (!TimeSpan.TryParseExact(text, TimeFormats, CultureInfo.InvariantCulture, out var time) || time <= TimeSpan.Zero)
                {
                    ModelState.AddModelError($"rows[{i}].FinishTime", "Use hours:minutes:seconds, for example 00:45:10.");
                    continue;
                }

                if (rows[i].Position is null or < 1)
                {
                    ModelState.AddModelError($"rows[{i}].Position", "Enter the finishing position, for example 1.");
                    continue;
                }

                requests.Add(new ResultRequest
                {
                    EntryId = rows[i].EntryId,
                    FinishTime = time,
                    Position = rows[i].Position
                });
            }

            if (!ModelState.IsValid)
                return View("Index", await BuildModelAsync(events, selected, rows));

            try
            {
                await _api.SaveResultsAsync(eventId, requests);
                TempData["Success"] = "Results saved and published.";
            }
            catch (ApiException ex) when (ex.IsUserFacing)
            {
                ModelState.AddModelError(string.Empty, ex.UserMessage);
                return View("Index", await BuildModelAsync(events, selected, rows));
            }

            return RedirectToAction(nameof(Index), new { eventId });
        }

        /// <param name="postedRows">When redisplaying after an error, the values the organiser typed are kept.</param>
        private async Task<ResultsEntryViewModel> BuildModelAsync(
            List<EventDto> events, EventDto? selected, List<ResultRowViewModel>? postedRows)
        {
            var model = new ResultsEntryViewModel { Events = events, Selected = selected };
            if (selected == null) return model;

            model.EventId = selected.EventId;
            model.EventHasHappened = selected.HasTakenPlace;

            var entries = await _api.GetEntriesAsync(selected.EventId);
            var results = await _api.GetResultsAsync(selected.EventId);
            var resultByEntry = results.ToDictionary(r => r.EntryId);

            model.TotalFinishers = results.Count;
            model.Rows = entries
                .Where(e => e.Status != "Cancelled")
                .Select(e =>
                {
                    resultByEntry.TryGetValue(e.EntryId, out var result);
                    return new ResultRowViewModel
                    {
                        EntryId = e.EntryId,
                        ParticipantName = e.ParticipantName,
                        CategoryName = e.CategoryName,
                        Status = e.Status,
                        FinishTime = result == null ? null : result.FinishTime.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
                        Position = result?.Position
                    };
                })
                .OrderBy(r => r.Position ?? int.MaxValue)
                .ThenBy(r => r.ParticipantName)
                .ToList();

            if (postedRows != null)
            {
                var typed = postedRows.ToDictionary(r => r.EntryId);
                foreach (var row in model.Rows)
                {
                    if (!typed.TryGetValue(row.EntryId, out var posted)) continue;
                    row.FinishTime = posted.FinishTime;
                    row.Position = posted.Position;
                }
            }

            return model;
        }
    }
}
