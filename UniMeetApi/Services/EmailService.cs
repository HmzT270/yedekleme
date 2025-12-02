using System.Net;
using System.Net.Mail;

namespace UniMeetApi.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        string GenerateEventNotificationEmail(string userName, string clubName, Event eventDetails, string actionUrl);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpHost = _config["Smtp:Host"];
                var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
                var smtpEnableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");
                var smtpUsername = _config["Smtp:Username"];
                var smtpPassword = _config["Smtp:Password"];
                var fromAddress = _config["Smtp:FromAddress"];
                var fromName = _config["Smtp:FromName"];

                using var message = new MailMessage
                {
                    From = new MailAddress(fromAddress ?? "noreply@unimeet.local", fromName ?? "UniMeet"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                message.To.Add(to);

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = smtpEnableSsl,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("E-posta başarıyla gönderildi: {To}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta gönderimi başarısız: {To}", to);
                return false;
            }
        }

        public string GenerateEventNotificationEmail(string userName, string clubName, Event eventDetails, string actionUrl)
        {
            var clientBaseUrl = _config["Client:BaseUrl"] ?? "http://localhost:5174";
            var eventViewUrl = $"{clientBaseUrl}/home?openEventId={eventDetails.EventId}";
            
            // UTC'den Türkiye saatine çevir (UTC+3)
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            var startAtLocal = TimeZoneInfo.ConvertTimeFromUtc(eventDetails.StartAt, turkeyTimeZone);
            var endAtLocal = eventDetails.EndAt.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(eventDetails.EndAt.Value, turkeyTimeZone) : (DateTime?)null;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: white; padding: 30px; border-radius: 0 0 5px 5px; }}
        .event-details {{ background-color: #f5f5f5; padding: 15px; margin: 20px 0; border-left: 4px solid #4CAF50; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 10px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Yeni Etkinlik Bildirimi</h1>
        </div>
        <div class=""content"">
            <p>Merhaba <strong>{userName}</strong>,</p>
            <p>Takip ettiğiniz <strong>{clubName}</strong> kulübü yeni bir etkinlik oluşturdu!</p>
            
            <div class=""event-details"">
                <h2>{eventDetails.Title}</h2>
                <p><strong>📅 Tarih:</strong> {startAtLocal:dd.MM.yyyy HH:mm}</p>
                <p><strong>📍 Konum:</strong> {eventDetails.Location}</p>
                {(endAtLocal.HasValue ? $"<p><strong>⏱️ Bitiş:</strong> {endAtLocal.Value:dd.MM.yyyy HH:mm}</p>" : "")}
                <p><strong>👥 Kontenjan:</strong> {eventDetails.Quota} kişi</p>
                {(!string.IsNullOrEmpty(eventDetails.Description) ? $"<p><strong>📝 Açıklama:</strong> {eventDetails.Description}</p>" : "")}
            </div>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{eventViewUrl}"" class=""button"">Etkinliği Görüntüle</a>
            </div>
            
            <p style=""margin-top: 30px; font-size: 14px; color: #666;"">
                Bu bildirimi aldınız çünkü <strong>{clubName}</strong> kulübüne üyesiniz. 
                Bildirimlerinizi yönetmek için <a href=""{clientBaseUrl}/settings"">hesap ayarlarınızı</a> ziyaret edebilirsiniz.
            </p>
        </div>
        <div class=""footer"">
            <p>© 2025 UniMeet - Üniversite Etkinlik Platformu</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
