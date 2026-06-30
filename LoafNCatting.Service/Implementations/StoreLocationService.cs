using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Service.Implementations;

public class StoreLocationService(IStoreLocationRepository locations) : IStoreLocationService
{
    public async Task<StoreLocationDto?> GetStoreLocationAsync()
    {
        var location = await locations.GetFirstAsync();
        return location is null ? null : ToDto(location);
    }

    public async Task<StoreLocationDto?> UpdateStoreLocationAsync(
        AdminStoreLocationRequestDto request)
    {
        var storeName = request.StoreName.Trim();
        var address = request.Address.Trim();
        var phoneNumber = NormalizeOptional(request.PhoneNumber);
        var openingHours = NormalizeOptional(request.OpeningHours);
        if (string.IsNullOrWhiteSpace(storeName) ||
            storeName.Length > 255 ||
            string.IsNullOrWhiteSpace(address) ||
            phoneNumber?.Length > 20 ||
            openingHours?.Length > 255 ||
            !double.IsFinite(request.Latitude) ||
            request.Latitude is < -90 or > 90 ||
            !double.IsFinite(request.Longitude) ||
            request.Longitude is < -180 or > 180)
        {
            return null;
        }

        var location = await locations.GetFirstAsync();
        if (location is null)
        {
            return null;
        }

        location.StoreName = storeName;
        location.Address = address;
        location.PhoneNumber = phoneNumber;
        location.OpeningHours = openingHours;
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        locations.Update(location);
        await locations.SaveChangesAsync();
        return ToDto(location);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static StoreLocationDto ToDto(StoreLocation location)
    {
        return new StoreLocationDto(
            location.StoreName,
            location.Address,
            location.PhoneNumber,
            location.OpeningHours,
            location.Latitude,
            location.Longitude);
    }
}



