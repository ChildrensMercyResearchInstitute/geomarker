namespace GeoMarker.Frontiers.Web
{
    /// <summary>
    /// Custom Error Handler redirect to user-friendly error page. 
    /// 1) Default Auth Server State Token life time is 15 minutes. 
    ///    This custom error handler looks for error description from openiddict and 
    ///    renew state token by redirecting user to home page. 
    /// 2) Any status code greater then 400 will be redirected to error page. 
    /// </summary>
    public class CustomErrorHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomErrorHandler> _logger;

        public CustomErrorHandler(RequestDelegate next, ILogger<CustomErrorHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Do not intercept or change the body for API errors.
            if (context.Request.Path.HasValue && context.Request.Path.Value.Contains("/api"))
            {
                await _next(context);
                return;
            }

            var originalBodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;
                await _next(context);
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                int statusCode = context.Response.StatusCode;
                if (statusCode == 400 && await RedirectOpenIdDictResponse(context, statusCode))
                    return;
                if (statusCode >= 400)
                {
                    context.Response.Clear();
                    context.Response.Redirect("/Home/Error?StatusCode=" + statusCode);
                    return;
                }
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        private static async Task<bool> RedirectOpenIdDictResponse(HttpContext context, int statusCode)
        {
            string responseContent = await new StreamReader(context.Response.Body).ReadToEndAsync();

            if (responseContent?.Contains("Invalid_token") == true || responseContent?.Contains("openiddict") == true)
            {
                context.Response.Clear();
                context.Response.Redirect("/");
                return true;
            }
            return false;
        }
    }
}