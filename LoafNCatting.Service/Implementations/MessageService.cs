using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class MessageService(
    IConversationRepository conversations,
    IMessageRepository messages,
    INotificationWriter? notifications = null,
    IUserRepository? users = null) : IMessageService
{
    public async Task<List<MessageDto>?> GetMessagesAsync(int conversationId, int requestingUserId)
    {
        var conversation = await conversations.GetByIdAsync(conversationId);
        if (conversation is null || conversation.CustomerUserId != requestingUserId)
        {
            return null;
        }

        var items = await messages.GetByConversationIdAsync(conversationId);

        var changed = false;
        foreach (var item in items.Where(message =>
                     message.SenderUserId != conversation.CustomerUserId && !message.IsRead))
        {
            item.IsRead = true;
            changed = true;
        }

        if (changed)
        {
            await messages.SaveChangesAsync();
        }

        return items.Select(message => CafeDtoMapper.ToMessageDto(message, conversation.CustomerUserId)).ToList();
    }

    public async Task<List<MessageDto>?> GetMessagesForSupportAsync(int conversationId)
    {
        var conversation = await conversations.GetByIdAsync(conversationId);
        if (conversation is null)
        {
            return null;
        }

        var items = (await messages.GetByConversationIdForSupportAsync(conversationId)).ToList();
        var changed = false;
        foreach (var item in items.Where(message =>
                     message.SenderUserId == conversation.CustomerUserId && !message.IsRead))
        {
            item.IsRead = true;
            changed = true;
        }

        if (changed)
        {
            await messages.SaveChangesAsync();
        }

        return items.Select(message => CafeDtoMapper.ToMessageDto(message, conversation.CustomerUserId)).ToList();
    }

    public async Task<List<MessageDto>?> SendMessageAsync(CreateMessageDto request, int requestingUserId)
    {
        var conversation = await conversations.GetByIdAsync(request.ConversationId);
        if (conversation is null ||
            conversation.CustomerUserId != requestingUserId ||
            request.SenderUserId != requestingUserId)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return await GetMessagesAsync(request.ConversationId, requestingUserId);
        }

        await messages.AddAsync(new Message
        {
            ConversationId = request.ConversationId,
            SenderUserId = request.SenderUserId,
            Content = request.Content.Trim(),
            IsRead = false
        });
        conversation.UpdatedAt = DateTime.UtcNow;

        await messages.SaveChangesAsync();
        await NotifySupportUsersAsync(conversation, request.Content.Trim());
        return await GetMessagesAsync(request.ConversationId, requestingUserId);
    }

    public async Task<List<MessageDto>?> SendSupportMessageAsync(
        int conversationId,
        SupportMessageDto request,
        int staffUserId)
    {
        var conversation = await conversations.GetByIdAsync(conversationId);
        if (conversation is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return await GetMessagesForSupportAsync(conversationId);
        }

        await messages.AddAsync(new Message
        {
            ConversationId = conversationId,
            SenderUserId = staffUserId,
            Content = request.Content.Trim(),
            IsRead = false
        });

        conversation.UpdatedAt = DateTime.UtcNow;
        await messages.SaveChangesAsync();
        await NotifyCustomerAsync(conversation.CustomerUserId, request.Content.Trim());

        return await GetMessagesForSupportAsync(conversationId);
    }

    private async Task NotifySupportUsersAsync(Conversation conversation, string content)
    {
        if (notifications is null || users is null)
        {
            return;
        }

        var supportUsers = await users.GetAdminUsersAsync(roleId: null, search: null, active: true);
        foreach (var user in supportUsers.Where(IsSupportUser))
        {
            await notifications.CreateAsync(
                user.UserId,
                "Tin nhắn khách hàng mới",
                $"Khách hàng #{conversation.CustomerUserId}: {Summarize(content)}",
                "chat");
        }
    }

    private async Task NotifyCustomerAsync(int customerUserId, string content)
    {
        if (notifications is null)
        {
            return;
        }

        await notifications.CreateAsync(
            customerUserId,
            "Tin nhắn mới từ Loaf'N Catting",
            Summarize(content),
            "chat");
    }

    private static bool IsSupportUser(User user)
    {
        return string.Equals(user.Role?.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Role?.RoleName, "Staff", StringComparison.OrdinalIgnoreCase);
    }

    private static string Summarize(string content)
    {
        const int maxLength = 80;
        return content.Length <= maxLength ? content : $"{content[..maxLength]}...";
    }
}



