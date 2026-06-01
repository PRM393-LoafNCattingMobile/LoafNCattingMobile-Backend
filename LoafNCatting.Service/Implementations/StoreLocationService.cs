using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class StoreLocationService(IStoreLocationRepository locations) : IStoreLocationService
{
    public async Task<StoreLocationDto?> GetStoreLocationAsync()
    {
        var location = await locations.GetFirstAsync();
        return location is null
            ? null
            : new StoreLocationDto(
                location.StoreName,
                location.Address,
                location.PhoneNumber,
                location.OpeningHours,
                location.Latitude,
                location.Longitude);
    }
}



