using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IMessageRepository : IGenericRepository<Message>
{
    Task<IEnumerable<Message>> GetByConversationIdAsync(int conversationId);
}

