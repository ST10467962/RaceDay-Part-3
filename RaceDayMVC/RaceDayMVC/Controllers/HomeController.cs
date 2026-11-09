using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClient _api;

        public HomeController(ApiClient api)
        {
            _api = api;
        }

        /// <summary>
        /// Visitors see the welcome page. Participants see upcoming events with filters.
        /// Organisers are sent to their dashboard.
        /// </summary>
        public async Task<IActionResult> Index(EventQuery filter)
        {
            var user = HttpContext.Session.GetUser();

            if (user == null) return View("Landing");
            if (user.IsOrganiser) return RedirectToAction("Index", "Dashboard");

            // Participants only ever browse upcoming events; ignore anything else in the URL.
            filter.Upcoming = true;
            filter.Mine = false;

            var events = await _api.GetEventsAsync(filter);

            return View(new EventListViewModel { Events = events, Filter = filter });
        }

        // Re-run by the exception handler with the original HTTP method, so it must not demand an anti-forgery token.
        [Route("Home/Error")]
        [IgnoreAntiforgeryToken]
        public IActionResult Error(int? code)
        {
            var status = code ?? StatusCodes.Status500InternalServerError;

            var model = status == StatusCodes.Status404NotFound
                ? new ErrorViewModel
                {
                    StatusCode = 404,
                    Title = "We couldn't find that page",
                    Message = "It may have moved, or the link might be out of date."
                }
                : new ErrorViewModel
                {
                    StatusCode = status,
                    Title = "Something went wrong",
                    Message = "Something unexpected happened on our side. Please try again in a moment."
                };

            return View(model);
        }
    }
}
