# 🤖 UniMeet Yapay Zeka Öneri Sistemi - Kullanım Kılavuzu

## 📊 Yapay Zeka Nasıl Çalışıyor?

UniMeet'in yapay zeka sistemi **5 farklı faktörü** analiz ederek size en uygun etkinlikleri öneriyor.

---

## 🎯 Önem Sıralaması (Skorlama Ağırlıkları)

### 1️⃣ **Kulüp Üyeliği** (Ağırlık: %30) ⭐⭐⭐⭐⭐
**EN ÖNEMLİ FAKTÖR!**

- Takip ettiğiniz kulüplerin etkinliklerine **otomatik olarak yüksek skor** verilir
- Örnek: E-Spor Kulübü'nü takip ediyorsanız, onların "Valorant Turnuvası" etkinliği öncelikli önerilir

**👤 Kullanıcılar için:**
- ✅ İlgilendiğiniz kulüpleri mutlaka **takip edin**
- ✅ Profil > Kulüpler > "Takip Et" butonuna tıklayın

**👨‍💼 Kulüp Yöneticileri için:**
- ✅ Kulüp sayfanızı aktif tutun, üye sayısını artırın
- ✅ Kulüp açıklamanızı net yazın

---

### 2️⃣ **İçerik Benzerliği** (Ağırlık: %25) ⭐⭐⭐⭐
**AÇIKLAMA METİNLERİ ÇOK ÖNEMLİ!**

Yapay zeka, **kulüp açıklamaları** ve **etkinlik açıklamaları** arasındaki kelimeleri karşılaştırıyor.

**Nasıl Çalışır:**
1. Takip ettiğiniz kulüplerin **Description** ve **Purpose** alanlarındaki kelimeler analiz edilir
2. Tüm etkinliklerin **Title** ve **Description** alanları taranır
3. **Ortak kelimeler** bulunur ve benzerlik skoru hesaplanır
4. Benzer içerikli etkinlikler size önerilir

**📝 Örnek:**

**İYİ KULÜP AÇIKLAMASI:**
```
Kulüp Adı: Yapay Zeka ve Makine Öğrenmesi Kulübü

Description: 
"Python, TensorFlow, PyTorch kullanarak derin öğrenme projeleri geliştiriyoruz. 
Veri analizi, bilgisayarlı görü (computer vision), doğal dil işleme (NLP) ve 
tahmine dayalı modelleme üzerine çalışıyoruz. Kaggle yarışmalarına katılıyoruz."

Purpose:
"Öğrencilerin yapay zeka teknolojilerini öğrenmesi, gerçek dünya problemlerini 
çözmesi ve AI projelerini ürünleştirmesini sağlamak. Hackathon ve araştırma 
gruplarıyla iş birliği yapmak."
```

**ÖNERİLECEK ETKİNLİK:**
```
Etkinlik Adı: Deep Learning Workshop: CNN ile Görüntü Sınıflandırma

Description:
"Bu workshopta PyTorch kullanarak Convolutional Neural Network (CNN) 
eğiteceğiz. Veri ön işleme, model mimarisi ve transfer learning konularını 
işleyeceğiz. Kaggle veri setleri üzerinde pratik yapacağız."
```

**NEDEN ÖNERİLİR:**
- Ortak kelimeler: `yapay zeka`, `Python`, `PyTorch`, `derin öğrenme`, `veri`, `Kaggle`
- Benzerlik skoru: **0.78** (çok yüksek!)
- Final skor: 0.78 × %25 = **0.195** puan

---

**❌ KÖTÜ KULÜP AÇIKLAMASI:**
```
Kulüp Adı: AI Kulübü
Description: "Yapay zeka"
Purpose: "AI öğrenmek"
```

**SORUN:**
- Çok az kelime var
- Detay yok, teknik terim yok
- Yapay zeka benzerlikleri bulamıyor
- Benzerlik skoru: **0.02** (çok düşük!)

---

### 3️⃣ **Geçmiş Davranış** (Ağırlık: %20) ⭐⭐⭐

Daha önce **katıldığınız etkinliklere** benzer etkinlikler önerilir.

**Nasıl Çalışır:**
- Geçmişte hangi kulüplerin etkinliklerine katıldınız?
- O kulüplere benzer kulüplerin yeni etkinlikleri size önerilir

**Örnek:**
- "Teknoloji Kulübü" etkinliklerine katılmışsınız
- "Yapay Zeka Kulübü" etkinlikleri önerilir (benzer alan)

---

### 4️⃣ **Zaman Faktörü** (Ağırlık: %15) ⭐⭐⭐

