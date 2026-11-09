using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDayMVC.Models.ViewModels;

namespace RaceDayMVC.Infrastructure
{
    /// <summary>
    /// Last line of defence for API failures that a controller did not handle itself.
    /// Users get a friendly page or redirect, never a stack trace or raw error text.
    /// </summary>
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly IModelMetadataProvider _metadata;
        private readonly ILogger<ApiExceptionFilter> _logger;

        public ApiExceptionFilter(
            ITempDataDictionaryFactory tempDataFactory,
            IModelMetadataProvider metadata,
            ILogger<ApiExceptionFilter> logger)
        {
            _tempDataFactory = tempDataFactory;
            _metadata = metadata;
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not ApiException apiException) return;

            var http = context.HttpContext;

            switch (apiException)
            {
                case ApiUnauthorizedException:
                    // The API login ran out. End the MVC session too and ask the user to log in again.
                    http.Session.Clear();
                    _tempDataFactory.GetTempData(http)["Error"] = "Your session has ended. Please log in again.";
                    context.Result = new RedirectToActionResult("Login", "Account", null);
                    break;

                case ApiForbiddenException:
                    context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                    break;

                case ApiNotFoundException:
                    context.Result = ErrorView(404, "We couldn't find that page",
                        "It may have been removed, or the link might be out of date.");
                    break;

                default:
                    _logger.LogWarning("API error {Status}: {Message}", (int)apiException.StatusCode, apiException.Message);
                    var status = apiException is ApiUnavailableException ? 503 : 500;
                    context.Result = ErrorView(status, "RaceDay hit a problem", apiException.UserMessage);
                    break;
            }

            context.ExceptionHandled = true;
        }

        private ViewResult ErrorView(int statusCode, string title, string message)
        {
            return new ViewResult
            {
                ViewName = "Error",
                StatusCode = statusCode,
                ViewData = new ViewDataDictionary<ErrorViewModel>(_metadata, new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    Model = new ErrorViewModel { StatusCode = statusCode, Title = title, Message = message }
                }
            };
        }
    }
}
