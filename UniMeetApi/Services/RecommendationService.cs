using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace UniMeetApi.Services
{
    public interface IRecommendationService
    {
        Task<List<Event>> GetRecommendedEventsAsync(int userId);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(AppDbContext context, ILogger<RecommendationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Yapay Zeka Algoritması: Metin benzerliği hesapla
        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;

            text1 = text1.ToLower();
            text2 = text2.ToLower();

            // Kelime tabanlı benzerlik
            var words1 = text1.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var words2 = text2.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            if (words1.Count == 0 || words2.Count == 0)
                return 0;

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return (double)intersection / union; // Jaccard benzerliği
        }

        // Kulüplerin metin tabanlı benzerliğini hesapla
        private double CalculateClubSimilarity(Club club1, Club club2)
        {
            double similarityScore = 0;

            // İsim benzerliği
            similarityScore += CalculateSimilarity(club1.Name, club2.Name) * 0.2;

            // Açıklama benzerliği
            similarityScore += CalculateSimilarity(club1.Description ?? "", club2.Description ?? "") * 0.3;

            // Amaç benzerliği
            similarityScore += CalculateSimilarity(club1.Purpose ?? "", club2.Purpose ?? "") * 0.5;

            return similarityScore;
        }

        public async Task<List<Event>> GetRecommendedEventsAsync(int userId)
        {
            // 1. Kullanıcının takip ettiği kulüpleri al
            var followedClubIds = await _context.ClubMembers
                .Where(cm => cm.UserId == userId)
                .Select(cm => cm.ClubId)
                .ToListAsync();

            _logger.LogInformation($"UserId {userId} tarafından takip edilen kulüp sayısı: {followedClubIds.Count}");

            if (!followedClubIds.Any())
            {
                _logger.LogInformation($"UserId {userId} hiçbir kulüp takip etmiyor");
                return new List<Event>();
            }

            // 2. Takip edilen kulüpleri getir
            var followedClubs = await _context.Clubs
                .Where(c => followedClubIds.Contains(c.ClubId))
                .ToListAsync();

            _logger.LogInformation($"Takip edilen {followedClubs.Count} kulüp yüklendi");

            // 3. Tüm kulüpleri getir (öneriler için)
            var allClubs = await _context.Clubs.ToListAsync();
            _logger.LogInformation($"Toplam {allClubs.Count} kulüp vardır");

            // 4. Benzer kulüpleri bul (takip edilmeyenleri)
            var similarClubIds = new HashSet<int>();
            var clubSimilarities = new Dictionary<int, double>();

            foreach (var followedClub in followedClubs)
            {
                foreach (var club in allClubs)
                {
                    // Zaten takip edilen kulüpleri hariç tut
                    if (followedClubIds.Contains(club.ClubId))
                        continue;

                    var similarity = CalculateClubSimilarity(followedClub, club);
                    
                    if (!clubSimilarities.ContainsKey(club.ClubId))
                        clubSimilarities[club.ClubId] = similarity;
                    else
                        clubSimilarities[club.ClubId] = Math.Max(clubSimilarities[club.ClubId], similarity);
                }
            }

            _logger.LogInformation($"Hesaplanan benzer kulüpler: {clubSimilarities.Count}");
            
            // İlk önce %15 eşiğiyle dene, yoksa %10'a düşür, yoksa %5'e düşür
            var thresholds = new[] { 0.15, 0.10, 0.05, 0.01 };
            foreach (var threshold in thresholds)
            {
                similarClubIds = new HashSet<int>(clubSimilarities
                    .Where(kvp => kvp.Value > threshold)
                    .Select(kvp => kvp.Key));

                _logger.LogInformation($"Eşik {threshold:P}: {similarClubIds.Count} benzer kulüp bulundu");

                if (similarClubIds.Any())
                    break;
            }

            // Eğer hiç benzer kulüp yoksa, tüm takip edilmeyen kulüplerden al
            if (!similarClubIds.Any())
            {
                similarClubIds = new HashSet<int>(allClubs
                    .Where(c => !followedClubIds.Contains(c.ClubId))
                    .Select(c => c.ClubId));
                _logger.LogInformation($"Benzerlik bulunamadı, tüm takip edilmeyen kulüplerden alınıyor: {similarClubIds.Count}");
            }

            // 5. Benzer kulüplerin etkinliklerini getir
            var recommendedEvents = await _context.Events
                .Where(e => similarClubIds.Contains(e.ClubId) && !e.IsCancelled && e.IsPublic)
                .OrderByDescending(e => e.StartAt)
                .Take(20)
                .ToListAsync();

            // Eğer hiç etkinlik bulunamadı, tüm public etkinlikleri döndür (fallback)
            if (!recommendedEvents.Any())
            {
                _logger.LogInformation($"Benzer kulüplerden etkinlik bulunamadı, tüm public etkinlikleri dönüyorum");
                recommendedEvents = await _context.Events
                    .Where(e => !e.IsCancelled && e.IsPublic && !followedClubIds.Contains(e.ClubId))
                    .OrderByDescending(e => e.StartAt)
                    .Take(20)
                    .ToListAsync();
            }

            _logger.LogInformation($"Toplamda {recommendedEvents.Count} etkinlik önerilecek");

            return recommendedEvents;
        }
    }
}
