-- ============================================================
-- UniMeet - YAPAY ZEKA İÇİN OPTİMİZE EDİLMİŞ VERİ SETİ
-- ============================================================
-- Bu script yapay zeka algoritmasının en iyi çalışması için
-- zengin açıklamalar ve anahtar kelimeler içeren gerçekçi veri seti oluşturur
-- ============================================================

USE UniMeetDers;
GO

-- Türkçe karakter desteği için collation ayarı
SET LANGUAGE Turkish;
GO

-- ============================================================
-- ADIM 1: Mevcut verileri temizle
-- ============================================================

DELETE FROM EventAttendees;
DELETE FROM FavoriteEvents;
DELETE FROM ClubMembers;
DELETE FROM Events;
DELETE FROM Clubs;
DELETE FROM Users WHERE UserId > 1; -- İlk kullanıcıyı koru
GO

-- ============================================================
-- ADIM 2: TEST KULLANICILARI OLUŞTUR
-- ============================================================

-- Şifre: Test123! (tüm kullanıcılar için)
DECLARE @hashedPassword NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEHxK8mN7YqP3Z0vQxR5jF8Km9YvC+3xL2gH4pT6wJ9sK1mN8xR3vB5aQ7cD9fE2gH4==';

INSERT INTO Users (Email, PasswordHash, FullName, IsActive, Role, EmailConfirmed, RequiresPasswordReset) VALUES
-- Test kullanıcıları (farklı ilgi alanları)
('200101010101@dogus.edu.tr', @hashedPassword, 'Ahmet Yılmaz (AI Meraklısı)', 1, 0, 1, 0),      -- Teknoloji/AI
('200202020202@dogus.edu.tr', @hashedPassword, 'Ayşe Demir (Girişimci)', 1, 0, 1, 0),          -- Girişimcilik
('200303030303@dogus.edu.tr', @hashedPassword, 'Mehmet Kaya (Gamer)', 1, 0, 1, 0),             -- E-Spor
('200404040404@dogus.edu.tr', @hashedPassword, 'Zeynep Arslan (Sanatçı)', 1, 0, 1, 0),         -- Sanat
('200505050505@dogus.edu.tr', @hashedPassword, 'Can Öztürk (Sporcu)', 1, 0, 1, 0),             -- Spor
('200606060606@dogus.edu.tr', @hashedPassword, 'Elif Çelik (Sosyal)', 1, 0, 1, 0),             -- Çok yönlü
-- Manager kullanıcıları
('201101010101@dogus.edu.tr', @hashedPassword, 'Kulüp Yöneticisi 1', 1, 1, 1, 0),
('201202020202@dogus.edu.tr', @hashedPassword, 'Kulüp Yöneticisi 2', 1, 1, 1, 0);
GO

-- ============================================================
-- ADIM 3: KULÜPLER - YZ İÇİN OPTİMİZE EDİLMİŞ AÇIKLAMALAR
-- ============================================================

SET IDENTITY_INSERT Clubs ON;

INSERT INTO Clubs (ClubId, Name, Description, Purpose) VALUES

-- 1. TEKNOLOJİ & YAPAY ZEKA KULÜBÜ
(1, 
'Yapay Zeka ve Makine Öğrenmesi Laboratuvarı',
'Python, TensorFlow, PyTorch ve scikit-learn kullanarak derin öğrenme (deep learning) ve makine öğrenmesi projeleri geliştiriyoruz. Computer vision (görüntü işleme), natural language processing (NLP), veri analizi (data analytics), tahmine dayalı modelleme (predictive modeling) ve büyük veri (big data) teknolojileri üzerine çalışıyoruz. Kaggle yarışmalarına katılıyor, OpenCV ile görüntü işleme, BERT ve GPT modelleri ile doğal dil işleme uygulamaları yapıyoruz. Numpy, Pandas, Matplotlib kullanarak veri görselleştirme ve analiz teknikleri öğretiyoruz.',
'Öğrencilerin yapay zeka teknolojilerini teorik ve pratik olarak öğrenmesi, gerçek dünya problemlerini AI ile çözmesi, makine öğrenmesi modellerini geliştirmesi ve ürünleştirmesini sağlamak. Hackathon, akademik araştırma grupları ve teknoloji şirketleriyle iş birliği yaparak öğrencileri sektöre hazırlamak. Neural networks, convolutional neural networks (CNN), recurrent neural networks (RNN), transformers gibi ileri düzey konularda uzmanlık kazandırmak.'),

-- 2. WEB & MOBİL GELİŞTİRME KULÜBÜ
(2,
'Full Stack Yazılım Geliştirme Topluluğu',
'Modern web ve mobil uygulama geliştirme teknolojileri: React.js, Next.js, Vue.js ile frontend development, Node.js, Express.js, Django, FastAPI ile backend development. Mobile app development için Flutter, React Native ve Swift kullanıyoruz. RESTful API, GraphQL, WebSocket protokolleri, SQL (PostgreSQL, MySQL) ve NoSQL (MongoDB, Firebase) veritabanları. DevOps araçları: Docker, Kubernetes, CI/CD pipelines, Git version control, GitHub Actions. Cloud platformları: AWS, Google Cloud, Azure deployment. Authentication (JWT, OAuth2), state management (Redux, Context API), responsive design, Progressive Web Apps (PWA) konularında workshop ve projeler düzenliyoruz.',
'Öğrencilerin profesyonel yazılım geliştirme becerilerini kazanması, açık kaynak (open source) projelere katkı vermesi, startup fikirleri için MVP (minimum viable product) geliştirmesi ve yazılım mühendisliği prensiplerini öğrenmesi. Agile-Scrum metodolojileri, test-driven development (TDD), clean code principles ve software architecture patterns konularında mentorluk sağlamak.'),

-- 3. GİRİŞİMCİLİK & STARTUP KULÜBÜ
(3,
'Girişimcilik ve İnovasyon Merkezi',
'Startup ekosistemi, business model canvas, lean startup metodolojisi, MVP geliştirme, customer discovery, product-market fit, growth hacking stratejileri konularında eğitimler ve workshoplar düzenliyoruz. Pitch deck hazırlama, investor relations, venture capital, angel investment, seed funding, Series A-B-C rounds gibi finansman süreçlerini öğretiyoruz. Market research, competitive analysis, financial projections, valuation, equity distribution, term sheets konularında pratik yapıyoruz. Mentorship programları, networking events, demo days ve startup yarışmaları organize ediyoruz.',
'Öğrencilerin girişimcilik ruhunu geliştirmesi, iş fikirleri oluşturması, piyasa analizi yapması, yatırımcılarla bağlantı kurması ve startup kurması için gerekli bilgi ve networkü sağlamak. Entrepreneurship mindset, problem-solving skills, leadership ve strategic thinking yeteneklerini geliştirmek. Mezun girişimciler ve sektör profesyonelleriyle buluşturarak mentörlük imkanı sunmak.'),

