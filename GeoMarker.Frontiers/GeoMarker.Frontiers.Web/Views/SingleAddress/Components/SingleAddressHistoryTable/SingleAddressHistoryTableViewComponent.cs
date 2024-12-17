using GeoMarker.Frontiers.Core.Resources;
using GeoMarker.Frontiers.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace GeoMarker.Frontiers.Web.Views.SingleAddress.Components.HistoryTable
{
    [ViewComponent]
    public class SingleAddressHistoryTableViewComponent : ViewComponent
    {
        private readonly ILogger<SingleAddressHistoryTableViewComponent> _logger;

        public SingleAddressHistoryTableViewComponent(ILogger<SingleAddressHistoryTableViewComponent> logger)
        {
            _logger = logger;
        }

        public IViewComponentResult Invoke(SingleAddressViewModel model)
        {
            return View("SingleAddressHistoryTable", model);
        }
    }
}
