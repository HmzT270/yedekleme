import { useEffect, useState, useMemo } from "react";
import {
  Typography, Container, Paper, Stack,
  TextField, Button, Snackbar, Alert, Box, FormControl,
  InputLabel, Select, MenuItem
} from "@mui/material";
import { useNavigate } from "react-router-dom";
import { api } from "../api/index";

export default function ManageEvents() {
  const navigate = useNavigate();

  // ---- Kullanıcı bilgisi (role & managedClubId) ----
  const user = useMemo(() => {
    try { return JSON.parse(localStorage.getItem("user") || "{}"); } catch { return {}; }
  }, []);
  const role = user?.role ?? null;
  const managedClubId = user?.managedClubId ?? null;
  const isManager = role === "Manager";
  const isAdmin = role === "Admin";

  // ---- Form state ----
  const [title, setTitle] = useState("");
  const [location, setLocation] = useState("");
  const [startDate, setStartDate] = useState(""); // YYYY-MM-DD
  const [startTime, setStartTime] = useState(""); // HH:mm
  const [endDate, setEndDate] = useState("");     // YYYY-MM-DD
  const [endTime, setEndTime] = useState("");     // HH:mm
  const [quota, setQuota] = useState("");
  const [clubId, setClubId] = useState("");
  const [description, setDescription] = useState("");
  const [isPublic, setIsPublic] = useState(true);

  // ---- UI state ----
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [okOpen, setOkOpen] = useState(false);

  // ---- Kulüp listesi ----
  const [clubs, setClubs] = useState([]);
  const [clubsLoading, setClubsLoading] = useState(true);

  // --- Helpers (bugünün tarih/saat stringleri) ---
  const pad = (n) => String(n).padStart(2, "0");

  const todayStr = useMemo(() => {
    const d = new Date();
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }, []);
  const nowTimeStr = useMemo(() => {
    const d = new Date();
    return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }, []);

  // Min değerler
  const startDateMin = todayStr;
  const startTimeMin = startDate === todayStr ? nowTimeStr : undefined;

  // End min: en az start ile aynı veya sonrası olmalı;
  // tarih aynıysa saat min = startTime
  const endDateMin = startDate || todayStr;
  const endTimeMin =
    endDate && startDate && endDate === startDate
      ? (startTime || nowTimeStr)
      : undefined;

  // ---- Kulüpleri yükle (Manager ise tek kulüp) ----
  useEffect(() => {
    let ignore = false;
    setClubsLoading(true);

    api.get("/api/Clubs")
      .then(res => {
        if (ignore) return;
        const list = Array.isArray(res.data) ? res.data : [];

        if (isManager && managedClubId) {
          const onlyMine = list.filter(c => c.clubId === managedClubId);
          setClubs(onlyMine);
          if (onlyMine.length > 0) setClubId(String(onlyMine[0].clubId));
        } else {
          setClubs(list);
        }
      })
      .catch(err => {
        console.error("Clubs fetch error:", err);
        setClubs([]);
      })
      .finally(() => { if (!ignore) setClubsLoading(false); });

    return () => { ignore = true; };
  }, [isManager, managedClubId]);

  // Datetime yardımcıları
  const toIso = (dateStr, timeStr) => {
    if (!dateStr || !timeStr) return null;
    const d = new Date(`${dateStr}T${timeStr}`);
    return isNaN(d.getTime()) ? null : d.toISOString();
  };
  const isPast = (dateStr, timeStr) => {
    if (!dateStr || !timeStr) return false;
    return new Date(`${dateStr}T${timeStr}`).getTime() < Date.now();
  };
  const compareDt = (d1, t1, d2, t2) => {
    // d1/t1 ile d2/t2'yi kıyasla: -1 küçük, 0 eşit, 1 büyük
    const a = new Date(`${d1}T${t1}`).getTime();
    const b = new Date(`${d2}T${t2}`).getTime();
    if (a < b) return -1;
    if (a > b) return 1;
    return 0;
  };

  // Doğrulama
  const validate = () => {
    if (!title.trim()) return "Etkinlik adı zorunludur.";
    if (!location.trim()) return "Etkinlik yeri zorunludur.";
    if (!startDate) return "Başlangıç tarihi zorunludur.";
    if (!startTime) return "Başlangıç saati zorunludur.";
    if (isPast(startDate, startTime))
      return "Geçmiş başlangıç tarih/saat seçilemez.";

    if (!endDate) return "Bitiş tarihi zorunludur.";
    if (!endTime) return "Bitiş saati zorunludur.";
    if (isPast(endDate, endTime))
      return "Geçmiş bitiş tarih/saat seçilemez.";

    // end >= start
    if (compareDt(endDate, endTime, startDate, startTime) < 0)
      return "Bitiş zamanı başlangıçtan önce olamaz.";

    if (!quota || isNaN(Number(quota)) || Number(quota) <= 0)
      return "Kontenjan pozitif bir sayı olmalıdır.";
    if (!clubId) return "Lütfen bir kulüp seçin.";
    if (isManager && managedClubId && parseInt(clubId, 10) !== managedClubId)
      return "Sadece yöneticisi olduğunuz kulüp için etkinlik oluşturabilirsiniz.";
    return "";
  };

  const hasErrors = !!validate();

  const handleSubmit = async () => {
    setError("");
    const v = validate();
    if (v) { setError(v); return; }

    setSubmitting(true);
    try {
      const payload = {
        title: title.trim(),
        location: location.trim(),
        startAt: toIso(startDate, startTime),
        endAt: toIso(endDate, endTime),
        quota: Number(quota),
        clubId: parseInt(clubId, 10),
        description: description.trim() || null,
        isPublic: isPublic,
      };

      await api.post("/api/Events", payload);

      setOkOpen(true);
      navigate("/home");
    } catch (e) {
      const msg = e?.response?.data || "Etkinlik oluşturulamadı.";
      setError(typeof msg === "string" ? msg : "Etkinlik oluşturulamadı.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <Container maxWidth="md" sx={{ mt: { xs: 2, sm: 4 }, mb: { xs: 4, sm: 6 }, px: { xs: 2, sm: 3 } }}>
        <Paper 
          elevation={0}
          sx={{ 
            p: { xs: 3, sm: 4 },
            borderRadius: 3,
            border: "1px solid rgba(106, 76, 255, 0.12)",
            boxShadow: "0 8px 32px rgba(106, 76, 255, 0.08)",
          }}
        >
          <Box sx={{ mb: 3, textAlign: "center" }}>
            <Box
              sx={{
                width: { xs: 50, sm: 60 },
                height: { xs: 50, sm: 60 },
                borderRadius: "50%",
                background: "linear-gradient(135deg, #6a4cff 0%, #8c6fff 100%)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                mx: "auto",
                mb: 2,
                fontSize: { xs: "1.5rem", sm: "1.8rem" },
                boxShadow: "0 6px 20px rgba(106, 76, 255, 0.25)",
              }}
            >
              ✨
            </Box>
            <Typography variant="h5" sx={{ fontWeight: 600, mb: 1, fontSize: { xs: "1.25rem", sm: "1.5rem" } }}>
              Yeni Etkinlik Oluştur
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ fontSize: { xs: "0.875rem", sm: "1rem" } }}>
              Öğrenciler için heyecan verici bir etkinlik düzenle
            </Typography>
          </Box>

          <Stack spacing={3}>
            {error && (
              <Alert 
                severity="error" 
                sx={{ 
                  borderRadius: 2,
                  border: "1px solid rgba(211, 47, 47, 0.3)",
                }}
              >
                {error}
              </Alert>
            )}

            <TextField 
              label="Etkinlik Adı" 
              value={title} 
              onChange={(e) => setTitle(e.target.value)} 
              required
              sx={{
                "& .MuiOutlinedInput-root": {
                  borderRadius: 2,
                  "&:hover": {
                    boxShadow: "0 4px 12px rgba(106, 76, 255, 0.08)",
                  },
                  "&.Mui-focused": {
                    boxShadow: "0 4px 16px rgba(106, 76, 255, 0.12)",
                  },
                },
              }}
            />
            <TextField 
              label="Yer" 
              value={location} 
              onChange={(e) => setLocation(e.target.value)} 
              required
              sx={{
                "& .MuiOutlinedInput-root": {
                  borderRadius: 2,
                  "&:hover": {
                    boxShadow: "0 4px 12px rgba(106, 76, 255, 0.08)",
                  },
                  "&.Mui-focused": {
                    boxShadow: "0 4px 16px rgba(106, 76, 255, 0.12)",
                  },
                },
              }}
            />

            {/* Başlangıç */}
            <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" }, gap: 2 }}>
              <TextField
                label="Başlangıç Tarihi"
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                InputLabelProps={{ shrink: true }}
                inputProps={{ min: startDateMin }}
                required
              />
              <TextField
                label="Başlangıç Saati"
                type="time"
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                InputLabelProps={{ shrink: true }}
                inputProps={startTimeMin ? { min: startTimeMin } : {}}
                required
              />
            </Box>

            {/* Bitiş */}
            <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" }, gap: 2 }}>
              <TextField
                label="Bitiş Tarihi"
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                InputLabelProps={{ shrink: true }}
                inputProps={{ min: endDateMin }}
                required
              />
              <TextField
                label="Bitiş Saati"
                type="time"
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                InputLabelProps={{ shrink: true }}
                inputProps={endTimeMin ? { min: endTimeMin } : {}}
                required
              />
            </Box>

            <TextField
              label="Kontenjan"
              type="number"
              inputProps={{ min: 1 }}
              value={quota}
              onChange={(e) => setQuota(e.target.value)}
              required
            />

            <FormControl fullWidth required>
              <InputLabel id="club-label">Kulüp</InputLabel>
              <Select
                labelId="club-label"
                label="Kulüp"
                value={clubId}
                onChange={(e) => setClubId(e.target.value)}
                disabled={clubsLoading || (isManager && !!managedClubId)}
              >
                {clubs.map((c) => (
                  <MenuItem key={c.clubId} value={String(c.clubId)}>
                    {c.name}
                  </MenuItem>
                ))}
                {!clubsLoading && clubs.length === 0 && (
                  <MenuItem disabled>Hiç kulüp bulunamadı</MenuItem>
                )}
              </Select>
            </FormControl>

            <TextField
              label="Etkinlik Açıklaması"
              multiline
              minRows={4}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />

            <FormControl fullWidth>
              <InputLabel id="visibility-label">Görünürlük</InputLabel>
              <Select
                labelId="visibility-label"
                label="Görünürlük"
                value={isPublic ? "public" : "members"}
                onChange={(e) => setIsPublic(e.target.value === "public")}
              >
                <MenuItem value="public">Herkese Açık</MenuItem>
                <MenuItem value="members">Sadece Kulüp Üyeleri</MenuItem>
              </Select>
            </FormControl>

            <Box sx={{ display: "flex", gap: 2, justifyContent: "flex-end", mt: 2, flexWrap: "wrap" }}>
              <Button 
                variant="outlined" 
                onClick={() => navigate("/home")}
                sx={{
                  borderRadius: 2,
                  textTransform: "none",
                  fontWeight: 600,
                  px: { xs: 2, sm: 3 },
                  fontSize: { xs: "0.875rem", sm: "1rem" },
                  "&:hover": {
                    backgroundColor: "rgba(106, 76, 255, 0.05)",
                  },
                }}
              >
                Vazgeç
              </Button>
              <Button 
                variant="contained" 
                onClick={handleSubmit} 
                disabled={submitting || hasErrors}
                sx={{
                  borderRadius: 2,
                  textTransform: "none",
                  fontWeight: 600,
                  px: { xs: 2, sm: 3 },
                  fontSize: { xs: "0.875rem", sm: "1rem" },
                  boxShadow: "0 6px 20px rgba(106, 76, 255, 0.3)",
                  "&:hover": {
                    transform: "translateY(-2px)",
                    boxShadow: "0 8px 28px rgba(106, 76, 255, 0.4)",
                  },
                }}
              >
                {submitting ? "Kaydediliyor..." : "🚀 Oluştur"}
              </Button>
            </Box>
          </Stack>
        </Paper>
      </Container>

      <Snackbar
        open={okOpen}
        autoHideDuration={2500}
        onClose={() => setOkOpen(false)}
        anchorOrigin={{ vertical: "top", horizontal: "right" }}
      >
        <Alert severity="success" variant="filled" sx={{ width: "100%" }}>
          Etkinlik başarıyla oluşturuldu.
        </Alert>
      </Snackbar>
    </>
  );
}
