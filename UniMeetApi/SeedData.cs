using UniMeetApi;

namespace UniMeetApi
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            // Eğer zaten veriler varsa, seed yapma
            if (context.Clubs.Any())
            {
                return;
            }

            // 4 Kulüp oluştur
            var clubs = new List<Club>
            {
                new Club
                {
                    Name = "Teknoloji Kulübü",
                    Description = "Yazılım geliştirme ve teknoloji hakkında tartışma yapan kulüp",
                    ProfileImageUrl = "https://via.placeholder.com/150?text=Tech+Club",
                    FoundedDate = new DateTime(2023, 1, 15),
                    Purpose = "Öğrencilere yazılım ve teknoloji alanında bilgi vermek"
                },
                new Club
                {
                    Name = "Spor Kulübü",
                    Description = "Çeşitli spor aktiviteleri ve fitness etkinlikleri düzenleyen kulüp",
                    ProfileImageUrl = "https://via.placeholder.com/150?text=Sports+Club",
                    FoundedDate = new DateTime(2023, 2, 20),
                    Purpose = "Öğrencilerin fiziksel aktivitelere katılmasını teşvik etmek"
                },
                new Club
                {
                    Name = "Sanat ve Tasarım Kulübü",
                    Description = "Görsel sanatlar, grafik tasarım ve fotoğrafçılık etkinlikleri",
                    ProfileImageUrl = "https://via.placeholder.com/150?text=Art+Club",
                    FoundedDate = new DateTime(2023, 3, 10),
                    Purpose = "Öğrencilerin yaratıcı yeteneklerini geliştirmek"
                },
                new Club
                {
                    Name = "Girişimcilik Kulübü",
                    Description = "Başlangıç şirketleri ve iş modelleri hakkında eğitim veren kulüp",
                    ProfileImageUrl = "https://via.placeholder.com/150?text=Startup+Club",
                    FoundedDate = new DateTime(2023, 4, 5),
                    Purpose = "Öğrencileri girişimcilik ve inovasyona teşvik etmek"
                }
            };

            context.Clubs.AddRange(clubs);
            context.SaveChanges();

            // Her kulüp için 3 etkinlik oluştur
            var events = new List<Event>();
            var now = DateTime.UtcNow;

            // Teknoloji Kulübü Etkinlikleri
            events.AddRange(new[]
            {
                new Event
                {
                    Title = "C# ve .NET Masterclass",
                    Location = "Bilgisayar Lab 1",
                    StartAt = now.AddDays(7),
                    EndAt = now.AddDays(7).AddHours(3),
                    Quota = 50,
                    ClubId = clubs[0].ClubId,
                    Description = "C# programlama dilinin temellerinden ileri seviyelere kadar kapsamlı bir eğitim",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                },
                new Event
                {
                    Title = "Web Geliştirme Çalıştayı",
                    Location = "Seminer Salonu A",
                    StartAt = now.AddDays(14),
                    EndAt = now.AddDays(14).AddHours(4),
                    Quota = 40,
                    ClubId = clubs[0].ClubId,
                    Description = "React, Vue ve Angular ile modern web uygulamaları geliştirme",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                },
                new Event
                {
                    Title = "Yapay Zeka Uygulamaları Sunumu",
                    Location = "Auditoryum",
                    StartAt = now.AddDays(21),
                    EndAt = now.AddDays(21).AddHours(2),
                    Quota = 100,
                    ClubId = clubs[0].ClubId,
                    Description = "Machine Learning ve Deep Learning projelerinin pratik uygulamaları",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                }
            });

            // Spor Kulübü Etkinlikleri
            events.AddRange(new[]
            {
                new Event
                {
                    Title = "Futsal Turnuvası",
                    Location = "Spor Salonu",
                    StartAt = now.AddDays(10),
                    EndAt = now.AddDays(10).AddHours(6),
                    Quota = 60,
                    ClubId = clubs[1].ClubId,
                    Description = "Ekip bazında futsal turnuvası, tüm öğrencilere açık",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                },
                new Event
                {
                    Title = "Yoga ve Meditasyon Seansı",
                    Location = "Yoga Stüdyosu",
                    StartAt = now.AddDays(8),
                    EndAt = now.AddDays(8).AddHours(1.5),
                    Quota = 30,
                    ClubId = clubs[1].ClubId,
                    Description = "Stres yönetimi ve fiziksel esneklik için yoga ve meditasyon seansı",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                },
                new Event
                {
                    Title = "Maraton Antrenman Başlangıç",
                    Location = "Kampüs Parkuru",
                    StartAt = now.AddDays(15),
                    EndAt = now.AddDays(15).AddHours(2),
                    Quota = 80,
                    ClubId = clubs[1].ClubId,
                    Description = "Maraton için antrenman programının başlangıç seansı",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                }
            });

            // Sanat ve Tasarım Kulübü Etkinlikleri
            events.AddRange(new[]
            {
                new Event
                {
                    Title = "Dijital Tasarım Atölyesi",
                    Location = "Tasarım Lab",
                    StartAt = now.AddDays(12),
                    EndAt = now.AddDays(12).AddHours(3),
                    Quota = 35,
                    ClubId = clubs[2].ClubId,
                    Description = "Photoshop, Illustrator ve Figma ile dijital tasarım eğitimi",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                },
                new Event
                {
                    Title = "Fotoğrafçılık Gezisi",
                    Location = "Şehir Merkezini",
                    StartAt = now.AddDays(18),
                    EndAt = now.AddDays(18).AddHours(4),
                    Quota = 25,
                    ClubId = clubs[2].ClubId,
                    Description = "Şehir manzaralarını fotoğraflayan tur ve sonrasında portfolio terapisi",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                },
                new Event
                {
                    Title = "Resim Sergisi Açılışı",
                    Location = "Sanat Galerisi",
                    StartAt = now.AddDays(25),
                    EndAt = now.AddDays(25).AddHours(3),
                    Quota = 100,
                    ClubId = clubs[2].ClubId,
                    Description = "Kulüp üyeleri tarafından yapılan sanat eserlerinin sergilenmesi",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                }
            });

            // Girişimcilik Kulübü Etkinlikleri
            events.AddRange(new[]
            {
                new Event
                {
                    Title = "Startup Pitch Yarışması",
                    Location = "Konferans Salonu",
                    StartAt = now.AddDays(9),
                    EndAt = now.AddDays(9).AddHours(4),
                    Quota = 50,
                    ClubId = clubs[3].ClubId,
                    Description = "Girişimci adaylarının iş fikirlerini sunduğu pitch yarışması",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                },
                new Event
                {
                    Title = "İş Modeli Geliştirme Atölyesi",
                    Location = "Seminer Salonu B",
                    StartAt = now.AddDays(16),
                    EndAt = now.AddDays(16).AddHours(3),
                    Quota = 40,
                    ClubId = clubs[3].ClubId,
                    Description = "Canvas Model ile etkili iş modeli tasarlama yöntemi",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                },
                new Event
                {
                    Title = "Başarılı Girişimciler Panel Sohbeti",
                    Location = "Auditoryum",
                    StartAt = now.AddDays(23),
                    EndAt = now.AddDays(23).AddHours(2.5),
                    Quota = 150,
                    ClubId = clubs[3].ClubId,
                    Description = "Başarılı girişimcilerden tecrübe ve ipuçları alma fırsatı",
                    IsPublic = true,
                    CreatedByUserId = 1,
                    CreatedAt = now
                }
            });

            context.Events.AddRange(events);
            context.SaveChanges();
        }
    }
}
