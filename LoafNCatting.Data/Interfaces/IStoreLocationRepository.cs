using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IStoreLocationRepository : IGenericRepository<StoreLocation>
{
    Task<StoreLocation?> GetFirstAsync();
}

