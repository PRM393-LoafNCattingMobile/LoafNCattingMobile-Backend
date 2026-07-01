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

    public async Task<List<ConversationInboxItemDto>> GetInboxAsync()
    {
        var items = await conversations.GetInboxAsync();

        return items
            .Select(conversation =>
            {
                var lastMessage = conversation.Messages
                    .OrderByDescending(message => message.SentAt)
                    .FirstOrDefault();

                return new ConversationInboxItemDto(
                    conversation.ConversationId,
                    conversation.CustomerUserId,
                    conversation.CustomerUser?.Name ?? string.Empty,
                    lastMessage?.Content,
                    lastMessage is null
                        ? null
                        : lastMessage.SenderUserId == conversation.CustomerUserId ? "customer" : "store",
                    lastMessage?.SentAt,
                    conversation.Messages.Count(message => !message.IsRead && message.SenderUserId == conversation.CustomerUserId),
                    conversation.CreatedAt,
                    conversation.UpdatedAt);
            })
            .OrderByDescending(item => item.LastMessageSentAt ?? item.CreatedAt)
            .ToList();
    }
}



