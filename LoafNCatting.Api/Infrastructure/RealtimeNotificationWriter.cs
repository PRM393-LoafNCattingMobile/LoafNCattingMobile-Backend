using LoafNCatting.Api.Hubs;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LoafNCatting.Api.Infrastructure;

internal sealed class RealtimeNotificationWriter(
    NotificationService notifications,
    IHubContext<NotificationHub> hub) : INotificationWriter
{
    public async Task<NotificationDto?> CreateAsync(
        int? userId,
        string title,
        string content,
        string type)
    {
        var notification = await notifications.CreateAsync(userId, title, content, type);
        if (notification?.UserId is int notificationUserId)
        {
            await hub.Clients
                .Group(NotificationHub.UserGroup(notificationUserId))
                .SendAsync(NotificationHub.NotificationCreatedEvent, notification);
        }

        return notification;
    }
}
