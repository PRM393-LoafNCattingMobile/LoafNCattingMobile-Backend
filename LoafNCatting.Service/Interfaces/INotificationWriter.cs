using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface INotificationWriter
{
    Task<NotificationDto?> CreateAsync(int? userId, string title, string content, string type);
}

