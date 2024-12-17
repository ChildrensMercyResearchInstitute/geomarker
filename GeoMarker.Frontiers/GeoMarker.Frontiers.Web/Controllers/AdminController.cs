using GeoMarker.Frontiers.Web.Models;
using GeoMarker.Frontiers.Web.Models.Configuration;
using GeoMarker.Frontiers.Web.Models.Services;
using LinqKit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GeoMarker.Frontiers.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IMetadataService _metadataService;

        public AdminController(ILogger<AdminController> logger, IMetadataService metadataService)
        {
            _logger = logger;
            _metadataService = metadataService;
        }

        [HttpGet("~/Admin")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "AdminUserPolicy")]
        public IActionResult Index()
        {            
            var model = new AdminViewModel();
            model.AllRecords = _metadataService.GetRecordsProcessed();
            model.UserIdOptions = _metadataService.GetRecordsProcessedUsers();
            return View(model);
        }

        [HttpPost("~/Admin")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "AdminUserPolicy")]
        public IActionResult Index(AdminViewModel model)
        {
            var predicate = PredicateBuilder.New<RecordsProcessed>(true);

            if (model.StartDate != null)
                predicate = predicate.And(r => r.UploadDateTime > model.StartDate);
            if (model.EndDate != null)
            {
                var time = model.EndDate?.AddDays(1);
                predicate = predicate.And(r => r.UploadDateTime < time);
            }
            if (model.UserIds != null && model.UserIds.Count > 0)
                predicate = predicate.And(r => model.UserIds.Contains(r.UserId));
            if (model.RequestTypes != null && model.RequestTypes.Count > 0)
                predicate = predicate.And(r => model.RequestTypes.Contains(r.RequestType));
            if (model.RequestFormat != null)
                predicate = predicate.And(r => r.Format == model.RequestFormat);

            model.AllRecords = _metadataService.GetRecordsProcessed(predicate);
            model.UserIdOptions = _metadataService.GetRecordsProcessedUsers();

            return View(model);
        }
    }
}
