namespace LogDashboard
{
    public class LogDashboardBasicAuthorizationFilter : LogDashboard.Authorization.ILogDashboardAuthorizationFilter
    {
        private readonly string _username;
        private readonly string _password;
        public LogDashboardBasicAuthorizationFilter(string username, string password)
        {
            this._username = username;
            this._password = password;
        }
        public bool Authorization(LogDashboardContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true) return true;

            var httpContext = context.HttpContext;

            var header = httpContext.Request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(header))
            {
                SetChallengeResponseAsync(httpContext);
                return false;
            }

            var authValues = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(header);

            if (!"Basic".Equals(authValues.Scheme, StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrWhiteSpace(authValues.Parameter))
            {
                SetChallengeResponseAsync(httpContext);
                return false;
            }

            var parameter = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authValues.Parameter));
            var parts = parameter.Split(':');

            if (parts.Length < 2)
            {
                SetChallengeResponseAsync(httpContext);
                return false;
            }

            var username = parts[0];
            var password = parts[1];

            if (string.IsNullOrWhiteSpace(username))
            {
                SetChallengeResponseAsync(httpContext);
                return false;
            }

            if (username.Equals(_username, StringComparison.OrdinalIgnoreCase) && password == _password)
            {
                return true;
            }

            SetChallengeResponseAsync(httpContext);
            return false;
        }

        private async void SetChallengeResponseAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = 401;
            httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
            await httpContext.Response.WriteAsync("Authentication is required.");
        }
    }
}
