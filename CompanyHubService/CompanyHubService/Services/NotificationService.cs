using CompanyHubService.Data;
using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.EntityFrameworkCore;

public class NotificationService
{
    private readonly CompanyHubDbContext _dbContext;

    public NotificationService(CompanyHubDbContext dbContext, EmailService emailService)
    {
        _dbContext = dbContext;
    }

    public async Task CreateNotificationAsync(string recipientId, string message)
    {
        var notification = new Notification
        {
            RecipientId = recipientId,
            Message = message
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
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
