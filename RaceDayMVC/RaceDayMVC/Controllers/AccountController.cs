using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Infrastructure;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiClient _api;

        public AccountController(ApiClient api)
        {
            _api = api;
        }

        // ---------- Login ----------

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var user = HttpContext.Session.GetUser();
            if (user != null) return RedirectToHome(user);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Start from a clean session so nothing from a previous visitor is carried over.
            HttpContext.Session.Clear();

            try
            {
                var result = await _api.LoginAsync(new LoginRequest
                {
                    Email = model.Email.Trim(),
                    Password = model.Password
                });

                if (!result.Success || result.User == null)
                {
                    ModelState.AddModelError(string.Empty, "We couldn't log you in. Check your email and password.");
                    return View(model);
                }

                var user = SessionUser.From(result.User);
                HttpContext.Session.SetUser(user);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return LocalRedirect(model.ReturnUrl);

                return RedirectToHome(user);
            }
            catch (ApiException ex) when (ex is not ApiUnavailableException)
            {
                HttpContext.Session.Clear();
                ModelState.AddModelError(string.Empty, ex.UserMessage);
                return View(model);
            }
        }

        // ---------- Register ----------

        [HttpGet]
        public IActionResult Register()
        {
            var user = HttpContext.Session.GetUser();
            if (user != null) return RedirectToHome(user);

            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role != "Participant")
            {
                // These fields only apply to participants; don't let stale values block an organiser.
                ModelState.Remove(nameof(model.DateOfBirth));
                ModelState.Remove(nameof(model.Gender));
            }

            if (!ModelState.IsValid) return View(model);

            HttpContext.Session.Clear();

            try
            {
                var result = await _api.RegisterAsync(new RegisterRequest
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Email = model.Email.Trim(),
                    Password = model.Password,
                    PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty,
                    Role = model.Role,
                    OrganiserAccessCode = model.Role == "Organiser" ? model.OrganiserAccessCode?.Trim() : null,
                    DateOfBirth = model.Role == "Participant" ? model.DateOfBirth : null,
                    Gender = model.Role == "Participant" ? model.Gender : null
                });

                if (!result.Success || result.User == null)
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(model);
                }

                // The API signs the new user in as part of registration, so go straight in.
                var user = SessionUser.From(result.User);
                HttpContext.Session.SetUser(user);
                TempData["Success"] = $"Welcome to RaceDay, {user.FirstName}.";

                return RedirectToHome(user);
            }
            catch (ApiException ex) when (ex is not ApiUnavailableException)
            {
                HttpContext.Session.Clear();
                ModelState.AddModelError(string.Empty, ex.UserMessage);
                return View(model);
            }
        }

        // ---------- Logout ----------

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _api.LogoutAsync();
            HttpContext.Session.Clear();

            TempData["Success"] = "You've been logged out.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        private IActionResult RedirectToHome(SessionUser user) =>
            user.IsOrganiser
                ? RedirectToAction("Index", "Dashboard")
                : RedirectToAction("Index", "Home");
    }
}
