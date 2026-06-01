using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class ReservationStatusRepository(LoafNcattingDbContext context) : GenericRepository<ReservationStatus>(context), IReservationStatusRepository
{
    public async Task<ReservationStatus> GetByNameAsync(string name)
    {
        return await _context.ReservationStatuses.FirstAsync(status => status.StatusName == name);
    }
}

