using FluentValidation.Results;
using GeoMarker.Frontiers.Core.Models.Commands;
using Microsoft.AspNetCore.Mvc;

namespace GeoMarker.Frontiers.Core.Services
{
    public interface IDeGaussCommandService
    {
        public Task<CommandTaskResponse> GetService(DeGaussCommandTask task, string commandGuid);
        public Task<string> StartGetServiceAsync(DeGaussCommandTask task, string commandGuid);
        public Task<CommandTaskResponse> GetServiceStatusAsync(string guid);
        public Task<CommandTaskResponse> GetServiceResultAsync(string guid, string type);
        public Task<JsonAddressResponse> GetJsonAddressService(JsonAddressCommandTask task);
        public Task<GeocodedJsonResponse> GetJsonService(string recordsJson, string? site = null, int? year = null);
    }
}
