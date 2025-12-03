using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniMeetApi.Services;

namespace UniMeetApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEventNotificationService _notificationService;
        private readonly IRecommendationService _recommendationService;
        private readonly IRecommendationProxyService _recommendationProxy;
        
        public EventsController(
            AppDbContext db, 
            IEventNotificationService notificationService, 
            IRecommendationService recommendationService,
            IRecommendationProxyService recommendationProxy)
        {
            _db = db;
            _notificationService = notificationService;
            _recommendationService = recommendationService;
            _recommendationProxy = recommendationProxy;
        }

        // === DTOs ===
        public record EventDto(
            int EventId,
            string Title,
            string Location,
            DateTime StartAt,
            DateTime? EndAt,
            int Quota,
            int ClubId,
            string? ClubName,
            string? Description,
            bool IsCancelled,
            bool IsPublic,
            int AttendeesCount,
            bool IsMember,
            bool IsJoined
        );

        public record RecommendedEventDto(
            int EventId,
            string Title,
            string Location,
            DateTime StartAt,
            DateTime? EndAt,
            int Quota,
            int ClubId,
            string? ClubName,
            string? Description,
            bool IsCancelled,
            bool IsPublic,
            int AttendeesCount,
            bool IsMember,
            bool IsJoined,
            double Score,
            string RecommendationReason,
            string ReasonDetails,
            Dictionary<string, double>? ReasonFeatures
        );

        public record CreateEventRequest(
            string Title,
            string Location,
            DateTime StartAt,
            DateTime? EndAt,
            int Quota,
            int ClubId,
            string? Description
        );

        public record UpdateEventRequest(
            string Title,
            string Location,
            DateTime StartAt,
            DateTime? EndAt,
            int Quota,
            int ClubId,
            string? Description,
            bool? IsCancelled
        );

        // === Helpers ===
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out userId);
        }

        private static bool IsAdmin(User u) => u.Role == UserRole.Admin;
        private static bool IsManager(User u) => u.Role == UserRole.Manager;

        // Manager sadece kendi kulübü için işlem yapabilir (Create/Update/Delete)
        private static bool ManagerOwnsClub(User u, int clubId)
            => IsManager(u) && u.ManagedClubId.HasValue && u.ManagedClubId.Value == clubId;

        // === Everyone can view ===
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<EventDto>>> GetAll([FromQuery] bool includeCancelled = false)
        {
            TryGetUserId(out var userId);
            
            var query = _db.Events.AsNoTracking();

            if (!includeCancelled)
                query = query.Where(e => !e.IsCancelled);

            var events = await query.OrderBy(e => e.StartAt).ToListAsync();
            var userClubIds = userId > 0 ? await _db.ClubMembers.Where(m => m.UserId == userId).Select(m => m.ClubId).ToListAsync() : new List<int>();
            var userEventIds = userId > 0 ? await _db.Set<EventAttendee>().Where(a => a.UserId == userId).Select(a => a.EventId).ToListAsync() : new List<int>();
            var userClubSet = new HashSet<int>(userClubIds);
            var userEventSet = new HashSet<int>(userEventIds);

            var list = new List<EventDto>();
            foreach (var e in events)
            {
                var clubName = await _db.Clubs.Where(c => c.ClubId == e.ClubId).Select(c => (string?)c.Name).FirstOrDefaultAsync();
                var attendeesCount = await _db.Set<EventAttendee>().CountAsync(a => a.EventId == e.EventId);
                var isMember = userClubSet.Contains(e.ClubId);
                var isJoined = userEventSet.Contains(e.EventId);

                list.Add(new EventDto(
                    e.EventId,
                    e.Title,
                    e.Location,
                    e.StartAt,
                    e.EndAt,
                    e.Quota,
                    e.ClubId,
                    clubName,
                    e.Description,
                    e.IsCancelled,
                    e.IsPublic,
                    attendeesCount,
                    isMember,
                    isJoined
                ));
            }

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<EventDto>> GetById(int id)
        {
            TryGetUserId(out var userId);
            
            var e = await _db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.EventId == id);
            if (e is null) return NotFound("Etkinlik bulunamadı.");

            var clubName = await _db.Clubs
                .Where(c => c.ClubId == e.ClubId)
                .Select(c => (string?)c.Name)
                .FirstOrDefaultAsync();

            var attendeesCount = await _db.Set<EventAttendee>().CountAsync(a => a.EventId == e.EventId);
            var isMember = userId > 0 ? await _db.ClubMembers.AnyAsync(m => m.UserId == userId && m.ClubId == e.ClubId) : false;
            var isJoined = userId > 0 ? await _db.Set<EventAttendee>().AnyAsync(a => a.UserId == userId && a.EventId == e.EventId) : false;

            var dto = new EventDto(
                e.EventId,
                e.Title,
                e.Location,
                e.StartAt,
                e.EndAt,
                e.Quota,
                e.ClubId,
                clubName,
                e.Description,
                e.IsCancelled,
                e.IsPublic,
                attendeesCount,
                isMember,
                isJoined
            );

            return Ok(dto);
        }

        // === ManagersOnly (Manager/Admin) ===
        [HttpPost]
        [Authorize(Policy = "ManagersOnly")]
        public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest("Etkinlik adı zorunludur.");
            if (string.IsNullOrWhiteSpace(req.Location))
                return BadRequest("Etkinlik yeri zorunludur.");
            if (req.Quota < 1)
                return BadRequest("Kontenjan en az 1 olmalıdır.");
            if (req.EndAt.HasValue && req.EndAt.Value < req.StartAt)
                return BadRequest("Bitiş, başlangıçtan önce olamaz.");

            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null || !user.IsActive) return Unauthorized("Kullanıcı bulunamadı veya pasif.");

            // ✅ KURAL: Admin serbest; Manager sadece kendi kulübü için
            if (!IsAdmin(user) && !ManagerOwnsClub(user, req.ClubId))
                return Forbid($"Yalnızca yöneticisi olduğunuz kulüp için etkinlik oluşturabilirsiniz. (Sizin kulübünüz: {user.ManagedClubId?.ToString() ?? "tanımsız"})");

            var entity = new Event
            {
                Title = req.Title.Trim(),
                Location = req.Location.Trim(),
                StartAt = DateTime.SpecifyKind(req.StartAt, DateTimeKind.Utc),
                EndAt = req.EndAt.HasValue ? DateTime.SpecifyKind(req.EndAt.Value, DateTimeKind.Utc) : null,
                Quota = req.Quota,
                ClubId = req.ClubId,
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                IsCancelled = false,
                CreatedByUserId = user.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Events.Add(entity);
            await _db.SaveChangesAsync();

            // 🔔 Kulüp üyelerine bildirim gönder (asenkron)
            _ = Task.Run(() => _notificationService.NotifyClubMembersAsync(entity.EventId, entity.ClubId));

            var clubName = await _db.Clubs
                .Where(c => c.ClubId == entity.ClubId)
                .Select(c => (string?)c.Name)
                .FirstOrDefaultAsync();

            var dto = new EventDto(
                entity.EventId,
                entity.Title,
                entity.Location,
                entity.StartAt,
                entity.EndAt,
                entity.Quota,
                entity.ClubId,
                clubName,
                entity.Description,
                entity.IsCancelled,
                entity.IsPublic,
                0,
                false,
                false
            );

            return CreatedAtAction(nameof(GetById), new { id = entity.EventId }, dto);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "ManagersOnly")]
        public async Task<ActionResult<EventDto>> Update(int id, [FromBody] UpdateEventRequest req)
        {
            var e = await _db.Events.FirstOrDefaultAsync(x => x.EventId == id);
            if (e is null) return NotFound("Etkinlik bulunamadı.");

            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest("Etkinlik adı zorunludur.");
            if (string.IsNullOrWhiteSpace(req.Location))
                return BadRequest("Etkinlik yeri zorunludur.");
            if (req.Quota < 1)
                return BadRequest("Kontenjan en az 1 olmalıdır.");
            if (req.EndAt.HasValue && req.EndAt.Value < req.StartAt)
                return BadRequest("Bitiş, başlangıçtan önce olamaz.");

            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null || !user.IsActive) return Unauthorized("Kullanıcı bulunamadı veya pasif.");

            // ✅ KURAL: Admin serbest; Manager sadece KENDİ kulübüne ait etkinliği güncelleyebilir
            var targetClubId = req.ClubId; // kulübü değiştirmeye de izin veriyorsak bu değer önemli
            if (!IsAdmin(user) && !ManagerOwnsClub(user, targetClubId))
                return Forbid("Yalnızca yöneticisi olduğunuz kulüp için güncelleme yapabilirsiniz.");

            e.Title = req.Title.Trim();
            e.Location = req.Location.Trim();
            e.StartAt = DateTime.SpecifyKind(req.StartAt, DateTimeKind.Utc);
            e.EndAt = req.EndAt.HasValue ? DateTime.SpecifyKind(req.EndAt.Value, DateTimeKind.Utc) : null;
            e.Quota = req.Quota;
            e.ClubId = req.ClubId;
            e.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();

            if (req.IsCancelled.HasValue)
                e.IsCancelled = req.IsCancelled.Value;

            await _db.SaveChangesAsync();

            var clubName = await _db.Clubs
                .Where(c => c.ClubId == e.ClubId)
                .Select(c => (string?)c.Name)
                .FirstOrDefaultAsync();

            var dto = new EventDto(
                e.EventId,
                e.Title,
                e.Location,
                e.StartAt,
                e.EndAt,
                e.Quota,
                e.ClubId,
                clubName,
                e.Description,
                e.IsCancelled,
                e.IsPublic,
                0,
                false,
                false
            );

            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "ManagersOnly")]
        public async Task<IActionResult> Cancel(int id)
        {
            var e = await _db.Events.FirstOrDefaultAsync(x => x.EventId == id);
            if (e is null) return NotFound("Etkinlik bulunamadı.");

            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null || !user.IsActive) return Unauthorized("Kullanıcı bulunamadı veya pasif.");

            // ✅ KURAL: Admin serbest; Manager sadece KENDİ kulübüne ait etkinliği iptal edebilir
            if (!IsAdmin(user) && !ManagerOwnsClub(user, e.ClubId))
                return Forbid("Yalnızca yöneticisi olduğunuz kulüp için iptal işlemi yapabilirsiniz.");

            e.IsCancelled = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // === ✅ YENİ: 24 saat içindeki etkinlikler (kullanıcının kulüpleri) ===
        [HttpGet("upcoming")]
        [Authorize]
        public async Task<ActionResult<List<EventDto>>> Upcoming([FromQuery] bool includeCancelled = false)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            // Kullanıcının üye olduğu kulüpler
            var myClubIds = await _db.ClubMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ClubId)
                .ToListAsync();

            if (myClubIds.Count == 0)
                return Ok(new List<EventDto>());

            var now = DateTime.UtcNow;
            var until = now.AddHours(24);

            var query = _db.Events.AsNoTracking()
                .Where(e =>
                    myClubIds.Contains(e.ClubId) &&
                    e.StartAt > now &&
                    e.StartAt <= until
                );

            if (!includeCancelled)
                query = query.Where(e => !e.IsCancelled);

            var myClubSet = new HashSet<int>(myClubIds);
            var userEventIds = await _db.Set<EventAttendee>().Where(a => a.UserId == userId).Select(a => a.EventId).ToListAsync();
            var userEventSet = new HashSet<int>(userEventIds);
            var events = await query.OrderBy(e => e.StartAt).ToListAsync();
            var list = new List<EventDto>();
            
            foreach (var e in events)
            {
                var clubName = await _db.Clubs.Where(c => c.ClubId == e.ClubId).Select(c => (string?)c.Name).FirstOrDefaultAsync();
                var attendeesCount = await _db.Set<EventAttendee>().CountAsync(a => a.EventId == e.EventId);
                var isMember = myClubSet.Contains(e.ClubId);
                var isJoined = userEventSet.Contains(e.EventId);
                
                list.Add(new EventDto(
                    e.EventId,
                    e.Title,
                    e.Location,
                    e.StartAt,
                    e.EndAt,
                    e.Quota,
                    e.ClubId,
                    clubName,
                    e.Description,
                    e.IsCancelled,
                    e.IsPublic,
                    attendeesCount,
                    isMember,
                    isJoined
                ));
            }

            return Ok(list);
        }

        // === ✅ Takip edilen kulüplerin etkinlikleri (Home feed) ===
        [HttpGet("feed")]
        [Authorize]
        public async Task<ActionResult<List<EventDto>>> Feed(
            [FromQuery] bool upcomingOnly = true,
            [FromQuery] bool includeCancelled = false)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            // Kullanıcının takip ettiği kulüpler
            var myClubIds = await _db.ClubMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ClubId)
                .ToListAsync();

            // Hiç kulüp takip etmiyorsa boş liste dön
            if (myClubIds.Count == 0) return Ok(new List<EventDto>());

            var now = DateTime.UtcNow;

            var query = _db.Events.AsNoTracking()
                .Where(e => myClubIds.Contains(e.ClubId));

            if (!includeCancelled)
                query = query.Where(e => !e.IsCancelled);

            if (upcomingOnly)
                query = query.Where(e => e.StartAt >= now);

            var myClubSet = new HashSet<int>(myClubIds);
            var userEventIds = await _db.Set<EventAttendee>().Where(a => a.UserId == userId).Select(a => a.EventId).ToListAsync();
            var userEventSet = new HashSet<int>(userEventIds);
            var events = await query.OrderBy(e => e.StartAt).ToListAsync();
            var list = new List<EventDto>();
            
            foreach (var e in events)
            {
                var clubName = await _db.Clubs.Where(c => c.ClubId == e.ClubId).Select(c => (string?)c.Name).FirstOrDefaultAsync();
                var attendeesCount = await _db.Set<EventAttendee>().CountAsync(a => a.EventId == e.EventId);
                var isMember = myClubSet.Contains(e.ClubId);
                var isJoined = userEventSet.Contains(e.EventId);
                
                list.Add(new EventDto(
                    e.EventId,
                    e.Title,
                    e.Location,
                    e.StartAt,
                    e.EndAt,
                    e.Quota,
                    e.ClubId,
                    clubName,
                    e.Description,
                    e.IsCancelled,
                    e.IsPublic,
                    attendeesCount,
                    isMember,
                    isJoined
                ));
            }

            return Ok(list);
        }

        // === Katılım: Join / Leave ===
        [HttpPost("{id:int}/join")]
        [Authorize]
        public async Task<IActionResult> Join(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            var ev = await _db.Events.FirstOrDefaultAsync(x => x.EventId == id);
            if (ev is null) return NotFound("Etkinlik bulunamadı.");

            var exists = await _db.Set<EventAttendee>().FirstOrDefaultAsync(x => x.UserId == userId && x.EventId == id);
            if (exists != null) return NoContent(); // Zaten katılmış

            _db.Set<EventAttendee>().Add(new EventAttendee { UserId = userId, EventId = id });
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}/join")]
        [Authorize]
        public async Task<IActionResult> Leave(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            var attendee = await _db.Set<EventAttendee>().FirstOrDefaultAsync(x => x.UserId == userId && x.EventId == id);
            if (attendee is null) return NoContent();

            _db.Set<EventAttendee>().Remove(attendee);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // === Favorilere ekle / kaldır / listele ===
        [HttpPost("{id:int}/favorite")]
        [Authorize]
        public async Task<IActionResult> AddFavorite(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.EventId == id);
            if (ev is null) return NotFound("Etkinlik bulunamadı.");

            var exists = await _db.Set<FavoriteEvent>().FindAsync(userId, id);
            if (exists != null) return NoContent();

            _db.Set<FavoriteEvent>().Add(new FavoriteEvent { UserId = userId, EventId = id, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}/favorite")]
        [Authorize]
        public async Task<IActionResult> RemoveFavorite(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            var fav = await _db.Set<FavoriteEvent>().FindAsync(userId, id);
            if (fav is null) return NoContent();

            _db.Set<FavoriteEvent>().Remove(fav);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("favorites")]
        [Authorize]
        public async Task<ActionResult<List<EventDto>>> MyFavorites()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            var events = await _db.FavoriteEvents
                .Where(f => f.UserId == userId)
                .Join(_db.Events, f => f.EventId, e => e.EventId, (f, e) => e)
                .Where(e => !e.IsCancelled)
                .OrderBy(e => e.StartAt)
                .ToListAsync();

            var userClubIds = await _db.ClubMembers.Where(m => m.UserId == userId).Select(m => m.ClubId).ToListAsync();
            var userClubSet = new HashSet<int>(userClubIds);
            var userEventIds = await _db.Set<EventAttendee>().Where(a => a.UserId == userId).Select(a => a.EventId).ToListAsync();
            var userEventSet = new HashSet<int>(userEventIds);

            var list = new List<EventDto>();
            foreach (var e in events)
            {
                var clubName = await _db.Clubs.Where(c => c.ClubId == e.ClubId).Select(c => (string?)c.Name).FirstOrDefaultAsync();
                var attendeesCount = await _db.Set<EventAttendee>().CountAsync(a => a.EventId == e.EventId);
                var isMember = userClubSet.Contains(e.ClubId);
                var isJoined = userEventSet.Contains(e.EventId);
                
                list.Add(new EventDto(
                    e.EventId,
                    e.Title,
                    e.Location,
                    e.StartAt,
                    e.EndAt,
                    e.Quota,
                    e.ClubId,
                    clubName,
                    e.Description,
                    e.IsCancelled,
                    e.IsPublic,
                    attendeesCount,
                    isMember,
                    isJoined
                ));
            }

            return Ok(list);
        }

        // === ✅ YENİ: Takip edilen kulüplere benzer kulüplerden etkinlik önerileri ===
        [HttpGet("recommendations")]
        [Authorize]
        public async Task<ActionResult<List<RecommendedEventDto>>> GetRecommendations()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            // YENİ: Python mikroservis proxy'sini kullan - detaylı sonuçlarla
            var recommendationResponse = await _recommendationProxy.GetDetailedRecommendationsAsync(userId, limit: 10);

            var userClubIds = await _db.ClubMembers.Where(m => m.UserId == userId).Select(m => m.ClubId).ToListAsync();
            var userClubSet = new HashSet<int>(userClubIds);
            var userEventIds = await _db.Set<EventAttendee>().Where(a => a.UserId == userId).Select(a => a.EventId).ToListAsync();
            var userEventSet = new HashSet<int>(userEventIds);

            var eventIds = recommendationResponse.Recommendations.Select(r => r.EventId).ToList();
            var events = await _db.Events.Where(e => eventIds.Contains(e.EventId)).ToDictionaryAsync(e => e.EventId);
            var clubNames = await _db.Clubs.Where(c => events.Values.Select(e => e.ClubId).Contains(c.ClubId))
                .ToDictionaryAsync(c => c.ClubId, c => c.Name);
            var attendeeCounts = await _db.Set<EventAttendee>()
                .Where(a => eventIds.Contains(a.EventId))
                .GroupBy(a => a.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventId, x => x.Count);

            var list = new List<RecommendedEventDto>();
            foreach (var recommendation in recommendationResponse.Recommendations)
            {
                if (!events.TryGetValue(recommendation.EventId, out var e))
                    continue;

                var clubName = clubNames.GetValueOrDefault(e.ClubId);
                var attendeesCount = attendeeCounts.GetValueOrDefault(e.EventId, 0);
                var isMember = userClubSet.Contains(e.ClubId);
                var isJoined = userEventSet.Contains(e.EventId);

                list.Add(new RecommendedEventDto(
                    e.EventId,
                    e.Title,
                    e.Location,
                    e.StartAt,
                    e.EndAt,
                    e.Quota,
                    e.ClubId,
                    clubName,
                    e.Description,
                    e.IsCancelled,
                    e.IsPublic,
                    attendeesCount,
                    isMember,
                    isJoined,
                    recommendation.Score,
                    recommendation.Reason.Primary,
                    recommendation.Reason.Details,
                    recommendation.Reason.Features
                ));
            }

            return Ok(list);
        }

        // === ✅ DEBUG: Recommendation debug info ===
        [HttpGet("recommendations-debug")]
        [Authorize]
        public async Task<ActionResult<object>> RecommendationsDebug()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Kullanıcı bilgisi alınamadı.");

            // Kullanıcının takip ettiği kulüpler
            var followedClubIds = await _db.ClubMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ClubId)
                .ToListAsync();

            var followedClubs = await _db.Clubs
                .Where(c => followedClubIds.Contains(c.ClubId))
                .Select(c => new { c.ClubId, c.Name, c.Description, c.Purpose })
                .ToListAsync();

            var allClubs = await _db.Clubs
                .Select(c => new { c.ClubId, c.Name, c.Description, c.Purpose })
                .ToListAsync();

            var allEvents = await _db.Events
                .Where(e => !e.IsCancelled && e.IsPublic)
                .Select(e => new { e.EventId, e.Title, e.ClubId, e.StartAt })
                .ToListAsync();

            return Ok(new
            {
                userId,
                followedClubCount = followedClubIds.Count,
                followedClubIds,
                followedClubs,
                totalClubsCount = allClubs.Count,
                allClubs,
                totalPublicEventsCount = allEvents.Count,
                eventsByClub = allEvents.GroupBy(e => e.ClubId).Select(g => new { clubId = g.Key, eventCount = g.Count() }).ToList()
            });
        }
    }
}
