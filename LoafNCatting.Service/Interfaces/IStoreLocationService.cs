using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IStoreLocationService
{
    Task<StoreLocationDto?> GetStoreLocationAsync();
}