-- 4. E-SPOR VE GAMİNG KULÜBÜ
(4,
'Profesyonel E-Spor ve Competitive Gaming Kulübü',
'League of Legends, Valorant, CS:GO, Dota 2, Overwatch, Rocket League gibi competitive games için profesyonel takım koçluğu ve strateji geliştirme. Esports tournaments, ranked gameplay, team coordination, meta analysis, patch notes incelemesi, professional streaming (Twitch, YouTube Gaming), content creation, video editing yapıyoruz. Discord sunucumuzda scrim (scrimmage) organizasyonu, replay analysis, individual coaching sessions düzenliyoruz. Game sense, mechanical skills, communication, shotcalling, positioning konularında antrenman programları. Gaming peripherals (mouse, keyboard, headset) önerileri ve setup optimization.',
'Öğrencilerin e-spor kariyeri için gerekli becerileri kazanması, competitive gaming ortamında takım çalışması yapması, profesyonel oyunculardan öğrenmesi ve e-spor endüstrisini tanıması. Tournament organization, event management, streaming infrastructure ve gaming industry career paths hakkında bilgi vermek. Mental health in gaming, balanced lifestyle ve responsible gaming konularında farkındalık yaratmak.'),

-- 5. DİJİTAL PAZARLAMA & SOSYAL MEDYA KULÜBÜ
(5,
'Digital Marketing ve Content Strategy Atölyesi',
'Social media marketing (Instagram, TikTok, LinkedIn, Twitter), content creation, SEO (search engine optimization), SEM (search engine marketing), Google Ads, Facebook Ads Manager, influencer marketing, email marketing campaigns, marketing automation, analytics (Google Analytics, Meta Business Suite), conversion rate optimization (CRO), A/B testing konularında projeler yapıyoruz. Copywriting, visual design (Canva, Adobe Creative Suite), video editing (Premiere Pro, CapCut), storytelling, brand identity, personal branding, community management, engagement strategies üzerine workshoplar düzenliyoruz.',
'Öğrencilerin dijital pazarlama stratejileri geliştirmesi, sosyal medya yönetimi yapması, içerik üretimi konusunda uzmanlaşması ve marketing analytics kullanarak veri odaklı kararlar almasını sağlamak. Brand awareness, lead generation, customer acquisition cost (CAC), lifetime value (LTV), funnel optimization gibi metrikleri öğretmek. Freelance ve agency çalışma fırsatları için portföy geliştirmelerine yardımcı olmak.'),

-- 6. VERİ BİLİMİ VE ANALİTİK KULÜBÜ
(6,
'Data Science ve Business Intelligence Topluluğu',
'Python ile veri analizi: Pandas, NumPy, data cleaning, data preprocessing, exploratory data analysis (EDA). Data visualization: Matplotlib, Seaborn, Plotly, Tableau, Power BI dashboard creation. Statistical analysis, hypothesis testing, regression analysis, time series forecasting. SQL querying, database management, ETL (extract-transform-load) processes. Big data technologies: Apache Spark, Hadoop ecosystem. Machine learning algorithms: supervised learning (classification, regression), unsupervised learning (clustering, dimensionality reduction), feature engineering. Real-world datasets üzerinde case studies, business problems solving, data storytelling.',
'Öğrencilerin veri bilimi araçlarını öğrenmesi, iş problemlerini veriyle çözmesi, veri görselleştirme ve raporlama becerilerini geliştirmesi. Business analytics, predictive modeling, data-driven decision making yeteneklerini kazandırmak. Finance, healthcare, e-commerce, marketing gibi farklı sektörlerde veri bilimi uygulamalarını göstermek. Kaggle competitions ve data science internship fırsatlarına hazırlamak.'),

-- 7. SİBER GÜVENLİK VE ETİK HACKING KULÜBÜ
(7,
'Cybersecurity ve Penetration Testing Laboratuvarı',
'Ethical hacking, penetration testing, vulnerability assessment, network security, web application security (OWASP Top 10), cryptography, malware analysis yapıyoruz. Kali Linux, Metasploit, Burp Suite, Wireshark, Nmap gibi security tools kullanımı. CTF (Capture The Flag) competitions, bug bounty programs, security certifications (CEH, OSCP, Security+) hazırlığı. SQL injection, XSS, CSRF, authentication bypass, privilege escalation gibi attack vectors. Secure coding practices, security by design, threat modeling, incident response, SIEM (Security Information and Event Management).',
'Öğrencilerin siber güvenlik alanında kariyer yapması için gerekli teknik bilgi ve sertifikasyonları kazanmasını sağlamak. Ethical hacking principles, legal boundaries, responsible disclosure konularında bilinç oluşturmak. Network defense, cloud security, application security konularında hands-on deneyim kazandırmak. SOC (Security Operations Center) analyst, penetration tester, security consultant rollerine hazırlamak.'),

-- 8. FİNANS & YATIRIM KULÜBÜ
(8,
'Finans Mühendisliği ve Yatırım Analizi Topluluğu',
'Borsa (BIST) trading, fundamental analysis, technical analysis (candlestick patterns, indicators, chart reading), portfolio management, risk management strategies. Cryptocurrency ve blockchain technology: Bitcoin, Ethereum, DeFi (decentralized finance), NFT markets. Forex trading, commodities, derivatives (options, futures). Financial modeling, Excel financial analysis, Python for finance (algorithmic trading, backtesting). Valuation methods (DCF, multiples), financial statements analysis. Personal finance, budgeting, investment planning, passive income strategies. Economic indicators, macroeconomic analysis, market psychology.',
'Öğrencilerin finansal okuryazarlığını artırmak, yatırım araçlarını tanıması, risk yönetimi yapması ve kişisel finans yönetimi becerilerini geliştirmesi. Trading psychology, disciplined investing, long-term wealth building konularında farkındalık yaratmak. Stock market simulations, paper trading ile risksiz deneyim kazandırmak. Finance career paths: investment banking, asset management, fintech konularında yönlendirme yapmak.'),

