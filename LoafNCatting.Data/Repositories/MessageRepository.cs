using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class MessageRepository(LoafNcattingDbContext context) : GenericRepository<Message>(context), IMessageRepository
{
    public async Task<IEnumerable<Message>> GetByConversationIdAsync(int conversationId)
    {
        return await _context.Messages
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.SentAt)
            .ToListAsync();
    }
}

