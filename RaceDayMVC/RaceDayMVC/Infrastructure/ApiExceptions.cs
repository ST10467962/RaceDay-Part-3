using System.Net;

namespace RaceDayMVC.Infrastructure
{
    /// <summary>A failed call to the RaceDay API, carrying a message that is safe to show to a user.</summary>
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string UserMessage { get; }

        public ApiException(HttpStatusCode statusCode, string userMessage)
            : base(userMessage)
        {
            StatusCode = statusCode;
            UserMessage = userMessage;
        }

        /// <summary>True for errors caused by what the user typed (the form can be shown again with the message).</summary>
        public bool IsUserFacing =>
            StatusCode == HttpStatusCode.BadRequest ||
            StatusCode == HttpStatusCode.Conflict ||
            StatusCode == HttpStatusCode.UnprocessableEntity;
    }

    /// <summary>The API says the login has expired or was never established (401).</summary>
    public class ApiUnauthorizedException : ApiException
    {
        public ApiUnauthorizedException(string message) : base(HttpStatusCode.Unauthorized, message) { }
    }

    /// <summary>The logged-in user is not allowed to do this (403).</summary>
    public class ApiForbiddenException : ApiException
    {
        public ApiForbiddenException(string message) : base(HttpStatusCode.Forbidden, message) { }
    }

    /// <summary>The requested record does not exist (404).</summary>
    public class ApiNotFoundException : ApiException
    {
        public ApiNotFoundException(string message) : base(HttpStatusCode.NotFound, message) { }
    }

    /// <summary>The API could not be reached at all (not running, wrong address, timeout).</summary>
    public class ApiUnavailableException : ApiException
    {
        public ApiUnavailableException()
            : base(HttpStatusCode.ServiceUnavailable,
                   "RaceDay can't reach its data service right now. Please try again in a moment.") { }
    }
}
