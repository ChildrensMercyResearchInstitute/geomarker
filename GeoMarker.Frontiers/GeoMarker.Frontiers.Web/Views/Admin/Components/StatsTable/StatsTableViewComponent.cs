using GeoMarker.Frontiers.Web.Models;
using GeoMarker.Frontiers.Web.Models.Services;
using GeoMarker.Frontiers.Web.Resources;
using Microsoft.AspNetCore.Mvc;

namespace GeoMarker.Frontiers.Web.Views.Home.Components.HistoryTable
{
    [ViewComponent]
    public class StatsTableViewComponent : ViewComponent
    {
        private readonly ILogger<StatsTableViewComponent> _logger;

        public StatsTableViewComponent(ILogger<StatsTableViewComponent> logger)
        {
            _logger = logger;
        }

        public IViewComponentResult Invoke(AdminViewModel model, string userId)
        {
            return View("StatsTable", model);
        }
    }
}
