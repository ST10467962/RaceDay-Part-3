using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize(Role = "Participant")]
    public class MyResultsController : Controller
    {
        private readonly ApiClient _api;

        public MyResultsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var results = await _api.GetResultsAsync();
            return View(new MyResultsViewModel
            {
                Results = results.OrderByDescending(r => r.EventDate).ToList()
            });
        }
    }
}