**Yakın tarihteki** etkinlikler daha yüksek skor alır.

**Formül:**
- 1-7 gün içindeki etkinlikler: **Yüksek skor**
- 8-30 gün içindeki etkinlikler: **Orta skor**
- 30+ gün sonraki etkinlikler: **Düşük skor**

**👨‍💼 Kulüp Yöneticileri için:**
- ✅ Etkinliği **en az 1 hafta önceden** oluşturun
- ✅ **Çok erken** (3 ay öncesi) etkinlikler düşük skorlu olur

---

### 5️⃣ **Kulüp Popülerliği** (Ağırlık: %10) ⭐⭐

Daha popüler kulüplerin etkinlikleri **hafif bonus** alır.

**Popülerlik Kriterleri:**
- Kulübün **üye sayısı**
- Kulübün **toplam etkinlik sayısı**
- Kulübün **aktiflik oranı**

---

## 📖 DETAYLI KULLANIM KLAVUZU

### 👤 KULLANICILAR İÇİN

#### ✅ Daha İyi Öneriler Almak İçin:

1. **Kulüpleri Takip Edin**
   - En az **3-5 kulüp** takip edin
   - İlgi alanlarınıza uygun kulüpleri seçin
   - Kulüpler > "Takip Et" butonuna tıklayın

2. **Etkinliklere Katılın**
   - Yapay zeka **davranışlarınızı öğreniyor**
   - Daha fazla katıldıkça, öneriler **kişiselleşiyor**

3. **Profil Bilgilerinizi Güncel Tutun**
   - İlgi alanlarınızı belirtin (gelecek özellik)

---

### 👨‍💼 KULÜP YÖNETİCİLERİ İÇİN

#### ✅ Etkinliklerinizin Daha Çok Önerilmesi İçin:

### 1. **Detaylı ve Anahtar Kelime Zengin Açıklama Yazın**

**❌ YANLIŞ:**
```
Etkinlik: Workshop
Açıklama: "Kodlama öğreneceğiz"
```

**✅ DOĞRU:**
```
Etkinlik: Python ile Web Scraping Workshop: BeautifulSoup ve Selenium

Açıklama:
"Bu workshopta Python programlama dili kullanarak web scraping (veri kazıma) 
tekniklerini öğreneceğiz. BeautifulSoup kütüphanesi ile HTML parsing, Selenium 
ile dinamik sayfa otomasyonu ve pandas ile veri analizi yapacağız. 

Katılımcılar:
- Web sitelerinden otomatik veri toplama
- API entegrasyonu
- Veri temizleme ve CSV/Excel'e aktarma
- Etik ve yasal sorumluluklar

konularını öğrenecek. Temel Python bilgisi yeterlidir. Laptop getirmeniz 
önerilir."
```

**NEDEN DAHA İYİ:**
- Teknik terimler: `Python`, `BeautifulSoup`, `Selenium`, `web scraping`, `pandas`
- Detaylı içerik: Ne yapılacak açıkça yazılmış
- Hedef kitle: "Temel Python bilgisi" → Benzer kulüpleri takip edenler bulacak
- Kelime sayısı: 100+ kelime (yeterli veri)

---

### 2. **Kulüp Açıklamasını Zenginleştirin**

**❌ YANLIŞ:**
```
Kulüp: Teknoloji Kulübü
Description: "Teknoloji hakkında"
Purpose: "Teknoloji öğretmek"
```

**✅ DOĞRU:**
```
Kulüp: Teknoloji ve Yazılım Geliştirme Kulübü

Description:
"Frontend (React, Vue.js), backend (Node.js, Python Django), mobil uygulama 
(Flutter, React Native) ve DevOps (Docker, Kubernetes, CI/CD) teknolojileri 
üzerine çalışıyoruz. Hackathon organizasyonları, açık kaynak proje geliştirme, 
peer programming seansları ve teknik workshop'lar düzenliyoruz. GitHub, 
Stack Overflow ve LinkedIn üzerinden sektör profesyonelleriyle networking 
imkanı sunuyoruz."

Purpose:
"Öğrencilerin modern yazılım geliştirme teknolojilerini öğrenmesi, gerçek dünya 
projeleri geliştirmesi, kariyer fırsatlarına erişmesi ve teknoloji topluluğunda 
aktif rol almasını sağlamak. Startup ekosistemi ile köprü kurmak."
```

**FAYDALARI:**
- 50+ teknik terim ve anahtar kelime
- Net hedef kitle tanımı
- Detaylı aktivite listesi
- Yapay zeka **bu kelimeleri kullanarak** benzer etkinlikleri eşleştirebilir

