using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace UniMeetApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Tüm endpoint'ler authentication gerektiriyor
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext db, ILogger<AdminController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // İstek/yanıt tipleri
        public record UserDto(int UserId, string Email, string FullName, string Role, bool IsActive);
        public record UpdateUserRoleReq(int UserId, string NewRole);

        // ✅ Tüm kullanıcıları getir (sadece Admin)
        [HttpGet("users")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            // Kullanıcının role'ünü kontrol et
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim != "Admin")
                return Forbid("Sadece admin kullanıcılar bu işlemi yapabilir.");

            try
            {
                var users = await _db.Users
                    .OrderBy(u => u.Email)
                    .Select(u => new UserDto(
                        u.UserId,
                        u.Email,
                        u.FullName,
                        u.Role.ToString(),
                        u.IsActive
                    ))
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar alınırken hata oluştu.");
                return StatusCode(500, new { message = "Kullanıcılar alınırken hata oluştu." });
            }
        }

        // ✅ Kullanıcı rolünü güncelle (sadece Admin)
        [HttpPost("users/{userId}/update-role")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleReq req)
        {
            // Kullanıcının role'ünü kontrol et
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim != "Admin")
                return Forbid("Sadece admin kullanıcılar bu işlemi yapabilir.");

            if (req is null || string.IsNullOrWhiteSpace(req.NewRole))
                return BadRequest(new { message = "Geçerli bir rol giriniz." });

            // Geçerli rol kontrol et
            if (!Enum.TryParse<UserRole>(req.NewRole, true, out var newRole))
                return BadRequest(new { message = $"Geçersiz rol: {req.NewRole}. Geçerli roller: Member, Manager, Admin" });

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user is null)
                    return NotFound(new { message = "Kullanıcı bulunamadı." });

                var oldRole = user.Role;
                user.Role = newRole;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Kullanıcı {user.Email} rolü {oldRole} → {newRole} olarak güncellendi.");
                return Ok(new { message = $"Rol başarıyla güncellendi: {oldRole} → {newRole}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol güncellenirken hata oluştu.");
                return StatusCode(500, new { message = "Rol güncellenirken hata oluştu." });
            }
        }

        // ✅ Kullanıcının aktif/pasif durumunu değiştir (sadece Admin)
        [HttpPost("users/{userId}/toggle-active")]
        public async Task<IActionResult> ToggleUserActive(int userId)
        {
            // Kullanıcının role'ünü kontrol et
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim != "Admin")
                return Forbid("Sadece admin kullanıcılar bu işlemi yapabilir.");

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user is null)
                    return NotFound(new { message = "Kullanıcı bulunamadı." });

                user.IsActive = !user.IsActive;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Kullanıcı {user.Email} aktif durumu: {user.IsActive}");
                return Ok(new { message = $"Kullanıcı {(user.IsActive ? "aktifleştirildi" : "pasifleştirildi")}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı durumu değiştirilirken hata oluştu.");
                return StatusCode(500, new { message = "Kullanıcı durumu değiştirilirken hata oluştu." });
            }
        }

        // === Bildirim Logları ===
        
        public record NotificationLogDto(
            int NotificationLogId,
            int UserId,
            string UserEmail,
            int? EventId,
            string? EventTitle,
            int? ClubId,
            string? ClubName,
            string Type,
            string Status,
            string RecipientEmail,
            string Subject,
            string? ErrorMessage,
            int RetryCount,
            DateTime CreatedAt,
            DateTime? SentAt
        );

        [HttpGet("notifications")]
        public async Task<ActionResult<object>> GetNotificationLogs(
            [FromQuery] string? status = null,
            [FromQuery] int? userId = null,
            [FromQuery] int? clubId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim != "Admin")
                return Forbid("Sadece admin kullanıcılar bu işlemi yapabilir.");

            try
            {
                var query = _db.NotificationLogs
                    .Include(n => n.User)
                    .Include(n => n.Event)
                    .Include(n => n.Club)
                    .AsQueryable();

                // Filtreleme
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<NotificationStatus>(status, true, out var statusEnum))
                    query = query.Where(n => n.Status == statusEnum);

                if (userId.HasValue)
                    query = query.Where(n => n.UserId == userId.Value);

                if (clubId.HasValue)
                    query = query.Where(n => n.ClubId == clubId.Value);

                var totalCount = await query.CountAsync();

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationLogDto(
                        n.NotificationLogId,
                        n.UserId,
                        n.User != null ? n.User.Email : "Bilinmiyor",
                        n.EventId,
                        n.Event != null ? n.Event.Title : null,
                        n.ClubId,
                        n.Club != null ? n.Club.Name : null,
                        n.Type.ToString(),
                        n.Status.ToString(),
                        n.RecipientEmail,
                        n.Subject,
                        n.ErrorMessage,
                        n.RetryCount,
                        n.CreatedAt,
                        n.SentAt
                    ))
                    .ToListAsync();

                var statistics = new
                {
                    Total = totalCount,
                    Sent = await _db.NotificationLogs.CountAsync(n => n.Status == NotificationStatus.Sent),
                    Pending = await _db.NotificationLogs.CountAsync(n => n.Status == NotificationStatus.Pending),
                    Failed = await _db.NotificationLogs.CountAsync(n => n.Status == NotificationStatus.Failed),
                    Retry = await _db.NotificationLogs.CountAsync(n => n.Status == NotificationStatus.Retry)
                };

                return Ok(new
                {
                    Notifications = notifications,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Statistics = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim logları getirilirken hata oluştu.");
                return StatusCode(500, new { message = "Bildirim logları getirilirken hata oluştu." });
            }
        }

        [HttpPost("notifications/retry-failed")]
        public async Task<ActionResult> RetryFailedNotifications()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim != "Admin")
                return Forbid("Sadece admin kullanıcılar bu işlemi yapabilir.");

            try
            {
                var failedNotifications = await _db.NotificationLogs
                    .Where(n => n.Status == NotificationStatus.Failed && n.RetryCount < 3)
                    .ToListAsync();

                foreach (var notification in failedNotifications)
                {
                    notification.Status = NotificationStatus.Retry;
                    notification.ErrorMessage = null;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation($"{failedNotifications.Count} başarısız bildirim yeniden deneme kuyruğuna eklendi.");
                return Ok(new { message = $"{failedNotifications.Count} bildirim yeniden deneme kuyruğuna eklendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Başarısız bildirimler yeniden denenirken hata oluştu.");
                return StatusCode(500, new { message = "İşlem sırasında hata oluştu." });
            }
        }
    }
}
