using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IReservationStatusRepository : IGenericRepository<ReservationStatus>
{
    Task<ReservationStatus> GetByNameAsync(string name);
}

