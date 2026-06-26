using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(int userId);
    Task<bool> MarkNotificationReadAsync(int notificationId, int userId);
}

