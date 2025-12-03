# 🎯 Öneri Sistemi - Test ve Çalıştırma Kılavuzu

## 🚀 Hızlı Başlangıç

### 1️⃣ Python Servisini Başlat

```powershell
# UniMeetRecommender klasörüne git
cd UniMeetRecommender

# Virtual environment aktif et (varsa)
.\venv\Scripts\Activate.ps1

# Bağımlılıkları kontrol et
pip install -r requirements.txt

# Servisi başlat
python app.py
```

**Çıktı:**
```
INFO - Database connector initialized
INFO - HybridRecommender initialized, model_version=0.2.0
INFO - Flask server running on http://localhost:5000
```

### 2️⃣ .NET Backend'i Başlat

```powershell
# UniMeetApi klasörüne git
cd ..\UniMeetApi

# Projeyi çalıştır
dotnet run
```

**Çıktı:**
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5062
```

### 3️⃣ Frontend'i Başlat

```powershell
# client klasörüne git
cd ..\client

# Dev server'ı başlat
npm run dev
```

## 🧪 Test Senaryoları

### Test 1: Temel Öneri İsteği

**API:**
```http
GET http://localhost:5062/api/events/recommendations
Authorization: Bearer {your_token}
```

**Beklenen Yanıt:**
```json
[
  {
    "eventId": 10,
    "title": "Python Data Analysis: Pandas, NumPy",
    "score": 0.87,
    "recommendationReason": "highly_relevant",
    "reasonDetails": "Event content strongly matches your interests (82% match)",
    "reasonFeatures": {
      "content_similarity": 0.654,
      "title_match": 0.821,
      "temporal_score": 0.723,
      "user_affinity": 0.600,
      "popularity": 0.450
    },
    "clubId": 6,
    "clubName": "Data Science ve Business Intelligence Topluluğu",
    "isMember": false,
    "isJoined": false
  }
]
```

### Test 2: Debug Bilgisi

**API:**
```http
GET http://localhost:5062/api/events/recommendations-debug
Authorization: Bearer {your_token}
```

**Ne İçerir:**
- Kullanıcının takip ettiği kulüpler
- Tüm kulüpler
- Etkinlik sayıları
- Ham veri

### Test 3: Python Servisi Direkt Test

**API:**
```http
POST http://localhost:5000/api/v1/recommend
Content-Type: application/json

{
  "userId": 9,
  "limit": 10,
  "context": {
    "excludeEventIds": [],
    "filters": {
      "minDate": "2025-12-03T00:00:00Z"
    }
  }
}
```

**Beklenen Yanıt:**
```json
{
  "recommendations": [
    {
      "eventId": 10,
      "score": 0.8734,
      "reason": {
        "primary": "highly_relevant",
        "details": "Event content strongly matches your interests (82% match)",
        "features": {
          "content_similarity": 0.654,
          "title_match": 0.821,
          "temporal_score": 0.723,
          "user_affinity": 0.600,
          "popularity": 0.450
        }
      }
    }
  ],
  "metadata": {
    "model_version": "0.2.0",
    "computed_at": "2025-12-03T12:34:56.789Z",
    "total_candidates": 17,
    "computation_time_ms": 145.3,
    "user_follows_clubs": 1
  }
}
```

## 🔍 Öneri Sistemi Nasıl Çalışıyor?

### Adım 1: Kullanıcı Profili Çıkarma
```python
# Kullanıcının takip ettiği kulüplerin içeriği
user_interests = "cybersecurity, penetration testing, ethical hacking, 
                  network security, SIEM, malware analysis..."
```

### Adım 2: Etkinlik Analizi
```python
# Her etkinlik için:
event_content = "Title: Data Analysis Workshop
                 Description: SIEM, log analysis, security analytics...
                 Club: Data Science..."

# Benzerlik hesapla
similarity = TF-IDF_cosine(user_interests, event_content)
```

### Adım 3: Scoring
```python
final_score = (
    content_similarity * 0.20 +
    title_match * 0.15 +
    temporal_score * 0.15 +
    user_affinity * 0.15 +
    popularity * 0.05 +
    club_membership * 0.30
)

# Title match > 0.4 ise %15 bonus!
if title_match > 0.4:
    final_score *= 1.15
```

### Adım 4: Sıralama ve Çeşitlendirme
```python
# En yüksek skordan düşüğe sırala
sorted_events = sort_by_score(events)

# Diversity: Aynı kulüpten max 2 etkinlik
diverse_events = apply_diversity_filter(sorted_events)

return top_10(diverse_events)
```

## 📊 Özellik Karşılaştırması

| Özellik | v0.1.0 | v0.2.0 (Yeni) |
|---------|--------|---------------|
| Kulüp içeriği analizi | ✅ | ✅ |
| Etkinlik başlığı analizi | ❌ | ✅ |
| Etkinlik açıklaması analizi | ❌ | ✅ |
| Title match scoring | ❌ | ✅ |
| Öneri sebepleri | ❌ | ✅ |
| Feature breakdown | ❌ | ✅ |
| Semantic similarity | Basit | Gelişmiş |
| Keyword overlap | ❌ | ✅ |

## 🎯 Örnek Kullanıcı Profilleri

### Profil 1: Cybersecurity Kulübü Üyesi

**İlgi Alanları:**
- Penetration testing, ethical hacking
- Network security, SIEM
- Cryptography, malware analysis

**Önerilenler:**
1. ⭐⭐⭐⭐⭐ Data Science - SIEM log analysis
2. ⭐⭐⭐⭐⭐ IoT Robotics - IoT security
3. ⭐⭐⭐⭐ NLP/BERT - Malware detection
4. ⭐⭐⭐ Game Dev - Anti-cheat systems

### Profil 2: AI/ML Kulübü Üyesi

**İlgi Alanları:**
- Machine learning, deep learning
- Computer vision, NLP
- TensorFlow, PyTorch

**Önerilenler:**
1. ⭐⭐⭐⭐⭐ NLP/BERT Workshop
2. ⭐⭐⭐⭐⭐ Data Science with Python
3. ⭐⭐⭐⭐ Robotics - Computer vision
4. ⭐⭐⭐ Game Dev - AI pathfinding

## 🐛 Sorun Giderme

### Problem: Python servisi başlamıyor
```powershell
# Bağımlılıkları yeniden yükle
pip install --upgrade -r requirements.txt

# Database connection string kontrol et
# .env dosyasında DB_CONNECTION_STRING var mı?
```

### Problem: "No recommendations returned"
```powershell
# 1. Kullanıcı hiç kulüp takip etmiyor mu?
# → Debug endpoint'ine bak

# 2. Etkinlik var mı?
# → recommendations-debug ile kontrol et

# 3. Tarih filtresi çalışıyor mu?
# → Gelecek tarihli etkinlik var mı?
```

### Problem: Skorlar çok düşük
```python
# config.json'da min_score_threshold'u düşür
"ranking_settings": {
  "min_score_threshold": 0.01  # 0.05 → 0.01
}
```

## 📈 Performans Metrikleri

### Benchmark (17 etkinlik, 1 kulüp takipçisi):
- **Hesaplama süresi:** ~145ms
- **TF-IDF fitting:** ~45ms
- **Similarity calculation:** ~80ms
- **Scoring & ranking:** ~20ms

### Optimizasyon İpuçları:
1. Club vectors cache'leniyor (5 dakika TTL)
2. Database queries batch'leniyor
3. TF-IDF max_features=200 (performans/kalite dengesi)

---

**Son Güncelleme:** 3 Aralık 2025  
**Model Version:** 0.2.0  
**Status:** ✅ Production Ready
