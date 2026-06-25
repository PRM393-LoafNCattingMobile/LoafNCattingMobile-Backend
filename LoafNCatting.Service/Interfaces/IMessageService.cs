using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IMessageService
{
    Task<List<MessageDto>?> GetMessagesAsync(int conversationId, int requestingUserId);
    Task<List<MessageDto>?> SendMessageAsync(CreateMessageDto request, int requestingUserId);
}

