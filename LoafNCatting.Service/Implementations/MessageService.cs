using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class MessageService(
    IConversationRepository conversations,
    IMessageRepository messages,
    IUserRepository users) : IMessageService
{
    public async Task<List<MessageDto>> GetMessagesAsync(int conversationId)
    {
        var conversation = await conversations.GetByIdAsync(conversationId);
        if (conversation is null)
        {
            return [];
        }

        var items = await messages.GetByConversationIdAsync(conversationId);

        return items.Select(message => CafeDtoMapper.ToMessageDto(message, conversation.CustomerUserId)).ToList();
    }

    public async Task<List<MessageDto>> SendMessageAsync(CreateMessageDto request)
    {
        var conversation = await conversations.GetByIdAsync(request.ConversationId);
        if (conversation is null || string.IsNullOrWhiteSpace(request.Content))
        {
            return await GetMessagesAsync(request.ConversationId);
        }

        await messages.AddAsync(new Message
        {
            ConversationId = request.ConversationId,
            SenderUserId = request.SenderUserId,
            Content = request.Content.Trim()
        });

        var staff = await users.GetFirstStaffAsync();

        if (staff is not null)
        {
            await messages.AddAsync(new Message
            {
                ConversationId = request.ConversationId,
                SenderUserId = staff.UserId,
                Content = BuildAutoReply(request.Content)
            });
        }

        await messages.SaveChangesAsync();
        return await GetMessagesAsync(request.ConversationId);
    }

    private static string BuildAutoReply(string input)
    {
        var text = input.ToLowerInvariant();
        if (text.Contains("hour") || text.Contains("open") || text.Contains("gio"))
        {
            return "Loaf'NCatting is open from 08:00 to 21:00 every day.";
        }

        if (text.Contains("reservation") || text.Contains("book") || text.Contains("dat ban"))
        {
            return "You can reserve a table from the Reservation tab. Pick date, time, guest count, then choose an available table.";
        }

        if (text.Contains("best") || text.Contains("popular") || text.Contains("ban chay"))
        {
            return "Best sellers for demo: Bac Xiu, Matcha Latte, Cheese Cake, and Loaf Combo.";
        }

        if (text.Contains("location") || text.Contains("address") || text.Contains("dia chi"))
        {
            return "Open the Store Location screen to see our address and directions.";
        }

        return "Thanks for messaging Loaf'NCatting. For demo, try asking about opening hours, reservation, best-selling items, or location.";
    }
}



