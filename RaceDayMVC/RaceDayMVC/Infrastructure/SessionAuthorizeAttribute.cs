using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayMVC.Infrastructure
{
    /// <summary>
    /// Session-based access control. Requires a logged-in user (stored in session at login)
    /// and, optionally, a specific role. Everyone else is sent to the login or access-denied page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        /// <summary>"Organiser" or "Participant". Leave empty to allow any logged-in user.</summary>
        public string? Role { get; set; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.Session.GetUser();

            if (user == null)
            {
                // Only remember the page for GET requests; a POST can't be replayed after login.
                var request = context.HttpContext.Request;
                var returnUrl = HttpMethods.IsGet(request.Method)
                    ? request.Path + request.QueryString
                    : null;

                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            if (!string.IsNullOrEmpty(Role) && !string.Equals(user.Role, Role, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}