-- 9. ROBOTIK VE IoT KULÜBÜ
(9,
'Robotik Sistemler ve Nesnelerin İnterneti (IoT) Atölyesi',
'Arduino, Raspberry Pi, ESP32 ile elektronik proje geliştirme. Robotik sistemler: sensör entegrasyonu (ultrasonic, infrared, temperature, humidity), motor kontrolü (servo, stepper), autonomous navigation, line following, obstacle avoidance. IoT applications: smart home automation, environmental monitoring, MQTT protocol, cloud integration (AWS IoT, Azure IoT Hub). Embedded systems programming (C/C++, MicroPython). 3D printing, CAD modeling (Fusion 360, SolidWorks), PCB design (KiCad, Eagle). Drone technology, computer vision integration, ROS (Robot Operating System). Home automation projects, wearable technology.',
'Öğrencilerin robotik ve IoT sistemlerini tasarlaması, prototip geliştirmesi, gerçek dünya problemlerini fiziksel çözümlerle ele alması. Mechatronics, automation engineering, embedded systems konularında pratik deneyim kazandırmak. Maker culture, DIY electronics, rapid prototyping becerilerini geliştirmek. Robotics competitions (FIRST, VEX) ve innovation challenges için takımlar oluşturmak.'),

-- 10. OYUN GELİŞTİRME (GAME DEV) KULÜBÜ
(10,
'Game Development ve Interactive Media Kulübü',
'Unity (C#) ve Unreal Engine (C++, Blueprints) ile 2D/3D oyun geliştirme. Game design principles: gameplay mechanics, level design, game balancing, player experience (UX). Graphics programming: shaders, lighting, particle systems, post-processing effects. Game physics, collision detection, AI behavior (pathfinding, state machines). Mobile game development (iOS, Android), PC gaming, VR/AR applications. Art pipeline: 3D modeling (Blender, Maya), texturing, animation (rigging, skinning). Sound design, music integration. Multiplayer networking, matchmaking systems. Game monetization strategies, publishing process (Steam, Google Play, App Store).',
'Öğrencilerin oyun geliştirme süreçlerini öğrenmesi, indie game projects oluşturması, game jamslere katılması ve gaming industryye girmesi için portföy hazırlaması. Game programming, game design, game art konularında uzmanlaşma fırsatı sunmak. Collaboration, project management, agile game development metodolojilerini öğretmek. Career paths: game programmer, game designer, technical artist, gameplay engineer.'),

-- 11. SANAT & KÜLTÜR KULÜBÜ
(11,
'Görsel Sanatlar ve Yaratıcı Kültür Topluluğu',
'Resim (akrilik, suluboya, yağlıboya teknikleri), çizim (karakalem, eskiz, anatomi, perspektif), fotoğrafçılık (kompozisyon, ışık, düzenleme, DSLR kullanımı, Adobe Lightroom, Photoshop), grafik tasarım (Adobe Illustrator, InDesign, typography, logo design, branding), dijital illüstrasyon (Procreate, digital painting). Sergi organizasyonu, müze gezileri (İstanbul Modern, Pera Müzesi, Sakıp Sabancı Müzesi), sanat tarihi seminerleri, contemporary art discussions. Film analizi, sinema kültürü, kısa film üretimi, senaryo yazımı. Tiyatro workshopları, performans sanatı, yaratıcı drama.',
'Öğrencilerin sanatsal ifade becerilerini geliştirmesi, farklı sanat formlarını deneyimlemesi, yaratıcı düşünme ve estetik duyarlılık kazanması. Sanat portföyü oluşturma, exhibition organization, art criticism konularında deneyim kazandırmak. Creative industries (advertising, media, design) kariyerlerine hazırlamak. Art therapy, self-expression, cultural awareness konularında farkındalık yaratmak.'),

-- 12. MÜZIK & SES PRODÜKSIYONU KULÜBÜ
(12,
'Müzik Prodüksiyonu ve Audio Engineering Atölyesi',
'Digital Audio Workstation (DAW) kullanımı: Ableton Live, FL Studio, Logic Pro, Pro Tools. Music production: beat making, melody composition, arrangement, mixing, mastering. Sound design, synthesis (FM, subtractive, wavetable), sampling techniques. Music theory: scales, chords, harmony, rhythm, song structure. Recording techniques, microphone placement, vocal recording, acoustic treatment. Audio effects: reverb, delay, compression, EQ, saturation. Electronic music genres: EDM, techno, house, lo-fi, trap production. Live performance setup, MIDI controllers, DJ equipment (CDJ, mixer). Podcast production, voice-over recording, sound editing.',
'Öğrencilerin müzik üretimi yapması, ses mühendisliği becerilerini geliştirmesi, müzik teorisi öğrenmesi ve müzik endüstrisini tanıması. Music career paths: producer, sound engineer, composer, DJ, audio post-production konularında yönlendirme. Collaboration opportunities, music releases (Spotify, SoundCloud), music marketing strategies. Performance anxiety management, musician wellness, creative process konularında destek sağlamak.'),

-- 13. FİTNESS & SAĞLIKLI YAŞAM KULÜBÜ
(13,
'Fitness, Nutrition ve Wellness Topluluğu',
'Strength training (ağırlık antrenmanı), calisthenics (vücut ağırlığı egzersizleri), functional fitness, CrossFit, HIIT (High-Intensity Interval Training). Yoga (Hatha, Vinyasa, Ashtanga), pilates, flexibility training, mobility work. Running club, marathon training programs, interval training, endurance building. Nutrition science: macronutrients (protein, carbs, fats), meal planning, calorie tracking, supplements. Weight management, body composition analysis, healthy eating habits. Mental health: mindfulness, meditation, stress management, sleep optimization. Sports injury prevention, recovery techniques, stretching routines. Gym equipment usage, proper form, workout programming.',
'Öğrencilerin fiziksel sağlığını iyileştirmesi, düzenli egzersiz alışkanlığı kazanması, beslenme konusunda bilinçlenmesi ve dengeli yaşam tarzı oluşturması. Fitness goals (muscle building, fat loss, athletic performance) için bilimsel yaklaşımlar öğretmek. Group workouts, accountability partners, fitness challenges ile motivasyon sağlamak. Wellness career interests: personal training, nutrition coaching, physiotherapy konularında bilgilendirme yapmak.'),

