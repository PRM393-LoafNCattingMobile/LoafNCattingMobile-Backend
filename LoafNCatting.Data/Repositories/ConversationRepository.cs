using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class ConversationRepository(LoafNcattingDbContext context) : GenericRepository<Conversation>(context), IConversationRepository
{
    public async Task<Conversation?> GetByCustomerUserIdAsync(int userId)
    {
        return await _context.Conversations.FirstOrDefaultAsync(conversation => conversation.CustomerUserId == userId);
    }

    public async Task<IEnumerable<Conversation>> GetInboxAsync()
    {
        return await _context.Conversations
            .Include(conversation => conversation.CustomerUser)
            .Include(conversation => conversation.Messages)
            .ToListAsync();
    }
}

