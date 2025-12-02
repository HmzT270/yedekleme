// src/pages/Home.jsx
import {
  Typography,
  Container,
  Box,
  Stack,
  Card,
  CardContent,
  Button,
  Chip,
  CircularProgress,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Divider,
  FormControlLabel,
  Checkbox,
  IconButton,
} from "@mui/material";
import FavoriteIcon from '@mui/icons-material/Favorite';
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder';
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useLocation, useSearchParams } from "react-router-dom";
import { api } from "../api/index";

export default function Home() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();

  // Mail'den parametrelerle gelindiyse
  const openEventIdFromParams = searchParams.get("openEventId");

  // Bildirimden gelindiyse: state.openEventId (eski yöntem)
  const openEventIdFromState = location.state?.openEventId;

  // Kullanıcı bilgisi (ileride gerekirse role bazlı içerik göstermek için)
  const user = useMemo(() => {
    try { return JSON.parse(localStorage.getItem("user") || "{}"); } catch { return {}; }
  }, []);
  const role = user?.role ?? null;
  const isAdmin = role === "Admin";
  const isManager = role === "Manager";

  // Eyaletler
  const [myClubs, setMyClubs] = useState([]);       // Katıldığım kulüpler
  const [clubsLoading, setClubsLoading] = useState(true);
  const [clubsErr, setClubsErr] = useState("");

  const [events, setEvents] = useState([]);         // Feed (takip ettiğim kulüplerin etkinlikleri)
  const [eventsLoading, setEventsLoading] = useState(true);
  const [eventsErr, setEventsErr] = useState("");
  
  // Favori etkinlikler
  const [favoriteIds, setFavoriteIds] = useState([]);
  
  // Filtre checkboxları
  const [showJoinedOnly, setShowJoinedOnly] = useState(false);
  const [showFavoritesOnly, setShowFavoritesOnly] = useState(false);

  // ---- Tarih/saat yardımcıları ----
  const parseAsUtc = (s) => {
    if (!s) return null;
    const hasTz = /[zZ]|[+\-]\d{2}:\d{2}$/.test(s);
    const iso = hasTz ? s : s + "Z";
    const d = new Date(iso);
    return isNaN(d.getTime()) ? null : d;
  };

  const fmt = (s) => {
    const d = parseAsUtc(s);
    return d
      ? d.toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" })
      : "-";
  };

  // Küçük bir yardımcı: aynı isteği önce /api/*, 404 olursa /*/ ile dene
  const getWithFallback = async (primary, fallback) => {
    try {
      return await api.get(primary);
    } catch (e) {
      const status = e?.response?.status;
      if (status === 404 && fallback) {
        return await api.get(fallback);
      }
      throw e;
    }
  };

  // Katıldığım kulüpleri çek (chip'ler için)
  useEffect(() => {
    let ignore = false;
    (async () => {
      setClubsLoading(true);
      setClubsErr("");
      try {
        const { data } = await getWithFallback("/api/Clubs/joined", "/Clubs/joined");
        if (!ignore) setMyClubs(Array.isArray(data) ? data : []);
      } catch (e) {
        if (!ignore) {
          setMyClubs([]);
          setClubsErr("Kulüp üyeliklerin yüklenemedi.");
        }
      } finally {
        if (!ignore) setClubsLoading(false);
      }
    })();
    return () => { ignore = true; };
  }, []);

  // FEED: Takip edilen kulüplerin etkinlikleri
  useEffect(() => {
    let ignore = false;
    (async () => {
      setEventsLoading(true);
      setEventsErr("");
      try {
        const { data } = await api.get("/api/Events/feed?includeCancelled=false&upcomingOnly=false");
        if (!ignore) setEvents(Array.isArray(data) ? data : []);
      } catch (e) {
        if (!ignore) {
          setEvents([]);
          setEventsErr("Etkinlik akışı yüklenemedi.");
        }
      } finally {
        if (!ignore) setEventsLoading(false);
      }
    })();
    return () => { ignore = true; };
  }, []);

  // TÜM etkinlikleri çek (favoriler için)
  const [allEvents, setAllEvents] = useState([]);
  useEffect(() => {
    let ignore = false;
    (async () => {
      try {
        const { data } = await api.get("/api/Events");
        if (!ignore) setAllEvents(Array.isArray(data) ? data : []);
      } catch (e) {
        if (!ignore) setAllEvents([]);
      }
    })();
    return () => { ignore = true; };
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

  // Feed'i tarihe göre sırala + küçük zaman toleransı uygula + checkbox filtresi uygula
  const myFeed = useMemo(() => {
    const TOLERANCE_MS = 12 * 60 * 60 * 1000;
    const now = Date.now();

    // Eğer favorileri veya katıldıklarımı göstermek istiyorsa, tüm etkinlikleri kullan; değilse feed'i kullan
    let source = (showFavoritesOnly || showJoinedOnly) ? allEvents : events;

    let filtered = (source || []).filter((e) => {
      const t = parseAsUtc(e?.startAt)?.getTime();
      return typeof t === "number" && t >= (now - TOLERANCE_MS);
    });
    
    // Checkbox filtrelerini uygula
    if (showJoinedOnly) {
      // Katıldığım etkinlikleri göster (herkese açık olanlar da dahil)
      filtered = filtered.filter(e => e.isJoined);
    }
    if (showFavoritesOnly) {
      filtered = filtered.filter(e => favoriteIds.includes(e.eventId));
    }

    return filtered.sort((a, b) => {
      const da = parseAsUtc(a?.startAt)?.getTime() ?? 0;
      const db = parseAsUtc(b?.startAt)?.getTime() ?? 0;
      return da - db;
    });
  }, [events, allEvents, showJoinedOnly, showFavoritesOnly, favoriteIds]);

  // Dinamik başlık
  const feedTitle = useMemo(() => {
    if (showFavoritesOnly && showJoinedOnly) {
      return "💜 Katıldığım Favori Etkinliklerim";
    } else if (showFavoritesOnly) {
      return "💜 Favori Etkinliklerim";
    } else if (showJoinedOnly) {
      return "💜 Katıldığım Etkinlikler";
    } else {
      return "✨ Takip Edilen Kulüp Etkinlikleri";
    }
  }, [showJoinedOnly, showFavoritesOnly]);

  // === Pop-up (sadece görüntüleme) için state ===
  const [detailOpen, setDetailOpen] = useState(false);
  const [selectedEvent, setSelectedEvent] = useState(null);
  const [initialEventOpened, setInitialEventOpened] = useState(false);

  const handleCardClick = (e) => {
    setSelectedEvent(e);   // feed’den gelen objeyi aynen koyuyoruz
    setDetailOpen(true);
  };

  const closeDetail = () => {
    setDetailOpen(false);
    setSelectedEvent(null);
  };

  // Bildirimden /home'a gelindiyse, ilgili etkinliğin pop-up'ını otomatik aç
  useEffect(() => {
    if (initialEventOpened) return;
    
    // URL parametresinden geldi mi kontrol et
    if (openEventIdFromParams) {
      if (!myFeed || myFeed.length === 0) return;
      const target = myFeed.find(ev => String(ev.eventId) === String(openEventIdFromParams));
      if (target) {
        handleCardClick(target);
        setInitialEventOpened(true);
      }
      return;
    }
    
    // Eski yöntem: state'den geldi mi kontrol et
    if (!openEventIdFromState) return;
    if (!myFeed || myFeed.length === 0) return;

    const target = myFeed.find(ev => String(ev.eventId) === String(openEventIdFromState));
    if (target) {
      handleCardClick(target);
      setInitialEventOpened(true);
    }
  }, [openEventIdFromParams, openEventIdFromState, myFeed, initialEventOpened]);

  return (
    <>
      <Container maxWidth="lg" sx={{ mt: { xs: 1, sm: 2 }, mb: { xs: 4, sm: 6 }, px: { xs: 2, sm: 3 } }}>
        {/* Hoş geldin mesajı */}
        <Box sx={{ mb: { xs: 3, sm: 4 }, mt: { xs: 1, sm: 2 } }}>
          <Typography variant="h4" sx={{ fontWeight: 700, mb: 1, fontSize: { xs: "1.75rem", sm: "2rem", md: "2.125rem" } }}>
            🎓 Hoş Geldin!
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ fontWeight: 500, fontSize: { xs: "0.875rem", sm: "1rem" } }}>
            Kampüsteki en güncel etkinlikleri keşfet ve arkadaşlarınla buluş
          </Typography>
        </Box>

        {/* Katıldığım Kulüpler */}
        <Box sx={{ mb: { xs: 3, sm: 4 } }}>
          <Typography variant="h5" sx={{ mb: 2, fontWeight: 600, fontSize: { xs: "1.25rem", sm: "1.5rem" } }}>
            💜 Takip Ettiğin Kulüpler
          </Typography>

          {clubsLoading ? (
            <Box sx={{ display: "flex", alignItems: "center", gap: 1.5 }}>
              <CircularProgress size={20} sx={{ color: "#6a4cff" }} /> 
              <Typography color="text.secondary">Yükleniyor…</Typography>
            </Box>
          ) : clubsErr ? (
            <Alert 
              severity="error" 
              sx={{ 
                borderRadius: 2,
                border: "1px solid rgba(211, 47, 47, 0.3)",
              }}
            >
              {clubsErr}
            </Alert>
          ) : myClubs.length === 0 ? (
            <Alert
              severity="info"
              sx={{ 
                borderRadius: 2,
                border: "1px solid rgba(2, 136, 209, 0.3)",
              }}
              action={
                <Button 
                  size="small" 
                  variant="contained" 
                  onClick={() => navigate("/clubs")}
                  sx={{
                    boxShadow: "0 2px 8px rgba(106, 76, 255, 0.25)",
                  }}
                >
                  Kulüpleri Gör
                </Button>
              }
            >
              Henüz herhangi bir kulübe katılmadın. Kulüplere katıl ve etkinlikleri burada gör.
            </Alert>
          ) : (
            <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
              {myClubs.map((c) => (
                <Chip 
                  key={c?.clubId ?? c?.name} 
                  label={c?.name ?? "Kulüp"} 
                  color="primary" 
                  variant="filled"
                  sx={{ 
                    fontWeight: 600,
                    transition: "all 0.2s ease-in-out",
                    "&:hover": {
                      transform: "translateY(-2px)",
                      boxShadow: "0 4px 12px rgba(106, 76, 255, 0.3)",
                    },
                  }}
                />
              ))}
            </Stack>
          )}
        </Box>

        {/* Feed: Katıldığım kulüplerin etkinlikleri */}
        <Box>
          <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2, flexWrap: "wrap", gap: 2 }}>
            <Typography variant="h5" sx={{ fontWeight: 600, fontSize: { xs: "1.25rem", sm: "1.5rem" } }}>
              {feedTitle}
            </Typography>
            <Stack direction="row" spacing={2}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={showJoinedOnly}
                    onChange={(e) => setShowJoinedOnly(e.target.checked)}
                  />
                }
                label="Katıldığım Etkinlikler"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={showFavoritesOnly}
                    onChange={(e) => setShowFavoritesOnly(e.target.checked)}
                  />
                }
                label="Favori Etkinliklerim"
              />
            </Stack>
          </Box>

          {eventsLoading ? (
            <Box sx={{ display: "flex", justifyContent: "center", py: 6 }}>
              <CircularProgress sx={{ color: "#6a4cff" }} />
            </Box>
          ) : eventsErr ? (
            <Alert 
              severity="error" 
              sx={{ 
                borderRadius: 2,
                border: "1px solid rgba(211, 47, 47, 0.3)",
              }}
            >
              {eventsErr}
            </Alert>
          ) : myClubs.length === 0 && !showFavoritesOnly && !showJoinedOnly ? (
            <Alert 
              severity="info" 
              sx={{ 
                borderRadius: 2,
                border: "1px solid rgba(2, 136, 209, 0.3)",
              }}
            >
              Kulüplere katıldığında, o kulüplerin etkinlikleri burada listelenecek.
            </Alert>
          ) : myFeed.length === 0 ? (
            <Alert 
              severity="info" 
              sx={{ 
                borderRadius: 2,
                border: "1px solid rgba(2, 136, 209, 0.3)",
              }}
            >
              Takip ettiğin kulüplere ait yaklaşan etkinlik bulunmuyor.
            </Alert>
          ) : (
            <Stack spacing={2}>
              {myFeed.map((e) => (
                <Card 
                  key={e.eventId}
                  onClick={() => handleCardClick(e)}
                  sx={{
                    cursor: "pointer",
                    transition: "all 0.2s ease-in-out",
                    position: "relative",
                    "&:hover": {
                      transform: "translateY(-4px) scale(1.01)",
                      boxShadow: "0 12px 32px rgba(106, 76, 255, 0.15)",
                    },
                  }}
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
                      {e.endAt && ` - ${fmt(e.endAt)}`}
                    </Typography>
                    {e.clubName && (
                      <Chip
                        label={e.clubName}
                        size="small"
                        color="primary"
                        sx={{ 
                          mt: 1.5, 
                          fontWeight: 600, 
                          fontSize: { xs: "0.7rem", sm: "0.75rem" },
                          transition: "all 0.2s ease-in-out",
                          "&:hover": {
                            transform: "translateY(-2px)",
                            boxShadow: "0 4px 12px rgba(106, 76, 255, 0.3)",
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

      {/* Sadece görüntüleme amaçlı pop-up */}
      <Dialog open={detailOpen} onClose={closeDetail} fullWidth maxWidth="sm">
        <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 1 }}>
          {selectedEvent?.title ?? "Etkinlik Detayı"}
          {selectedEvent?.isCancelled && (
            <Chip label="İptal Edildi" color="error" size="small" />
          )}
        </DialogTitle>
        <DialogContent dividers>
          {selectedEvent ? (
            <Stack spacing={1.5}>
              {selectedEvent.clubName && (
                <Chip label={selectedEvent.clubName} color="primary" variant="outlined" />
              )}
              <Divider />
              <Typography><strong>Yer:</strong> {selectedEvent.location}</Typography>
              <Typography><strong>Başlangıç:</strong> {fmt(selectedEvent.startAt)}</Typography>
              <Typography>
                <strong>Bitiş:</strong> {selectedEvent.endAt ? fmt(selectedEvent.endAt) : "-"}
              </Typography>
              <Typography><strong>Kontenjan:</strong> {selectedEvent.quota}</Typography>
              {selectedEvent.description && (
                <>
                  <Divider />
                  <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>Açıklama</Typography>
                  <Typography color="text.secondary" sx={{ whiteSpace: "pre-wrap" }}>
                    {selectedEvent.description}
                  </Typography>
                </>
              )}
            </Stack>
          ) : (
            <Typography color="text.secondary">Etkinlik bilgisi bulunamadı.</Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDetail}>Kapat</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