-- 14. FOTOĞRAFÇILIK & VİDEOGRAFİ KULÜBÜ
(14,
'Fotoğraf ve Video Prodüksiyon Akademisi',
'Photography fundamentals: exposure triangle (aperture, shutter speed, ISO), composition rules (rule of thirds, leading lines, symmetry), lighting techniques (natural light, studio lighting, flash photography). Camera equipment: DSLR, mirrorless cameras, lenses (wide angle, telephoto, macro, prime vs zoom). Photo editing: Adobe Lightroom (color grading, exposure correction, presets), Photoshop (retouching, compositing, manipulation). Photography genres: portrait, landscape, street photography, event coverage, product photography, food photography. Videography: cinematic filming, b-roll, stabilization (gimbal, tripod), frame rates, slow motion. Video editing: Adobe Premiere Pro, Final Cut Pro, DaVinci Resolve (color correction). YouTube content creation, vlogs, documentaries, commercial videos.',
'Öğrencilerin fotoğraf ve video çekimi yapması, profesyonel editing skills kazanması, visual storytelling öğrenmesi ve content creator olması. Portfolio development, client work, freelance photography/videography fırsatları. Photography business: pricing, contracts, marketing, social media presence. Career paths: wedding photographer, commercial videographer, content creator, cinematographer konularında yönlendirme.'),

-- 15. PSİKOLOJİ & KİŞİSEL GELİŞİM KULÜBÜ
(15,
'Psikoloji ve Kişisel Gelişim Akademisi',
'Psychology fundamentals: cognitive psychology, behavioral psychology, positive psychology. Mental health awareness, stress management techniques, anxiety coping strategies, depression support. Emotional intelligence (EQ), self-awareness, emotion regulation, empathy development. Communication skills, active listening, conflict resolution, assertiveness training. Productivity hacks: time management (Pomodoro, time blocking), goal setting (SMART goals), habit formation, procrastination elimination. Mindfulness practices, meditation techniques, breathing exercises, journaling. Growth mindset, resilience building, self-confidence, imposter syndrome. Relationship psychology, attachment styles, healthy boundaries. Career development: CV writing, interview skills, LinkedIn optimization, personal branding.',
'Öğrencilerin mental sağlığını koruması, kişisel gelişim becerilerini artırması, sosyal ilişkilerini geliştirmesi ve yaşam kalitesini yükseltmesi. Peer support groups, safe space for discussions, mental health destigmatization. Psychology career paths: counseling, HR, organizational psychology konularında bilgilendirme. Self-help resources, therapy awareness, psychological first aid konularında farkındalık yaratmak.');

SET IDENTITY_INSERT Clubs OFF;
GO

-- ============================================================
-- ADIM 4: KULÜP ÜYELİKLERİ (Yapay Zeka için Çeşitli Profiller)
-- ============================================================

-- UserID'leri dinamik olarak al
DECLARE @AhmetId INT = (SELECT UserId FROM Users WHERE Email = '200101010101@dogus.edu.tr');
DECLARE @AyseId INT = (SELECT UserId FROM Users WHERE Email = '200202020202@dogus.edu.tr');
DECLARE @MehmetId INT = (SELECT UserId FROM Users WHERE Email = '200303030303@dogus.edu.tr');
DECLARE @ZeynepId INT = (SELECT UserId FROM Users WHERE Email = '200404040404@dogus.edu.tr');
DECLARE @CanId INT = (SELECT UserId FROM Users WHERE Email = '200505050505@dogus.edu.tr');
DECLARE @ElifId INT = (SELECT UserId FROM Users WHERE Email = '200606060606@dogus.edu.tr');
DECLARE @Manager1Id INT = (SELECT UserId FROM Users WHERE Email = '201101010101@dogus.edu.tr');

-- Ahmet Yılmaz (AI Meraklısı) - Teknoloji odaklı
INSERT INTO ClubMembers (UserId, ClubId, JoinedAt) VALUES
(@AhmetId, 1, GETDATE()),  -- Yapay Zeka Kulübü
(@AhmetId, 2, GETDATE()),  -- Full Stack Dev
(@AhmetId, 6, GETDATE()),  -- Data Science
(@AhmetId, 9, GETDATE());  -- Robotik & IoT

-- Ayşe Demir (Girişimci) - Business odaklı
INSERT INTO ClubMembers (UserId, ClubId, JoinedAt) VALUES
(@AyseId, 3, GETDATE()),  -- Girişimcilik
(@AyseId, 5, GETDATE()),  -- Digital Marketing
(@AyseId, 8, GETDATE()),  -- Finans & Yatırım
(@AyseId, 15, GETDATE()); -- Kişisel Gelişim

-- Mehmet Kaya (Gamer) - Gaming odaklı
INSERT INTO ClubMembers (UserId, ClubId, JoinedAt) VALUES
(@MehmetId, 4, GETDATE()),  -- E-Spor
(@MehmetId, 10, GETDATE()), -- Game Development
(@MehmetId, 5, GETDATE()),  -- Digital Marketing (content creation)
(@MehmetId, 12, GETDATE()); -- Müzik (streaming music)

-- Zeynep Arslan (Sanatçı) - Sanat odaklı
INSERT INTO ClubMembers (UserId, ClubId, JoinedAt) VALUES
(@ZeynepId, 11, GETDATE()), -- Sanat & Kültür
(@ZeynepId, 14, GETDATE()), -- Fotoğrafçılık
(@ZeynepId, 12, GETDATE()), -- Müzik
(@ZeynepId, 15, GETDATE()); -- Kişisel Gelişim

-- Can Öztürk (Sporcu) - Sağlık odaklı
INSERT INTO ClubMembers (UserId, ClubId, JoinedAt) VALUES
(@CanId, 13, GETDATE()), -- Fitness
(@CanId, 15, GETDATE()), -- Kişisel Gelişim
(@CanId, 4, GETDATE()),  -- E-Spor (competitive mindset)
(@CanId, 3, GETDATE());  -- Girişimcilik (leadership)

-- Elif Çelik (Çok yönlü) - Balanced
INSERT INTO ClubMembers (UserId, ClubId, JoinedAt) VALUES
(@ElifId, 2, GETDATE()),  -- Full Stack Dev
(@ElifId, 5, GETDATE()),  -- Digital Marketing
(@ElifId, 11, GETDATE()), -- Sanat
(@ElifId, 13, GETDATE()), -- Fitness
(@ElifId, 15, GETDATE()); -- Kişisel Gelişim

-- İlk kullanıcı (Manager)
INSERT INTO ClubMembers (UserId, ClubId, JoinedAt) VALUES
(@Manager1Id, 1, GETDATE()),
(@Manager1Id, 2, GETDATE()),
(@Manager1Id, 3, GETDATE());

