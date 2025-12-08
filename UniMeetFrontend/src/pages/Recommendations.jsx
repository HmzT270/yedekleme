import {
  Typography,
  Button,
  Container,
  Box,
  Card,
  CardContent,
  IconButton,
  Stack,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Chip,
  Divider,
  CircularProgress,
  Alert,
  TextField,
  Autocomplete
} from "@mui/material";
import FavoriteIcon from '@mui/icons-material/Favorite';
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder';
import LightbulbIcon from '@mui/icons-material/Lightbulb';
import { useEffect, useMemo, useState } from "react";
import { api } from "../api/index";

export default function Recommendations() {
  // Kullanıcı
  const user = useMemo(() => {
    try { return JSON.parse(localStorage.getItem("user") || "{}"); } catch { return {}; }
  }, []);
  const role = user?.role ?? null;
  const managedClubId = user?.managedClubId ?? null;
  const isManager = role === "Manager";
  const isAdmin = role === "Admin";

  const [events, setEvents] = useState([]);
  const [favoriteIds, setFavoriteIds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Detay dialog state
  const [detailOpen, setDetailOpen] = useState(false);
  const [detail, setDetail] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailErr, setDetailErr] = useState("");
  const [notFound, setNotFound] = useState(false);

  // UTC güvenli parse + format
  const parseAsUtc = (s) => {
    if (!s) return null;
    const hasTz = /[zZ]|[+\-]\d{2}:\d{2}$/.test(s);
    const iso = hasTz ? s : s + "Z";
    const d = new Date(iso);
    return isNaN(d.getTime()) ? null : d;
  };

  const pad = (n) => String(n).padStart(2, "0");

  const fmt = (s) => {
    const d = parseAsUtc(s);
    return d
      ? d.toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" })
      : "-";
  };

  // Önerileri yükle
  useEffect(() => {
    (async () => {
      setLoading(true);
      setError("");
      try {
        // Debug endpoint'ini çağır
        try {
          const debugRes = await api.get("/api/Events/recommendations-debug");
          console.log("📊 Debug Info:", debugRes.data);
        } catch (debugErr) {
          console.log("Debug endpoint hata:", debugErr);
        }

        let { data } = await api.get("/api/Events/recommendations");
        
        console.log("🤖 Yapay Zeka Önerileri:", data);
        console.log(`✅ ${data?.length || 0} etkinlik önerildi`);
        
        // Fallback: Eğer hiçbir etkinlik önerilmediyse, tüm etkinlikleri göster
        if (!data || data.length === 0) {
          console.log("⚠️ Fallback: Önerilen etkinlik yok, tüm etkinlikler gösteriliyor");
          const allEventsRes = await api.get("/api/Events");
          data = Array.isArray(allEventsRes.data) ? allEventsRes.data : [];
        }
        
        setEvents(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error("Öneriler yüklenemedi:", err);
        setError(err?.response?.data || "Öneriler yüklenemedi. Lütfen daha sonra tekrar deneyin.");
        setEvents([]);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  // Favori etkinlikleri çek
  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get('/api/Events/favorites');
        if (Array.isArray(data)) setFavoriteIds(data.map(d => d.eventId));
      } catch (err) {
        // silent fail if unauthorized
      }
    })();
  }, []);

  const openDetail = async (id) => {
    setDetailLoading(true);
    setDetailErr("");
    setNotFound(false);
    setDetailOpen(true);

    try {
      const { data } = await api.get(`/api/Events/${id}`);
      setDetail(data);
    } catch (e) {
      const status = e?.response?.status;
      if (status === 404) setNotFound(true);
      else setDetailErr(e?.response?.data || "Etkinlik detayı yüklenemedi.");
      setDetail(null);
    } finally {
      setDetailLoading(false);
    }
  };

  const resetDetailState = () => {
    setDetail(null);
    setDetailErr("");
    setNotFound(false);
  };

  const closeDetail = () => {
    setDetailOpen(false);
    setTimeout(resetDetailState, 200);
  };

  return (
    <>
      <Container maxWidth="lg" sx={{ px: { xs: 2, sm: 3 } }}>
        <Box sx={{ mt: { xs: 2, sm: 4 } }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 1 }}>
            <LightbulbIcon sx={{ fontSize: "2rem", color: "#6a4cff" }} />
            <Typography variant="h5" sx={{ fontWeight: 600, fontSize: { xs: "1.25rem", sm: "1.5rem" } }}>
              🤖 Yapay Zeka Önerileri
            </Typography>
          </Box>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3, fontWeight: 500, fontSize: { xs: "0.875rem", sm: "1rem" } }}>
            Takip ettiğiniz kulüplere benzer kulüplerden önerilen etkinlikler
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          {loading ? (
            <Box sx={{ display: "flex", justifyContent: "center", py: 6 }}>
              <CircularProgress sx={{ color: "#6a4cff" }} />
            </Box>
          ) : events.length === 0 ? (
            <Alert 
              severity="info" 
              sx={{ 
                borderRadius: 2,
                border: "1px solid rgba(2, 136, 209, 0.3)",
              }}
            >
              Henüz hiçbir etkinlik tavsiye edilemiyor. Lütfen bazı kulüpleri takip edin!
            </Alert>
          ) : (
            <Stack spacing={2}>
              {events.map((e) => (
                <Card 
                  key={e.eventId} 
                  sx={{ 
                    cursor: "pointer",
                    transition: "all 0.2s ease-in-out",
                    border: "2px solid #8c6fff",
                    background: "linear-gradient(135deg, rgba(140, 111, 255, 0.08) 0%, rgba(170, 140, 255, 0.12) 100%)",
                    position: "relative",
                    "&:hover": {
                      transform: "translateY(-4px) scale(1.01)",
                      boxShadow: "0 12px 32px rgba(140, 111, 255, 0.4)",
                      border: "2px solid #a68cff",
                      background: "linear-gradient(135deg, rgba(140, 111, 255, 0.15) 0%, rgba(170, 140, 255, 0.2) 100%)",
                    },
                  }} 
                  onClick={() => openDetail(e.eventId)}
                >
                  <IconButton
                    onClick={async (ev) => {
                      ev.stopPropagation();
                      try {
                        if (favoriteIds.includes(e.eventId)) {
                          await api.delete(`/api/Events/${e.eventId}/favorite`);
                          setFavoriteIds(prev => prev.filter(id => id !== e.eventId));
                        } else {
                          await api.post(`/api/Events/${e.eventId}/favorite`);
                          setFavoriteIds(prev => [...prev, e.eventId]);
                        }
                      } catch (err) {
                        console.error('Favori güncellenemedi', err);
                      }
                    }}
                    sx={{ position: 'absolute', right: 8, top: 8, zIndex: 10, bgcolor: 'rgba(255,255,255,0.9)' }}
                    size="small"
                  >
                    {favoriteIds.includes(e.eventId) ? (
                      <FavoriteIcon sx={{ color: '#e53935' }} />
                    ) : (
                      <FavoriteBorderIcon sx={{ color: '#6a4cff' }} />
                    )}
                  </IconButton>
                  <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
                    <Typography variant="h6" sx={{ fontWeight: 600, mb: 1, fontSize: { xs: "1.1rem", sm: "1.25rem" } }}>
                      {e.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5, fontSize: { xs: "0.875rem", sm: "0.875rem" } }}>
                      📍 {e.location}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ fontSize: { xs: "0.875rem", sm: "0.875rem" } }}>
                      🕒 {fmt(e.startAt)}
                    </Typography>
                    {e.endAt && (
                      <Typography variant="body2" color="text.secondary" sx={{ fontSize: { xs: "0.875rem", sm: "0.875rem" } }}>
                        ⏱ {fmt(e.endAt)}
                      </Typography>
                    )}
                    {e.clubName && (
                      <Chip 
                        label={`💡 ${e.clubName}`}
                        size="small" 
                        color="primary"
                        sx={{ 
                          mt: 1.5, 
                          fontWeight: 600, 
                          fontSize: { xs: "0.7rem", sm: "0.75rem" },
                          transition: "all 0.2s ease-in-out",
                          background: "linear-gradient(135deg, #8c6fff 0%, #a68cff 100%)",
                          "&:hover": {
                            transform: "translateY(-2px)",
                            boxShadow: "0 4px 12px rgba(140, 111, 255, 0.4)",
                          },
                        }}
                      />
                    )}
                  </CardContent>
                </Card>
              ))}
            </Stack>
          )}
        </Box>
      </Container>

      {/* Detay Dialog */}
      <Dialog open={detailOpen} onClose={closeDetail} fullWidth maxWidth="sm">
        <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 1 }}>
          {detail?.title ?? "Etkinlik Detayı"}
          {detail?.isCancelled && (
            <Chip label="İptal Edildi" color="error" size="small" />
          )}
        </DialogTitle>
        <DialogContent dividers>
          {detailLoading ? (
            <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
              <CircularProgress />
            </Box>
          ) : detailErr ? (
            <Alert severity="error">{String(detailErr)}</Alert>
          ) : notFound ? (
            <Alert severity="warning">Etkinlik bulunamadı.</Alert>
          ) : detail ? (
            <Stack spacing={1.5}>
              {detail.clubName && (
                <Chip label={`💡 ${detail.clubName}`} color="primary" variant="outlined" />
              )}
              <Divider />
              <Typography><strong>Yer:</strong> {detail.location}</Typography>
              <Typography><strong>Başlangıç:</strong> {fmt(detail.startAt)}</Typography>
              <Typography><strong>Bitiş:</strong> {detail.endAt ? fmt(detail.endAt) : "-"}</Typography>
              <Typography><strong>Katılımcı:</strong> {detail.attendeesCount ?? 0}/{detail.quota ?? "?"}</Typography>
              {detail.description && (
                <>
                  <Divider />
                  <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>Açıklama</Typography>
                  <Typography color="text.secondary" sx={{ whiteSpace: "pre-wrap" }}>
                    {detail.description}
                  </Typography>
                </>
              )}
            </Stack>
          ) : null}
        </DialogContent>
        <DialogActions>
          {/* Katıl/Ayrıl butonu */}
          {detail && role === "Member" && (detail.isMember || detail.isPublic) && !detailLoading && !detailErr && (
            detail.isJoined ? (
              <Button
                variant="outlined"
                color="error"
                onClick={async () => {
                  try {
                    await api.delete(`/api/Events/${detail.eventId}/join`);
                    const { data } = await api.get(`/api/Events/${detail.eventId}`);
                    setDetail(data);
                  } catch (err) {
                    alert("Ayrılma işlemi başarısız oldu.");
                  }
                }}
              >Ayrıl</Button>
            ) : (
              <Button
                variant="contained"
                color="primary"
                disabled={detail.attendeesCount >= detail.quota}
                onClick={async () => {
                  try {
                    await api.post(`/api/Events/${detail.eventId}/join`);
                    const { data } = await api.get(`/api/Events/${detail.eventId}`);
                    setDetail(data);
                  } catch (err) {
                    alert("Katılım başarısız oldu: " + (err?.response?.data || ""));
                  }
                }}
              >Katıl</Button>
            )
          )}
          {detail && role === "Member" && !detail.isMember && !detail.isPublic && !detailLoading && !detailErr && (
            <Button
              variant="outlined"
              onClick={async () => {
                try {
                  await api.post(`/api/Clubs/${detail.clubId}/follow`);
                  const { data } = await api.get(`/api/Events/${detail.eventId}`);
                  setDetail(data);
                } catch (err) {
                  alert("Kulübe katılma başarısız oldu: " + (err?.response?.data || ""));
                }
              }}
            >Kulübe Katıl</Button>
          )}
          <Button onClick={closeDetail}>Kapat</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
