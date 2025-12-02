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
import { useEffect, useMemo, useState } from "react";
import { api } from "../api/index";

export default function Events() {
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

  // Detay dialog state
  const [detailOpen, setDetailOpen] = useState(false);
  const [detail, setDetail] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailErr, setDetailErr] = useState("");
  const [notFound, setNotFound] = useState(false);

  // Edit state
  const [editMode, setEditMode] = useState(false);
  const [editSaving, setEditSaving] = useState(false);
  const [editErr, setEditErr] = useState("");

  // Delete state
  const [deleteAsk, setDeleteAsk] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [deleteErr, setDeleteErr] = useState("");

  // Edit form alanları
  const [title, setTitle] = useState("");
  const [location, setLocation] = useState("");
  const [startDate, setStartDate] = useState(""); // YYYY-MM-DD
  const [startTime, setStartTime] = useState(""); // HH:mm
  const [endDate, setEndDate] = useState("");     // YYYY-MM-DD
  const [endTime, setEndTime] = useState("");     // HH:mm
  const [quota, setQuota] = useState("");
  const [description, setDescription] = useState("");

  // === YENİ: Kulüp filtresi state'leri ===
  const [clubs, setClubs] = useState([]);
  const [selectedClubs, setSelectedClubs] = useState([]);
  // =======================================

  // Min tarih/saat
  const pad = (n) => String(n).padStart(2, "0");
  const todayStr = useMemo(() => {
    const d = new Date();
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }, []);
  const nowTimeStr = useMemo(() => {
    const d = new Date();
    return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }, []);

  const startTimeMin = startDate === todayStr ? nowTimeStr : undefined;
  const endDateMin = startDate || todayStr;
  const endTimeMin =
    endDate && startDate && endDate === startDate
      ? (startTime || nowTimeStr)
      : undefined;

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get("/api/Events");
        setEvents(data ?? []);
      } catch (err) {
        console.error("Etkinlikler alınamadı:", err);
      }
    })();
  }, []);

  // Favori etkinlikleri çek (giriş yapılmışsa)
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

  // === YENİ: Kulüpleri çek ===
  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get("/api/Clubs");
        // Beklenen: [{ clubId, name, ... }]
        setClubs(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error("Kulüpler alınamadı:", err);
        setClubs([]);
      }
    })();
  }, []);
  // ===========================

  // === YENİ: UTC güvenli parse + format ===
  const parseAsUtc = (s) => {
    if (!s) return null;
    const hasTz = /[zZ]|[+\-]\d{2}:\d{2}$/.test(s);
    const iso = hasTz ? s : s + "Z"; // Z ekleyerek UTC kabul et
    const d = new Date(iso);
    return isNaN(d.getTime()) ? null : d;
  };

  const fmt = (s) => {
    const d = parseAsUtc(s);
    return d
      ? d.toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" })
      : "-";
  };
  // === /YENİ ===

  const refreshList = async () => {
    try {
      const { data } = await api.get("/api/Events");
      setEvents(data ?? []);
    } catch {}
  };

  const openDetail = async (id) => {
    setDetailLoading(true);
    setDetailErr("");
    setNotFound(false);
    setEditMode(false);
    setDeleteAsk(false);
    setDetailOpen(true);

    try {
      const { data } = await api.get(`/api/Events/${id}`);
      setDetail(data);

      // formu doldur (UTC güvenli)
      if (data?.startAt) {
        const s = parseAsUtc(data.startAt);
        setStartDate(`${s.getFullYear()}-${pad(s.getMonth() + 1)}-${pad(s.getDate())}`);
        setStartTime(`${pad(s.getHours())}:${pad(s.getMinutes())}`);
      } else {
        setStartDate(""); setStartTime("");
      }
      if (data?.endAt) {
        const e = parseAsUtc(data.endAt);
        setEndDate(`${e.getFullYear()}-${pad(e.getMonth() + 1)}-${pad(e.getDate())}`);
        setEndTime(`${pad(e.getHours())}:${pad(e.getMinutes())}`);
      } else {
        setEndDate(""); setEndTime("");
      }
      setTitle(data?.title ?? "");
      setLocation(data?.location ?? "");
      setQuota(String(data?.quota ?? ""));
      setDescription(data?.description ?? "");
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
    setEditMode(false);
    setEditErr("");
    setDeleteAsk(false);
    setDeleteErr("");
    setStartDate(""); setStartTime("");
    setEndDate(""); setEndTime("");
    setTitle(""); setLocation(""); setQuota(""); setDescription("");
  };

  const closeDetail = () => {
    setDetailOpen(false);
    setTimeout(resetDetailState, 200);
  };

  const canEditOrDelete = !!detail && (isAdmin || (isManager && managedClubId === detail.clubId));

  // datetime yardımcıları / doğrulama
  const toIso = (d, t) => {
    if (!d || !t) return null;
    const x = new Date(`${d}T${t}`);
    return isNaN(x.getTime()) ? null : x.toISOString();
  };
  const isPast = (d, t) => {
    if (!d || !t) return false;
    return new Date(`${d}T${t}`).getTime() < Date.now();
  };
  const compareDt = (d1, t1, d2, t2) => {
    const a = new Date(`${d1}T${t1}`).getTime();
    const b = new Date(`${d2}T${t2}`).getTime();
    if (a < b) return -1;
    if (a > b) return 1;
    return 0;
  };

  const validateEdit = () => {
    if (!title.trim()) return "Etkinlik adı zorunludur.";
    if (!location.trim()) return "Etkinlik yeri zorunludur.";
    if (!startDate) return "Başlangıç tarihi zorunludur.";
    if (!startTime) return "Başlangıç saati zorunludur.";
    if (isPast(startDate, startTime)) return "Geçmiş başlangıç tarih/saat seçilemez.";

    if (!endDate) return "Bitiş tarihi zorunludur.";
    if (!endTime) return "Bitiş saati zorunludur.";
    if (isPast(endDate, endTime)) return "Geçmiş bitiş tarih/saat seçilemez.";

    if (compareDt(endDate, endTime, startDate, startTime) < 0)
      return "Bitiş zamanı başlangıçtan önce olamaz.";

    if (!quota || isNaN(Number(quota)) || Number(quota) <= 0)
      return "Kontenjan pozitif bir sayı olmalıdır.";
    return "";
  };

  const saveEdit = async () => {
    const v = validateEdit();
    if (v) { setEditErr(v); return; }
    setEditErr("");
    setEditSaving(true);
    try {
      await api.put(`/api/Events/${detail.eventId}`, {
        title: title.trim(),
        location: location.trim(),
        startAt: toIso(startDate, startTime),
        endAt: toIso(endDate, endTime),
        quota: Number(quota),
        clubId: detail.clubId,
        description: description.trim() || null,
        isCancelled: detail.isCancelled ?? false
      });

      const [listRes, detailRes] = await Promise.all([
        api.get("/api/Events"),
        api.get(`/api/Events/${detail.eventId}`)
      ]);
      setEvents(listRes.data ?? []);
      setDetail(detailRes.data ?? null);
      setEditMode(false);
    } catch (e) {
      setEditErr(e?.response?.data || "Kaydedilemedi.");
    } finally {
      setEditSaving(false);
    }
  };

  const confirmDelete = () => { setDeleteAsk(true); setDeleteErr(""); };
  const doDelete = async () => {
    if (!detail) return;
    setDeleting(true); setDeleteErr("");
    try {
      await api.delete(`/api/Events/${detail.eventId}`);
      await refreshList();
      closeDetail();
    } catch (e) {
      setDeleteErr(e?.response?.data || "Silme işlemi başarısız oldu.");
    } finally {
      setDeleting(false);
    }
  };

  // === YENİ: Seçime göre filtrelenmiş etkinlikler ===
  const filteredEvents = selectedClubs.length
    ? events.filter(e => selectedClubs.some(c => c?.clubId === e?.clubId))
    : events;
  // ================================================

  return (
    <>
      <Container maxWidth="lg" sx={{ px: { xs: 2, sm: 3 } }}>
        <Box sx={{ mt: { xs: 2, sm: 4 } }}>
          <Typography variant="h5" sx={{ mb: 1, fontWeight: 600, fontSize: { xs: "1.25rem", sm: "1.5rem" } }}>
            🎪 Tüm Etkinlikler
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3, fontWeight: 500, fontSize: { xs: "0.875rem", sm: "1rem" } }}>
            Kampüste düzenlenen tüm etkinliklere göz at
          </Typography>

          {/* === YENİ: Kulüp Çoklu Filtresi (Kısaltılmış) === */}
          <Box sx={{ mb: 3, maxWidth: { xs: "100%", sm: 360 } }}>
            <Autocomplete
              multiple
              size="small"
              sx={{ width: { xs: "100%", sm: 360 } }}
              options={clubs}
              value={selectedClubs}
              onChange={(_, val) => setSelectedClubs(val || [])}
              getOptionLabel={(o) => o?.name ?? ""}
              isOptionEqualToValue={(opt, val) => opt?.clubId === val?.clubId}
              renderInput={(params) => (
                <TextField
                  {...params}
                  size="small"
                  label="Kulüp Filtresi"
                  placeholder="Seçiniz"
                />
              )}
            />
          </Box>
          {/* === /YENİ === */}

          {filteredEvents.length === 0 ? (
            <Typography color="text.secondary">Henüz etkinlik bulunmuyor.</Typography>
          ) : (
            <Stack spacing={2}>
              {filteredEvents.map((e) => (
                <Card 
                  key={e.eventId} 
                  sx={{ 
                    cursor: "pointer",
                    transition: "all 0.2s ease-in-out",
                    border: "2px solid #6a4cff",
                    background: "linear-gradient(135deg, rgba(106, 76, 255, 0.05) 0%, rgba(140, 111, 255, 0.08) 100%)",
                    position: "relative",
                    "&:hover": {
                      transform: "translateY(-4px) scale(1.01)",
                      boxShadow: "0 12px 32px rgba(106, 76, 255, 0.3)",
                      border: "2px solid #8c6fff",
                      background: "linear-gradient(135deg, rgba(106, 76, 255, 0.1) 0%, rgba(140, 111, 255, 0.15) 100%)",
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

      {/* Detay + Düzenle + Sil Dialog */}
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
            editMode ? (
              <>
                {editErr && <Alert severity="error" sx={{ mb: 2 }}>{editErr}</Alert>}
                <Stack spacing={2}>
                  <TextField label="Etkinlik Adı" value={title} onChange={(e) => setTitle(e.target.value)} />
                  <TextField label="Yer" value={location} onChange={(e) => setLocation(e.target.value)} />

                  {/* Başlangıç */}
                  <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" }, gap: 2 }}>
                    <TextField
                      label="Başlangıç Tarihi"
                      type="date"
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                      InputLabelProps={{ shrink: true }}
                      inputProps={{ min: todayStr }}
                    />
                    <TextField
                      label="Başlangıç Saati"
                      type="time"
                      value={startTime}
                      onChange={(e) => setStartTime(e.target.value)}
                      InputLabelProps={{ shrink: true }}
                      inputProps={startTimeMin ? { min: startTimeMin } : {}}
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
                    />
                    <TextField
                      label="Bitiş Saati"
                      type="time"
                      value={endTime}
                      onChange={(e) => setEndTime(e.target.value)}
                      InputLabelProps={{ shrink: true }}
                      inputProps={endTimeMin ? { min: endTimeMin } : {}}
                    />
                  </Box>

                  <TextField
                    label="Kontenjan"
                    type="number"
                    inputProps={{ min: 1 }}
                    value={quota}
                    onChange={(e) => setQuota(e.target.value)}
                  />
                  <TextField
                    label="Açıklama"
                    multiline
                    minRows={4}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                  />
                </Stack>
              </>
            ) : (
              <Stack spacing={1.5}>
                {detail.clubName && (
                  <Chip label={detail.clubName} color="primary" variant="outlined" />
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
                {deleteErr && <Alert severity="error">{deleteErr}</Alert>}
              </Stack>
            )
          ) : null}
        </DialogContent>
        <DialogActions>
          {detail && (isAdmin || (isManager && managedClubId === detail.clubId)) && !detailLoading && !detailErr && !editMode && (
            <>
              {deleteAsk ? (
                <>
                  <Button onClick={() => setDeleteAsk(false)} disabled={deleting}>Vazgeç</Button>
                  <Button color="error" variant="contained" onClick={doDelete} disabled={deleting}>
                    {deleting ? "Siliniyor..." : "Sil"}
                  </Button>
                </>
              ) : (
                <>
                  <Button color="error" onClick={() => { setDeleteAsk(true); setDeleteErr(""); }}>Sil</Button>
                  <Button variant="outlined" onClick={() => setEditMode(true)}>Düzenle</Button>
                </>
              )}
            </>
          )}
          {editMode && (
            <>
              <Button onClick={() => setEditMode(false)} disabled={editSaving}>Vazgeç</Button>
              <Button variant="contained" onClick={saveEdit} disabled={editSaving}>
                {editSaving ? "Kaydediliyor..." : "Kaydet"}
              </Button>
            </>
          )}
          {/* Katıl/Ayrıl butonu detayda */}
          {detail && role === "Member" && (detail.isMember || detail.isPublic) && !detailLoading && !editMode && !detailErr && (
            detail.isJoined ? (
              <Button
                variant="outlined"
                color="error"
                disabled={false}
                onClick={async () => {
                  try {
                    await api.delete(`/api/Events/${detail.eventId}/join`);
                    const { data } = await api.get(`/api/Events/${detail.eventId}`);
                    setDetail(data);
                    await refreshList();
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
                    await refreshList();
                  } catch (err) {
                    alert("Katılım başarısız oldu: " + (err?.response?.data || ""));
                  }
                }}
              >Katıl</Button>
            )
          )}
          {detail && role === "Member" && !detail.isMember && !detail.isPublic && !detailLoading && !editMode && !detailErr && (
            <Button
              variant="outlined"
              onClick={async () => {
                try {
                  await api.post(`/api/Clubs/${detail.clubId}/follow`);
                  const { data } = await api.get(`/api/Events/${detail.eventId}`);
                  setDetail(data);
                  await refreshList();
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