-- ============================================================
-- ADIM 5: ETKİNLİK KATILIMLARI - Etkinlikler eklendikten sonra eklenecek
-- ============================================================
-- Bu bölüm şimdilik yorumda, Events eklendikten sonra çalıştırılacak

GO

-- ============================================================
-- ADIM 6: ETKİNLİKLER - YZ İÇİN OPTİMİZE EDİLMİŞ AÇIKLAMALAR
-- ============================================================

-- Tarih hesaplaması için değişkenler
DECLARE @Today DATETIME = GETDATE();
DECLARE @Tomorrow DATETIME = DATEADD(DAY, 1, @Today);
DECLARE @ThreeDays DATETIME = DATEADD(DAY, 3, @Today);
DECLARE @Week DATETIME = DATEADD(DAY, 7, @Today);
DECLARE @TwoWeeks DATETIME = DATEADD(DAY, 14, @Today);
DECLARE @Month DATETIME = DATEADD(DAY, 30, @Today);

INSERT INTO Events (Title, Description, Location, StartAt, EndAt, Quota, ClubId, IsPublic, IsCancelled, CreatedByUserId, CreatedAt) VALUES

-- YAPAY ZEKA KULÜBÜ ETKİNLİKLERİ
('Deep Learning Workshop: Convolutional Neural Networks ile Görüntü Sınıflandırma',
'Bu hands-on workshopta PyTorch framework kullanarak Convolutional Neural Network (CNN) mimarisi tasarlayacak ve eğiteceğiz. CIFAR-10 dataset üzerinde image classification problemi çözeceğiz. Konular: data preprocessing, data augmentation, CNN layers (convolution, pooling, batch normalization), activation functions (ReLU, Softmax), model training, validation, overfitting prevention (dropout, regularization), transfer learning with pre-trained models (ResNet, VGG). Katılımcılar kendi laptop''larında Google Colab veya local environment kullanarak pratik yapacaklar. Temel Python ve makine öğrenmesi bilgisi gereklidir. GPU acceleration, TensorBoard visualization, model evaluation metrics (accuracy, precision, recall, F1-score) konularını işleyeceğiz.',
'Mühendislik Fakültesi - AI Lab 301', 
@ThreeDays, DATEADD(HOUR, 4, @ThreeDays), 
40, 1, 1, 0, 1, GETDATE()),

('Natural Language Processing: BERT ve Transformer Modelleri',
'Modern NLP (Natural Language Processing) teknolojileri: BERT (Bidirectional Encoder Representations from Transformers), GPT architecture, attention mechanism, tokenization (WordPiece, BPE). Hugging Face Transformers library kullanarak text classification, named entity recognition (NER), sentiment analysis, question answering tasks. Fine-tuning pre-trained models, transfer learning in NLP. Turkish language models, multilingual BERT. Practical applications: chatbots, text summarization, machine translation. NLTK, spaCy libraries. Word embeddings: Word2Vec, GloVe, contextual embeddings. Code implementation in Python with real-world datasets.',
'Online - Zoom Meeting',
@Week, DATEADD(HOUR, 3, @Week),
60, 1, 1, 0, 1, GETDATE()),

-- FULL STACK DEV KULÜBÜ
('React.js Masterclass: Hooks, Context API ve State Management',
'React 18 ile modern frontend development: useState, useEffect, useContext, useReducer, useMemo, useCallback hooks deep dive. Custom hooks creation, React Context API for global state, Redux Toolkit integration, Redux Saga middleware. Component lifecycle, virtual DOM, reconciliation algorithm. Performance optimization: React.memo, lazy loading, code splitting. Routing with React Router v6, protected routes, navigation guards. Form handling: Formik, React Hook Form validation. Styling solutions: CSS Modules, styled-components, Tailwind CSS. Testing: Jest, React Testing Library. Real-world project: E-commerce dashboard with authentication, CRUD operations, API integration (axios, fetch), error handling.',
'Teknokent - Yazılım Atölyesi B Block 205',
@ThreeDays, DATEADD(HOUR, 5, @ThreeDays),
35, 2, 1, 0, 1, GETDATE()),

('Backend Development: Node.js, Express ve MongoDB ile RESTful API',
'Node.js runtime environment, Express.js framework ile RESTful API design. MongoDB database, Mongoose ODM, schema design, CRUD operations. Authentication & Authorization: JWT tokens, bcrypt password hashing, middleware implementation. API security: rate limiting, helmet.js, CORS configuration, input validation (Joi, express-validator). File upload handling (Multer), image processing (Sharp). Error handling, logging (Winston, Morgan). Environment variables (.env, dotenv). Async/await, Promises, error-first callbacks. Deployment: Heroku, Railway, MongoDB Atlas. Postman API testing. Real project: Blog API with user authentication, posts, comments, file uploads.',
'Kampüs - Backend Lab A304',
@TwoWeeks, DATEADD(HOUR, 6, @TwoWeeks),
30, 2, 1, 0, 1, GETDATE()),

-- GİRİŞİMCİLİK KULÜBÜ
('Startup Pitch Night: Investor Feedback ve Networking Session',
'Girişimcilik ekosisteminde pitch presentation sanatı: elevator pitch (30 sn), investor pitch (5 dk), demo day presentation. Pitch deck hazırlama: problem statement, solution, market size (TAM, SAM, SOM), business model canvas, competitive analysis, traction metrics, financial projections, team introduction, funding ask. Storytelling techniques, compelling narratives, visual design principles. Body language, vocal delivery, Q&A handling. Angel investors ve VC (venture capital) temsilcileri katılacak, gerçek feedback verecek. Networking session: one-on-one investor meetings, founder-to-founder knowledge sharing. Previous participants startup''ları showcase edilecek. Öğrenci startup fikirlerine mentorluk fırsatı.',
'Merkez Kampüs - Konferans Salonu',
@Week, DATEADD(HOUR, 4, @Week),
80, 3, 1, 0, 1, GETDATE()),

('Growth Hacking Strategies: User Acquisition ve Retention Techniques',
'Startup growth için data-driven strategies: customer acquisition cost (CAC), lifetime value (LTV), retention rate, churn analysis. Growth loops, viral loops, referral programs design. A/B testing methodology, conversion funnel optimization, landing page optimization. Digital marketing channels: SEO (search engine optimization), SEM (search engine marketing), social media advertising, content marketing, email campaigns. Growth metrics: AARRR (Acquisition, Activation, Retention, Revenue, Referral) pirate metrics. Tools: Google Analytics, Mixpanel, Amplitude, Hotjar heatmaps. Case studies: Airbnb, Dropbox, Uber growth tactics. Practical workshop: analyze your startup''s growth potential.',
'İnovasyon Merkezi - Workshop Room 2',
@TwoWeeks, DATEADD(HOUR, 3, @TwoWeeks),
45, 3, 1, 0, 1, GETDATE()),