---

### 3. **Anahtar Kelime Stratejisi**

**ÖNEMLİ KELİMELER (Kategorilere Göre):**

#### **Teknoloji Kulüpleri:**
```
Python, JavaScript, React, Node.js, AI, machine learning, deep learning,
data science, web development, mobile app, DevOps, cloud computing, 
AWS, Azure, Docker, Kubernetes, API, database, SQL, MongoDB, Git, 
GitHub, hackathon, coding, programming, software engineering
```

#### **Girişimcilik Kulüpleri:**
```
startup, pitch, investor, funding, business model, MVP, product market fit,
customer development, lean startup, venture capital, angel investor,
networking, mentorship, scaling, growth hacking, market research,
competitive analysis, revenue model, equity, valuation
```

#### **Sanat & Kültür Kulüpleri:**
```
tiyatro, sergi, müze, galeri, performans, sahne, sanat eseri, resim, heykel,
fotoğraf, sinema, film, kısa film, senaryo, oyunculuk, yönetmenlik,
yaratıcı yazarlık, şiir, edebiyat, müzik, konser, orkestra
```

#### **Spor Kulüpleri:**
```
futbol, basketbol, voleybol, fitness, yoga, pilates, koşu, maraton,
trekking, dağcılık, bisiklet, kamp, outdoor, doğa yürüyüşü, antrenman,
kondisyon, maç, turnuva, şampiyona, takım
```

#### **E-Spor Kulüpleri:**
```
League of Legends, Valorant, CS:GO, Dota 2, esport, gaming, streaming,
Twitch, tournament, competitive gaming, team strategy, coaching, 
gameplay analysis, Discord, scrim, ranked, meta, patch notes
```

---

### 4. **Etkinlik Başlığı Optimizasyonu**

**Formül:**
```
[ANA KONU] + [DETAY] + [AÇIKLAYICI EKLEME]
```

**✅ ÖRNEKLER:**

```
❌ Kötü: "Workshop"
✅ İyi: "React Hooks Workshop: useState ve useEffect Mastery"

❌ Kötü: "Networking Etkinliği"
✅ İyi: "Startup Founders Networking: Investor Pitch & Feedback Session"

❌ Kötü: "Spor Günü"
✅ İyi: "Campus 5K Run Marathon: Fitness Challenge & Health Talk"

❌ Kötü: "Oyun Gecesi"
✅ İyi: "Valorant 5v5 Tournament: Double Elimination Championship"

❌ Kötü: "Konser"
✅ İyi: "Akustik Canlı Performans: İndie Rock & Alternative Music Night"
```

---

## 🔬 YAPIAY ZEKA SKORLAMA ÖRNEĞİ

**Senaryo:**
- **Kullanıcı:** Ali
- **Takip Ettiği Kulüpler:** 
  - Yapay Zeka Kulübü
  - E-Spor Kulübü
- **Geçmiş Katılımlar:** 
  - "Python Workshop" (Teknoloji Kulübü)
  - "Valorant Turnuvası" (E-Spor Kulübü)

---

### **Etkinlik A: "Deep Learning ile Görüntü İşleme Workshop"**

**Kulüp:** Yapay Zeka Kulübü (Takip ediliyor ✅)

**Açıklama:**
"TensorFlow ve Keras kullanarak CNN (Convolutional Neural Network) mimarisi 
geliştireceğiz. CIFAR-10 veri setinde görüntü sınıflandırma, transfer learning 
ve model optimizasyonu konularını işleyeceğiz."

**Skor Hesaplama:**

1. **Kulüp Üyeliği:** Ali takip ediyor → **1.0** × 0.30 = **0.30**
2. **İçerik Benzerliği:** 
   - Ortak kelimeler: `yapay zeka`, `TensorFlow`, `görüntü`, `model`
   - Benzerlik: **0.65** × 0.25 = **0.16**
3. **Geçmiş Davranış:**
   - "Python Workshop"a benzer → **0.40** × 0.20 = **0.08**
4. **Zaman:** 5 gün sonra → **0.85** × 0.15 = **0.13**
5. **Popülerlik:** Kulübün 120 üyesi var → **0.70** × 0.10 = **0.07**

**TOPLAM SKOR: 0.74** ⭐⭐⭐⭐⭐ (ÇOK YÜKSEK!)

---

### **Etkinlik B: "Kahve Saati: Sohbet Etkinliği"**

**Kulüp:** Sosyal Aktiviteler Kulübü (Takip edilmiyor ❌)

