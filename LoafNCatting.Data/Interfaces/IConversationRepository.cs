using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IConversationRepository : IGenericRepository<Conversation>
{
    Task<Conversation?> GetByCustomerUserIdAsync(int userId);
}