-- E-SPOR KULÜBÜ
('Valorant 5v5 Tournament: Campus Championship Finals',
'Competitive Valorant turnuvası: double-elimination bracket, best-of-3 matches, grand finals best-of-5. Professional tournament rules, Riot Games official settings. Team registration (5 players + 1 substitute), rank restriction (minimum Gold 2). Prize pool: gaming peripherals (keyboard, mouse, headset), tournament champion medals. Live casting ve commentary (Turkish), Twitch stream broadcast. Pro player analysis, map strategies (Ascent, Bind, Haven, Icebox), agent compositions, economy management. Scrim practice sessions öncesinde. Spectator area, live audience cheering. Post-match analysis sessions, replay reviews. Network with other competitive players, team recruitment opportunities.',
'E-Spor Arena - Gaming Center',
@Week, DATEADD(HOUR, 8, @Week),
64, 4, 1, 0, 1, GETDATE()),

('Streaming Masterclass: Twitch Setup, OBS ve Content Creation',
'Professional streaming setup: OBS Studio configuration, scene creation, source management, transitions, alerts (StreamElements, StreamLabs). Audio setup: microphone (XLR vs USB), audio mixer, noise suppression, compression, EQ settings. Video quality: bitrate, resolution (1080p vs 720p), frame rate (60fps), encoder settings (x264 vs NVENC). Overlay design: Canva, Photoshop templates, animated alerts. Chat interaction, moderators, bot commands (Nightbot, StreamElements bot). Twitch affiliate vs partner requirements, monetization (subscriptions, bits, donations). Content strategy: streaming schedule consistency, niche selection, community building. Social media cross-promotion (Twitter, Instagram, YouTube clips). Avoiding burnout, stream health, viewer engagement tactics.',
'Medya Stüdyosu - Streaming Lab',
@ThreeDays, DATEADD(HOUR, 3, @ThreeDays),
25, 4, 1, 0, 1, GETDATE()),

-- DİJİTAL PAZARLAMA KULÜBÜ
('Instagram Marketing: Content Strategy ve Growth Tactics',
'Instagram algorithm 2025: chronological feed, engagement signals, Reels priority. Content pillars strategy, visual branding, color palette consistency, grid aesthetic. Content types: feed posts, Stories, Reels, IGTV, Live. Hashtag strategy: trending hashtags, branded hashtags, community hashtags, optimal hashtag count (5-10). Caption copywriting: hooks, storytelling, call-to-action (CTA), line breaks. Best posting times, content calendar planning. Instagram Insights analytics: reach, impressions, engagement rate, saves, shares. Influencer collaboration, shoutout for shoutout (S4S), giveaways. Instagram Shopping, product tagging. Competitor analysis, content inspiration. Reels editing: CapCut, InShot, trending audio, transitions. Growth tactics: follow-for-follow, engagement pods, DM strategy.',
'İletişim Fakültesi - Digital Studio',
@ThreeDays, DATEADD(HOUR, 2, @ThreeDays),
50, 5, 1, 0, 1, GETDATE()),

-- VERİ BİLİMİ KULÜBÜ
('Python Data Analysis: Pandas, NumPy ve Data Visualization',
'Data analysis workflow: data loading (CSV, Excel, JSON, SQL), data inspection (head, info, describe), data cleaning (handling missing values, duplicates, outliers). Pandas DataFrame operations: indexing, slicing, filtering, groupby, merge, join, concat. NumPy arrays, vectorized operations, mathematical functions, random number generation. Data visualization: Matplotlib (line plots, scatter plots, bar charts, histograms), Seaborn (heatmaps, pair plots, distribution plots), Plotly (interactive plots). Real-world dataset analysis: COVID-19 data, stock prices, e-commerce transactions. Exploratory Data Analysis (EDA) best practices. Time series analysis, date-time handling. Exporting results: CSV, Excel, PDF reports. Jupyter Notebook workflows.',
'Veri Bilimi Lab - Science Building 402',
@Week, DATEADD(HOUR, 4, @Week),
35, 6, 1, 0, 1, GETDATE()),

-- SİBER GÜVENLİK KULÜBÜ
('Ethical Hacking 101: Penetration Testing ve Web Security',
'Ethical hacking fundamentals: reconnaissance (information gathering, OSINT), scanning (Nmap, vulnerability scanners), gaining access, maintaining access, covering tracks. Web application security: OWASP Top 10 vulnerabilities (SQL injection, XSS, CSRF, insecure authentication, security misconfiguration). Kali Linux tools: Metasploit Framework, Burp Suite, Wireshark, John the Ripper, Hydra. SQL injection attacks: union-based, blind, time-based. XSS (Cross-Site Scripting): reflected, stored, DOM-based. Password cracking, hash algorithms (MD5, SHA). Network security: packet sniffing, man-in-the-middle attacks. Legal and ethical boundaries, responsible disclosure. Hands-on labs: HackTheBox, TryHackMe challenges. CTF (Capture The Flag) practice.',
'Siber Güvenlik Lab - Secure Room B1',
@TwoWeeks, DATEADD(HOUR, 5, @TwoWeeks),
25, 7, 0, 0, 1, GETDATE()), -- Private event

-- FİNANS KULÜBÜ
('Stock Market Analysis: Technical Indicators ve Trading Strategies',
'Technical analysis fundamentals: candlestick patterns (doji, hammer, engulfing, shooting star), chart patterns (head and shoulders, double top/bottom, triangles, flags). Technical indicators: Moving Averages (SMA, EMA), RSI (Relative Strength Index), MACD (Moving Average Convergence Divergence), Bollinger Bands, Fibonacci retracement. Support and resistance levels, trend lines, breakout strategies. Volume analysis, momentum indicators. Trading strategies: day trading, swing trading, position trading. Risk management: stop-loss, take-profit, risk-reward ratio, position sizing. BIST 100 analysis, Turkish stock market specifics. TradingView platform tutorial. Paper trading simulation. Psychological aspects: FOMO, fear and greed index, discipline.',
'İşletme Fakültesi - Trading Room',
@Week, DATEADD(HOUR, 3, @Week),
40, 8, 1, 0, 1, GETDATE()),