**Açıklama:**
"Rahat bir ortamda kahve içerek tanışma etkinliği."

**Skor Hesaplama:**

1. **Kulüp Üyeliği:** Takip edilmiyor → **0.0** × 0.30 = **0.00**
2. **İçerik Benzerliği:** 
   - Ortak kelime yok → **0.05** × 0.25 = **0.01**
3. **Geçmiş Davranış:**
   - Benzer etkinlik yok → **0.10** × 0.20 = **0.02**
4. **Zaman:** 3 gün sonra → **0.90** × 0.15 = **0.14**
5. **Popülerlik:** Kulübün 80 üyesi var → **0.50** × 0.10 = **0.05**

**TOPLAM SKOR: 0.22** ⭐⭐ (DÜŞÜK - ÖNERİLMEZ)

---

## 📈 BAŞARI İPUÇLARI

### 👨‍💼 Kulüp Yöneticileri:

✅ **Yapılacaklar:**
1. **En az 100 kelimelik** etkinlik açıklaması yazın
2. **5-10 anahtar kelime** ekleyin (teknik terimler, konular)
3. **Hedef kitle** belirtin (başlangıç/orta/ileri seviye)
4. **Ne öğrenilecek** açıkça yazın
5. **Gereksinimler** belirtin (laptop, ön bilgi, vb.)
6. Kulüp açıklamasını **ayda bir güncelleyin**
7. **Tutarlı** terminoloji kullanın

❌ **Yapılmaması Gerekenler:**
1. Genel/belirsiz açıklamalar yazmak ("Eğlenceli etkinlik")
2. Tek kelime açıklamalar ("Workshop")
3. Teknik detay vermemek
4. Copy-paste açıklamalar (her etkinlik benzemesin)

---

### 👤 Kullanıcılar:

✅ **Yapılacaklar:**
1. **İlgilendiğiniz tüm kulüpleri** takip edin
2. **Düzenli olarak** etkinliklere katılın
3. **Farklı kategorilerden** kulüpler takip edin (çeşitlilik)

---

## 🎓 ÖZET: YAPAY ZEKA PUANLAMA TABLOSU

| Faktör | Ağırlık | Nasıl Artırılır |
|--------|---------|-----------------|
| **Kulüp Üyeliği** | %30 | Kullanıcı takip etsin / Kulüp üye sayısını artır |
| **İçerik Benzerliği** | %25 | Detaylı açıklama + Anahtar kelimeler |
| **Geçmiş Davranış** | %20 | Kullanıcı etkinliklere katılsın |
| **Zaman** | %15 | 1-2 hafta içindeki etkinlikler oluştur |
| **Popülerlik** | %10 | Kulüp aktifliğini artır |

---

## 🚀 HIZLI BAŞLANGIÇ ŞABLONLARı

### Şablon 1: Teknoloji Etkinliği

```markdown
Etkinlik Adı: [Teknoloji] Workshop: [Spesifik Konu]

Açıklama:
Bu workshopta [teknoloji adı] kullanarak [proje/çıktı] geliştireceğiz.

Konu başlıkları:
- [Konu 1]: [Detay]
- [Konu 2]: [Detay]
- [Konu 3]: [Detay]

Katılımcılar şunları öğrenecek:
✅ [Öğrenim 1]
✅ [Öğrenim 2]
✅ [Öğrenim 3]

Gereksinimler:
- [Seviye] düzey [ön bilgi]
- Laptop (önerilir)

Kontenjan: [Sayı] kişi
Süre: [X] saat
```

### Şablon 2: Girişimcilik Etkinliği

```markdown
Etkinlik Adı: [Konu] + [Format]: [Değer Önerisi]

Açıklama:
[Açıklayıcı paragraf - 3-4 cümle]

Etkinlik formatı:
🎯 [Bölüm 1]: [Ne yapılacak]
🎯 [Bölüm 2]: [Ne yapılacak]
🎯 [Bölüm 3]: [Ne yapılacak]

Kimler katılmalı:
- [Hedef kitle 1]
- [Hedef kitle 2]

Katılımcılar ne kazanır:
✨ [Fayda 1]
✨ [Fayda 2]
✨ [Networking fırsatı]
```

---

## 📞 DESTEK

Sorularınız için:
- **Admin Paneli** > Yapay Zeka Ayarları
- **E-posta:** [destek@unimeet.com]

---

**Son Güncelleme:** Aralık 2025  
**Versiyon:** 1.0  
**Yapay Zeka Modeli:** Hybrid Recommender v0.1.0
