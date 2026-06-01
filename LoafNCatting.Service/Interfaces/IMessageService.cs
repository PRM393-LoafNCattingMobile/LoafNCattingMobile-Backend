using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IMessageService
{
    Task<List<MessageDto>> GetMessagesAsync(int conversationId);
    Task<List<MessageDto>> SendMessageAsync(CreateMessageDto request);
}

