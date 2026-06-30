using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IReservationRepository : IGenericRepository<Reservation>
{
    Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId);
    Task<IEnumerable<Reservation>> GetStaffReservationsAsync(int? statusId, DateOnly? date);
    Task<Reservation?> GetByIdWithDetailsAsync(int reservationId);
    Task<List<int>> GetUnavailableTableIdsAsync(DateOnly date, TimeOnly time);
}

