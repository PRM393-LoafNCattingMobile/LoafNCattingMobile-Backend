using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class ConversationService(IConversationRepository conversations) : IConversationService
{
    public async Task<ConversationDto> GetOrCreateConversationAsync(int userId)
    {
        var conversation = await conversations.GetByCustomerUserIdAsync(userId);

        if (conversation is null)
        {
            conversation = new Conversation { CustomerUserId = userId };
            await conversations.AddAsync(conversation);
            await conversations.SaveChangesAsync();
        }

        return new ConversationDto(conversation.ConversationId, conversation.CustomerUserId, conversation.CreatedAt);
    }

    public Task<List<ConversationInboxItemDto>> GetInboxAsync() =>
        throw new NotImplementedException();
}



