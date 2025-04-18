using CompanyHubService.Data;
using CompanyHubService.Hubs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class NotificationService
{
    private readonly CompanyHubDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(CompanyHubDbContext dbContext, IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task CreateNotificationAsync(string recipientId, string message, string notificationType, string url)
    {
        var notification = new Notification
        {
            RecipientId = recipientId,
            Message = message,
            NotificationType = notificationType,
            Url = url
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        await _hubContext.Clients.User(recipientId)
        .SendAsync("ReceiveNotification", new
        {
            notification.NotificationId,
            notification.Message,
            notification.NotificationType,
            notification.Url,
            notification.CreatedAt
        });
    }

    // Optional: Get unread notifications for a user
    public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
    {
        return await _dbContext.Notifications
            .Where(n => n.RecipientId == userId && n.ReadAt == null)
            .ToListAsync();
    }

    // Optional: Mark notification as read
    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _dbContext.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
}