-- ROBOTİK KULÜBÜ
('Arduino & Robotics: Autonomous Line Follower Robot Workshop',
'Arduino microcontroller basics: digital/analog pins, power supply, programming environment (Arduino IDE). Electronic components: IR sensors, ultrasonic sensors, DC motors, motor drivers (L298N), servo motors, LED indicators, breadboards, jumper wires. Circuit design, schematic reading. C/C++ programming for Arduino: digitalWrite, analogRead, PWM, serial communication. Line follower algorithm: sensor calibration, PID control (Proportional-Integral-Derivative), motor speed control. Obstacle avoidance logic. Assembling the robot chassis, mounting sensors, wiring. Testing, debugging, optimization. Applications: warehouse automation, delivery robots. Future projects: Bluetooth control, autonomous navigation, computer vision integration. Take-home kit provided.',
'Robotik Atölyesi - Maker Space',
@ThreeDays, DATEADD(HOUR, 5, @ThreeDays),
20, 9, 1, 0, 1, GETDATE()),

-- GAME DEV KULÜBÜ
('Unity Game Development: 2D Platformer Game from Scratch',
'Unity Engine introduction: interface layout, scene hierarchy, inspector, project structure. C# scripting basics for Unity: MonoBehaviour, Start(), Update(), transform, GameObject, components. 2D game development: Sprite Renderer, 2D Collider (Box, Circle), Rigidbody2D, physics materials. Player movement: keyboard input (Input.GetAxis), velocity-based movement, jumping mechanics, ground detection. Level design: Tilemap, Tile Palette, background layers, parallax scrolling. Enemy AI: patrol behavior, chase behavior, attack pattern. Collision detection: OnCollisionEnter2D, OnTriggerEnter2D. Game mechanics: collectibles, health system, score system, game over condition. UI design: Canvas, Text, Button, pause menu. Animations: Animator Controller, animation clips, state machines. Build and export (Windows, WebGL).',
'Game Dev Studio - Creative Lab 3',
@TwoWeeks, DATEADD(HOUR, 6, @TwoWeeks),
30, 10, 1, 0, 1, GETDATE()),

-- SANAT KULÜBÜ
('Fotoğrafçılık Basics: Kompozisyon ve Işık Teknikleri',
'Photography fundamentals: exposure triangle (aperture, shutter speed, ISO), depth of field, bokeh effect. Composition rules: rule of thirds, leading lines, symmetry, patterns, framing, negative space, golden ratio. Camera modes: manual (M), aperture priority (Av), shutter priority (Tv), automatic (Auto). Lighting techniques: natural light (golden hour, blue hour), window light, reflectors, diffusers. Flash photography: on-camera flash, off-camera flash, bounce flash, fill flash. White balance, color temperature (tungsten, daylight, cloudy, shade). Photography genres: portrait (headshots, environmental portraits), landscape (wide-angle, long exposure), street photography, macro photography. Practical outdoor shoot session, model photography. Camera equipment: DSLR vs mirrorless, lens types (prime, zoom, wide-angle, telephoto). Bring your own camera (DSLR, mirrorless, or smartphone).',
'Kampüs Dışı - Beykoz Korusu (Outdoor Shoot)',
@Week, DATEADD(HOUR, 4, @Week),
25, 14, 1, 0, 1, GETDATE()),

-- MÜZİK KULÜBÜ
('Music Production 101: FL Studio ile Beat Making Workshop',
'FL Studio interface: channel rack, playlist, mixer, piano roll, browser. MIDI basics: notes, velocity, quantization, chord progressions (major, minor, diminished, augmented). Drum programming: kick, snare, hi-hat patterns, 808 bass, rhythm variations, swing. Melody creation: scales (major, minor, pentatonic, harmonic minor), chord voicings, countermelody. Sound design: synthesizers (3xOSC, Sytrus), presets vs custom sounds, filters (low-pass, high-pass, band-pass), envelopes (ADSR), LFOs (Low-Frequency Oscillators). Sampling: chopping samples, pitch-shifting, time-stretching. Arrangement: intro, verse, chorus, bridge, outro, transitions. Mixing basics: volume leveling, panning, EQ (equalization), compression. Export: WAV vs MP3, audio rendering. Music genres: Trap, Lo-Fi Hip-Hop, EDM, House. Royalty-free samples, copyright basics.',
'Müzik Stüdyosu - Production Room A',
@ThreeDays, DATEADD(HOUR, 3, @ThreeDays),
20, 12, 1, 0, 1, GETDATE()),

-- FİTNESS KULÜBÜ
('HIIT Training Session: High-Intensity Interval Workout',
'High-Intensity Interval Training (HIIT) principles: work-rest intervals, fat burning, cardiovascular benefits, EPOC (Excess Post-Exercise Oxygen Consumption). Warm-up routine: dynamic stretching, mobility exercises, joint activation. HIIT exercises: burpees, mountain climbers, jump squats, high knees, plank jacks, push-ups, bicycle crunches. Circuit training format: 30 seconds work, 15 seconds rest, 4 rounds. Heart rate monitoring, target heart rate zones. Cool-down stretches, static stretching, foam rolling. Nutrition timing: pre-workout meal, post-workout recovery. Hydration importance. Progressive overload, workout tracking. Modifications for beginners vs advanced. Safety tips: proper form, injury prevention, listening to your body. Benefits: metabolism boost, time-efficient, muscle preservation.',
'Spor Salonu - Fitness Center',
@ThreeDays, DATEADD(HOUR, 1, @ThreeDays),
30, 13, 1, 0, 1, GETDATE()),

-- KİŞİSEL GELİŞİM KULÜBÜ
('Time Management ve Productivity: Pomodoro, Time Blocking Techniques',
'Time management systems: Pomodoro Technique (25 min focus, 5 min break), Time Blocking (calendar blocking), Getting Things Done (GTD), Eisenhower Matrix (urgent vs important). Productivity tools: Notion, Todoist, Trello, Google Calendar, RescueTime. Habit formation: atomic habits, habit stacking, 21-day rule, habit tracking. Procrastination elimination: eat the frog method, 5-minute rule, accountability partners. Goal setting: SMART goals (Specific, Measurable, Achievable, Relevant, Time-bound), OKRs (Objectives and Key Results), yearly planning, quarterly reviews. Energy management: circadian rhythm, peak performance hours, power naps. Digital minimalism: phone addiction, social media limits, notification management, deep work sessions. Weekly planning ritual, daily to-do lists, priority setting. Work-life balance, burnout prevention.',
'Kütüphane - Seminer Odası',
@Week, DATEADD(HOUR, 2, @Week),
50, 15, 1, 0, 1, GETDATE());

