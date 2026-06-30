using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class ReservationRepository(LoafNcattingDbContext context) : GenericRepository<Reservation>(context), IReservationRepository
{
    public async Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId)
    {
        return await IncludeDetails(_context.Reservations)
            .Where(reservation => reservation.UserId == userId)
            .OrderByDescending(reservation => reservation.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetStaffReservationsAsync(
        int? statusId,
        DateOnly? date)
    {
        var query = IncludeDetails(_context.Reservations);
        if (statusId.HasValue)
        {
            query = query.Where(reservation => reservation.StatusId == statusId.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(reservation => reservation.Date == date.Value);
        }

        return await query
            .OrderByDescending(reservation => reservation.Date)
            .ThenByDescending(reservation => reservation.Time)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdWithDetailsAsync(int reservationId)
    {
        return await IncludeDetails(_context.Reservations)
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

    private static IQueryable<Reservation> IncludeDetails(IQueryable<Reservation> query)
    {
        return query
            .Include(reservation => reservation.Status)
            .Include(reservation => reservation.Table);
    }
}

