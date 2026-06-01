using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class NotificationService(INotificationRepository notifications) : INotificationService
{
    public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId)
    {
        var items = await notifications.GetByUserIdAsync(userId);
        return items.Select(CafeDtoMapper.ToNotificationDto).ToList();
    }

    public async Task<bool> MarkNotificationReadAsync(int notificationId)
    {
        var notification = await notifications.GetByIdAsync(notificationId);
        if (notification is null)
        {
            return false;
        }

        notification.IsRead = true;
        await notifications.SaveChangesAsync();
        return true;
    }
}



