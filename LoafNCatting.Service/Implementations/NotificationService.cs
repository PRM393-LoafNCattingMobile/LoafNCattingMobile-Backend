using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Service.Implementations;

public class NotificationService(INotificationRepository notifications) : INotificationService, INotificationWriter
{
    public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId)
    {
        var items = await notifications.GetByUserIdAsync(userId);
        return items.Select(CafeDtoMapper.ToNotificationDto).ToList();
    }

    public async Task<bool> MarkNotificationReadAsync(int notificationId, int userId)
    {
        var notification = await notifications.GetByIdAsync(notificationId);
        if (notification is null || notification.UserId != userId)
        {
            return false;
        }

        notification.IsRead = true;
        await notifications.SaveChangesAsync();
        return true;
    }

    public async Task<NotificationDto?> CreateAsync(int? userId, string title, string content, string type)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        var notification = new Notification
        {
            UserId = userId.Value,
            Title = title.Trim(),
            Content = content.Trim(),
            Type = type.Trim()
        };

        await notifications.AddAsync(notification);
        await notifications.SaveChangesAsync();
        return CafeDtoMapper.ToNotificationDto(notification);
    }
}



