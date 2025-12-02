using Microsoft.EntityFrameworkCore;

namespace UniMeetApi.Services
{
    public interface IEventNotificationService
    {
        Task NotifyClubMembersAsync(int eventId, int clubId);
        Task ProcessPendingNotificationsAsync();
    }

    public class EventNotificationService : IEventNotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventNotificationService> _logger;

        public EventNotificationService(
            IServiceScopeFactory scopeFactory,
            ILogger<EventNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task NotifyClubMembersAsync(int eventId, int clubId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Etkinlik bilgisini al
                var eventDetails = await db.Events.FindAsync(eventId);
                if (eventDetails == null)
                {
                    _logger.LogWarning("Etkinlik bulunamadı: {EventId}", eventId);
                    return;
                }

                // Kulüp bilgisini al
                var club = await db.Clubs.FindAsync(clubId);
                if (club == null)
                {
                    _logger.LogWarning("Kulüp bulunamadı: {ClubId}", clubId);
                    return;
                }

                // Kulübün üyelerini al (bildirim tercihlerini etkin olanlar)
                var members = await db.ClubMembers
                    .Include(cm => cm.User)
                    .Where(cm => cm.ClubId == clubId 
                        && cm.User != null 
                        && cm.User.EmailNotificationsEnabled 
                        && cm.User.EventNotificationsEnabled
                        && cm.User.IsActive)
                    .Select(cm => cm.User!)
                    .ToListAsync();

                _logger.LogInformation("Kulüp {ClubName} için {Count} üyeye bildirim gönderilecek", 
                    club.Name, members.Count);

                // Her üye için bildirim kaydı oluştur
                foreach (var member in members)
                {
                    var notification = new NotificationLog
                    {
                        UserId = member.UserId,
                        EventId = eventId,
                        ClubId = clubId,
                        Type = NotificationType.EventCreated,
                        Status = NotificationStatus.Pending,
                        RecipientEmail = member.Email,
                        Subject = $"🎉 Yeni Etkinlik: {eventDetails.Title}",
                        Body = "", // Email service tarafından oluşturulacak
                        CreatedAt = DateTime.UtcNow
                    };

                    db.NotificationLogs.Add(notification);
                }

                await db.SaveChangesAsync();
                _logger.LogInformation("Bildirim kayıtları oluşturuldu, toplam: {Count}", members.Count);

                // Bildirimleri arka planda gönder (fire and forget)
                _ = Task.Run(() => ProcessPendingNotificationsAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim oluşturulurken hata: EventId={EventId}, ClubId={ClubId}", 
                    eventId, clubId);
            }
        }

        public async Task ProcessPendingNotificationsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Bekleyen bildirimleri al (en fazla 3 deneme yapılmış olanları hariç tut)
                var pendingNotifications = await db.NotificationLogs
                    .Include(n => n.User)
                    .Include(n => n.Event)
                    .Include(n => n.Club)
                    .Where(n => (n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Retry)
                        && n.RetryCount < 3)
                    .OrderBy(n => n.CreatedAt)
                    .Take(50) // Toplu işlem sınırı
                    .ToListAsync();

                _logger.LogInformation("İşlenecek bildirim sayısı: {Count}", pendingNotifications.Count);

                foreach (var notification in pendingNotifications)
                {
                    try
                    {
                        if (notification.User == null || notification.Event == null || notification.Club == null)
                        {
                            _logger.LogWarning("Bildirim için gerekli veri eksik: {NotificationId}", 
                                notification.NotificationLogId);
                            notification.Status = NotificationStatus.Failed;
                            notification.ErrorMessage = "İlişkili veri bulunamadı";
                            continue;
                        }

                        // Email içeriğini oluştur
                        var emailBody = emailService.GenerateEventNotificationEmail(
                            notification.User.FullName,
                            notification.Club.Name,
                            notification.Event,
                            $"http://localhost:5173/events/{notification.EventId}"
                        );

                        notification.Body = emailBody;

                        // Email gönder
                        var success = await emailService.SendEmailAsync(
                            notification.RecipientEmail,
                            notification.Subject,
                            emailBody,
                            isHtml: true
                        );

                        if (success)
                        {
                            notification.Status = NotificationStatus.Sent;
                            notification.SentAt = DateTime.UtcNow;
                            _logger.LogInformation("Bildirim gönderildi: {Email} - {Subject}", 
                                notification.RecipientEmail, notification.Subject);
                        }
                        else
                        {
                            notification.RetryCount++;
                            notification.Status = notification.RetryCount >= 3 
                                ? NotificationStatus.Failed 
                                : NotificationStatus.Retry;
                            notification.ErrorMessage = "E-posta gönderilemedi";
                            _logger.LogWarning("Bildirim gönderilemedi (Deneme {Retry}): {Email}", 
                                notification.RetryCount, notification.RecipientEmail);
                        }

                        // Her 5 bildirimde bir kaydet (performans için)
                        if (pendingNotifications.IndexOf(notification) % 5 == 0)
                        {
                            await db.SaveChangesAsync();
                        }

                        // Rate limiting - saniyede 2 mail
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        notification.RetryCount++;
                        notification.Status = notification.RetryCount >= 3 
                            ? NotificationStatus.Failed 
                            : NotificationStatus.Retry;
                        notification.ErrorMessage = ex.Message;
                        _logger.LogError(ex, "Bildirim gönderilirken hata: {NotificationId}", 
                            notification.NotificationLogId);
                    }
                }

                await db.SaveChangesAsync();
                _logger.LogInformation("Bildirim işleme tamamlandı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim işleme sırasında genel hata");
            }
        }
    }
}
