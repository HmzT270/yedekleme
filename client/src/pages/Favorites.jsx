import { Container, Typography, Stack, Card, CardContent, Chip, Box, Dialog, DialogTitle, DialogContent, DialogActions, Button, CircularProgress, Alert, Divider } from "@mui/material";
import { useEffect, useState } from "react";
import { api } from "../api/index";

export default function Favorites() {
  const [events, setEvents] = useState([]);

  // Detail dialog state (same UX as Events.jsx)
  const [detailOpen, setDetailOpen] = useState(false);
  const [detail, setDetail] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailErr, setDetailErr] = useState("");
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get('/api/Events/favorites');
        setEvents(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error('Favoriler alınamadı', err);
        setEvents([]);
      }
    })();
  }, []);

  // UTC-safe parse + format
  const parseAsUtc = (s) => {
    if (!s) return null;
    const hasTz = /[zZ]|[+\-]\d{2}:\d{2}$/.test(s);
    const iso = hasTz ? s : s + 'Z';
    const d = new Date(iso);
    return isNaN(d.getTime()) ? null : d;
  };

  const fmt = (s) => {
    const d = parseAsUtc(s);
    return d ? d.toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' }) : '-';
  };

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
      else setDetailErr(e?.response?.data || 'Etkinlik detayı yüklenemedi.');
      setDetail(null);
    } finally {
      setDetailLoading(false);
    }
  };

  const closeDetail = () => {
    setDetailOpen(false);
    setTimeout(() => {
      setDetail(null); setDetailErr(""); setNotFound(false);
    }, 200);
  };

  return (
    <Container maxWidth="lg" sx={{ px: { xs: 2, sm: 3 } }}>
      <Box sx={{ mt: { xs: 2, sm: 4 } }}>
        <Typography variant="h5" sx={{ mb: 1, fontWeight: 600 }}>⭐ Favori Etkinliklerim</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>Favori olarak işaretlediğin etkinlikler burada görünür.</Typography>

        {events.length === 0 ? (
          <Typography color="text.secondary">Henüz favori etkinliğin yok.</Typography>
        ) : (
          <Stack spacing={2}>
            {events.map(e => (
              <Card key={e.eventId} sx={{ border: '2px solid #6a4cff', cursor: 'pointer' }} onClick={() => openDetail(e.eventId)}>
                <CardContent>
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>{e.title}</Typography>
                  <Typography color="text.secondary">📍 {e.location} • 🕒 {fmt(e.startAt)}</Typography>
                  {e.clubName && <Chip label={e.clubName} size="small" sx={{ mt: 1 }} />}
                </CardContent>
              </Card>
            ))}
          </Stack>
        )}
      </Box>

      {/* Detail dialog (reuses Events.jsx style) */}
      <Dialog open={detailOpen} onClose={closeDetail} fullWidth maxWidth="sm">
        <DialogTitle>{detail?.title ?? 'Etkinlik Detayı'}</DialogTitle>
        <DialogContent dividers>
          {detailLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
          ) : detailErr ? (
            <Alert severity="error">{String(detailErr)}</Alert>
          ) : notFound ? (
            <Alert severity="warning">Etkinlik bulunamadı.</Alert>
          ) : detail ? (
            <>
              {detail.clubName && <Chip label={detail.clubName} color="primary" variant="outlined" />}
              <Divider sx={{ my: 1 }} />
              <Typography><strong>Yer:</strong> {detail.location}</Typography>
              <Typography><strong>Başlangıç:</strong> {fmt(detail.startAt)}</Typography>
              <Typography><strong>Bitiş:</strong> {detail.endAt ? fmt(detail.endAt) : '-'}</Typography>
              <Typography><strong>Kontenjan:</strong> {detail.quota}</Typography>
              {detail.description && (
                <>
                  <Divider sx={{ my: 1 }} />
                  <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>Açıklama</Typography>
                  <Typography color="text.secondary" sx={{ whiteSpace: 'pre-wrap' }}>{detail.description}</Typography>
                </>
              )}
            </>
          ) : null}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDetail}>Kapat</Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}
