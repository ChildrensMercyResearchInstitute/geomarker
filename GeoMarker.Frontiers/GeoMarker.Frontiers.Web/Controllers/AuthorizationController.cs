using GeoMarker.Frontiers.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoMarker.Frontiers.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthorizationController : Controller
    {
        private readonly ILogger<AuthorizationController> _logger;

        public AuthorizationController(ILogger<AuthorizationController> logger)
        {
            _logger = logger;
        }

        [HttpGet("~/AccessDenied")]
        [AllowAnonymous]
        public IActionResult AccessDenied(int? statusCode = null, string? failedUrl = null)
        {
            AuthorizationFailureViewModel model = new AuthorizationFailureViewModel();

            if (failedUrl != null && failedUrl.Contains("Admin",StringComparison.OrdinalIgnoreCase))
                model.IsAdminAccessAttempt = true;

            return View(model);
        }
    }
}