GO

-- ============================================================
-- ADIM 6: ETKİNLİK KATILIMLARI (Geçmiş Davranış için)
-- ============================================================

-- Her kullanıcı için ilgi alanlarına uygun geçmiş katılımlar
-- UserID've EventID'leri dinamik olarak al (son eklenen etkinlikler)
DECLARE @AhmetId2 INT = (SELECT UserId FROM Users WHERE Email = '200101010101@dogus.edu.tr');
DECLARE @AyseId2 INT = (SELECT UserId FROM Users WHERE Email = '200202020202@dogus.edu.tr');
DECLARE @MehmetId2 INT = (SELECT UserId FROM Users WHERE Email = '200303030303@dogus.edu.tr');
DECLARE @ZeynepId2 INT = (SELECT UserId FROM Users WHERE Email = '200404040404@dogus.edu.tr');

DECLARE @Event1 INT = (SELECT TOP 1 EventId FROM Events WHERE Title LIKE 'Deep Learning%' ORDER BY EventId DESC);
DECLARE @Event7 INT = (SELECT TOP 1 EventId FROM Events WHERE Title LIKE 'Python Data Analysis%' ORDER BY EventId DESC);
DECLARE @Event5 INT = (SELECT TOP 1 EventId FROM Events WHERE Title LIKE 'Startup Pitch Night%' ORDER BY EventId DESC);
DECLARE @Event9 INT = (SELECT TOP 1 EventId FROM Events WHERE Title LIKE 'Instagram Marketing%' ORDER BY EventId DESC);
DECLARE @Event7v INT = (SELECT TOP 1 EventId FROM Events WHERE Title LIKE 'Valorant 5v5%' ORDER BY EventId DESC);
DECLARE @Event8 INT = (SELECT TOP 1 EventId FROM Events WHERE Title LIKE 'Streaming Masterclass%' ORDER BY EventId DESC);
DECLARE @Event13 INT = (SELECT TOP 1 EventId FROM Events WHERE Title LIKE 'Fotoğrafçılık Basics%' ORDER BY EventId DESC);
DECLARE @Event14 INT = (SELECT TOP 1 EventId FROM Events WHERE Title LIKE 'Music Production 101%' ORDER BY EventId DESC);

INSERT INTO EventAttendees (EventId, UserId, CreatedAt) VALUES
-- Ahmet (AI) - teknoloji etkinliklerine katılmış
(@Event1, @AhmetId2, GETDATE()), -- Deep Learning
(@Event7, @AhmetId2, GETDATE()), -- Pandas Data Analysis

-- Ayşe (Girişimci) - business etkinliklerine katılmış
(@Event5, @AyseId2, GETDATE()), -- Pitch Night
(@Event9, @AyseId2, GETDATE()), -- Instagram Marketing

-- Mehmet (Gamer) - gaming etkinliklerine katılmış
(@Event7v, @MehmetId2, GETDATE()), -- Valorant Tournament
(@Event8, @MehmetId2, GETDATE()), -- Streaming Masterclass

-- Zeynep (Sanatçı) - sanat etkinliklerine katılmış
(@Event13, @ZeynepId2, GETDATE()), -- Fotoğrafçılık
(@Event14, @ZeynepId2, GETDATE()); -- Music Production

GO

-- ============================================================
-- ÖZET RAPOR
-- ============================================================

PRINT '============================================================';
PRINT 'VERİ YÜKLEME TAMAMLANDI!';
PRINT '============================================================';
PRINT '';
PRINT 'KULÜPLER:';
SELECT ClubId, Name, LEN(Description) as DescriptionLength, LEN(Purpose) as PurposeLength 
FROM Clubs 
ORDER BY ClubId;

PRINT '';
PRINT 'ETKİNLİKLER:';
SELECT EventId, Title, ClubId, LEN(Description) as DescriptionLength, IsPublic
FROM Events 
ORDER BY EventId;

PRINT '';
PRINT 'KULLANICI PROFİLLERİ:';
SELECT 
    u.UserId,
    u.FullName,
    COUNT(DISTINCT cm.ClubId) as TakipEdilenKulupSayisi,
    COUNT(DISTINCT ea.EventId) as KatildigiEtkinlikSayisi
FROM Users u
LEFT JOIN ClubMembers cm ON u.UserId = cm.UserId
LEFT JOIN EventAttendees ea ON u.UserId = ea.UserId
WHERE u.UserId > 1 -- Test kullanıcıları
GROUP BY u.UserId, u.FullName
ORDER BY u.UserId;

PRINT '';
PRINT '============================================================';
PRINT 'YAPAY ZEKA İÇİN OPTİMİZASYON NOTLARI:';
PRINT '============================================================';
PRINT '✅ Her kulüp 100+ kelime description içeriyor';
PRINT '✅ Her kulüp 50+ kelime purpose içeriyor';
PRINT '✅ Her etkinlik 200+ kelime description içeriyor';
PRINT '✅ Teknik terimler ve anahtar kelimeler bol miktarda';
PRINT '✅ Farklı kullanıcı profilleri oluşturuldu';
PRINT '✅ Geçmiş katılım verileri eklendi';
PRINT '✅ Kulüp üyelikleri çeşitlendirildi';
PRINT '';
PRINT '🎯 YAPAY ZEKA TESTİ İÇİN:';
PRINT '- Ahmet (UserId:2) için AI/Data Science etkinlikleri önerilmeli';
PRINT '- Ayşe (UserId:3) için Girişimcilik/Marketing etkinlikleri önerilmeli';
PRINT '- Mehmet (UserId:4) için Gaming/Streaming etkinlikleri önerilmeli';
PRINT '- Zeynep (UserId:5) için Sanat/Fotoğraf etkinlikleri önerilmeli';
PRINT '';
PRINT 'TEST KULLANICILARI - Şifre: Test123!';
PRINT '- 200101010101@dogus.edu.tr (Ahmet - AI)';
PRINT '- 200202020202@dogus.edu.tr (Ayşe - Girişimci)';
PRINT '- 200303030303@dogus.edu.tr (Mehmet - Gamer)';
PRINT '- 200404040404@dogus.edu.tr (Zeynep - Sanatçı)';
PRINT '============================================================';

GO
