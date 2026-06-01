using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class ReservationRepository(LoafNcattingDbContext context) : GenericRepository<Reservation>(context), IReservationRepository
{
    public async Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId)
    {
        return await _context.Reservations
            .Include(reservation => reservation.Status)
            .Include(reservation => reservation.Table)
            .Where(reservation => reservation.UserId == userId)
            .OrderByDescending(reservation => reservation.CreatedAt)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdWithDetailsAsync(int reservationId)
    {
        return await _context.Reservations
            .Include(reservation => reservation.Status)
            .Include(reservation => reservation.Table)
            .FirstOrDefaultAsync(reservation => reservation.ReservationId == reservationId);
    }

    public async Task<List<int>> GetUnavailableTableIdsAsync(DateOnly date, TimeOnly time)
    {
        return await _context.Reservations
            .Where(reservation =>
                reservation.Date == date &&
                reservation.Time == time &&
                reservation.Status.StatusName != "Cancelled")
            .Select(reservation => reservation.TableId)
            .ToListAsync();
    }
}

