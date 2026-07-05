using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IMessageService
{
    Task<List<MessageDto>?> GetMessagesAsync(int conversationId, int requestingUserId);
    Task<List<MessageDto>?> GetMessagesForSupportAsync(int conversationId);
    Task<List<MessageDto>?> SendMessageAsync(CreateMessageDto request, int requestingUserId);
    Task<List<MessageDto>?> SendSupportMessageAsync(int conversationId, SupportMessageDto request, int staffUserId);
}

