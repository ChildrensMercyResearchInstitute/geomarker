using GeoMarker.Frontiers.Web.Models;
using GeoMarker.Frontiers.Web.Models.Services;
using GeoMarker.Frontiers.Web.Resources;
using Microsoft.AspNetCore.Mvc;

namespace GeoMarker.Frontiers.Web.Views.Home.Components.HistoryTable
{
    [ViewComponent]
    public class HistoryTableViewComponent : ViewComponent
    {
        private readonly IUserRequestRepository _userRequestRepository;
        private readonly ILogger<HistoryTableViewComponent> _logger;


        public HistoryTableViewComponent(IUserRequestRepository userRequestRepository,
                                         ILogger<HistoryTableViewComponent> logger
                                        )
        {
            _userRequestRepository = userRequestRepository;
            _logger = logger;

        }

        public IViewComponentResult Invoke(MultiAddressIndexViewModel model, string userId)
        {
            try
            {
                foreach (var request in model.UserRequests)
                {
                    List<UserRequest> group;
                    if (!model.UserRequestGroups.TryGetValue(request.UploadDateTime, out group!))
                    {
                        group = new();
                        model.UserRequestGroups.Add(request.UploadDateTime, group);
                    }
                    group.Add(request);
                }

                foreach (var group in model.UserRequestGroups)
                {
                    group.Value.Sort((first, second) =>
                    {
                        var firstValue = (int)Enum.Parse(typeof(DeGaussRequestType), first.RequestSubType);
                        var secondValue = (int)Enum.Parse(typeof(DeGaussRequestType), second.RequestSubType);

                        if (firstValue == secondValue)
                            return 0;

                        if (firstValue > secondValue)
                            return 1;
                        else
                            return -1;
                    });
                }

                model.UserRequestGroups.Reverse();
            }
            catch (Exception e)
            {
                _logger.LogError(e, Messages.UiController_LoadFailureMessage);
                model.ErrorResponse = Messages.UiController_LoadFailureMessage;
            }

            return View("HistoryTable", model);
        }
    }
}
