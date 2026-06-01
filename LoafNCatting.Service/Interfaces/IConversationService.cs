using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IConversationService
{
    Task<ConversationDto> GetOrCreateConversationAsync(int userId);
}

