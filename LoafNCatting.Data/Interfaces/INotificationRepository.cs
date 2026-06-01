using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
}

