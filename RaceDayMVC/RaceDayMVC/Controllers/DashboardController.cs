using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize(Role = "Organiser")]
    public class DashboardController : Controller
    {
        private readonly ApiClient _api;

        public DashboardController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _api.GetEventsAsync(new EventQuery { Mine = true });
            return View(new DashboardViewModel { Events = events });
        }
    }
}
