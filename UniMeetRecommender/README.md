# UniMeet Recommendation Service

Python-based microservice for generating personalized event recommendations using hybrid collaborative and content-based filtering.

## Features

- **Hybrid Recommendation Engine**: Combines content similarity, temporal features, user affinity, and popularity
- **Real-time Processing**: Fresh recommendations on every request
- **Configurable**: Runtime-adjustable parameters via JSON config
- **RESTful API**: Flask-based HTTP API with JSON responses
- **Logging**: Structured JSON logging for monitoring
- **Database**: SQL Server integration via SQLAlchemy

## Architecture

```
Client Request
     ↓
Flask API (app.py)
     ↓
HybridRecommender (recommender.py)
     ├─→ FeatureEngine (feature_engine.py)
     │   ├─→ Content Similarity (TF-IDF)
     │   ├─→ Temporal Features
     │   ├─→ User Affinity
     │   └─→ Popularity Metrics
     └─→ DatabaseConnector (db_connector.py)
         └─→ SQL Server
```

## Installation

### Prerequisites

- Python 3.9 or higher
- SQL Server (LocalDB, Express, or Standard)
- pip (Python package manager)

### Setup

1. **Clone or navigate to the service directory**:
   ```bash
   cd UniMeetRecommender
   ```

2. **Install dependencies**:
   ```bash
   pip install -r requirements.txt
   ```

3. **Configure environment variables**:
   
   Copy `.env.example` to `.env`:
   ```bash
   copy .env.example .env
   ```
   
   Edit `.env` and set your values:
   ```env
   DB_CONNECTION_STRING=Server=localhost\SQLEXPRESS;Database=UniMeet;Trusted_Connection=True;TrustServerCertificate=True
   API_KEY=your-secret-api-key-change-this-in-production
   FLASK_ENV=development
   FLASK_DEBUG=1
   HOST=0.0.0.0
   PORT=5000
   LOG_LEVEL=INFO
   ```

4. **Test database connection**:
   ```bash
   python -c "from models.db_connector import DatabaseConnector; import os; from dotenv import load_dotenv; import json; load_dotenv(); config = json.load(open('config.json')); db = DatabaseConnector(os.getenv('DB_CONNECTION_STRING'), config['database']); print('Connected!' if db.test_connection() else 'Failed')"
   ```

## Running the Service

### Development Mode

```bash
python app.py
```

The service will start on `http://localhost:5000` (or the port specified in `.env`).

### Production Mode (with Gunicorn)

1. Install Gunicorn:
   ```bash
   pip install gunicorn
   ```

2. Run with multiple workers:
   ```bash
   gunicorn -w 4 -b 0.0.0.0:5000 app:app
   ```

## API Documentation

### Base URL
```
http://localhost:5000/api/v1
```

### Endpoints

#### 1. Health Check
**GET** `/health`

Check service health and database connectivity.

**Response**:
```json
{
  "status": "ok",
  "version": "0.1.0",
  "timestamp": "2025-12-03T10:15:30Z",
  "database": "connected"
}
```

---

#### 2. Get Recommendations
**POST** `/recommend`

Generate personalized event recommendations for a user.

**Request Body**:
```json
{
  "userId": 123,
  "limit": 10,
  "context": {
    "excludeEventIds": [1, 2, 3],
    "filters": {
      "minDate": "2025-12-03T00:00:00Z",
      "maxDate": "2025-12-31T23:59:59Z"
    }
  }
}
```

**Response**:
```json
{
  "recommendations": [
    {
      "eventId": 42,
      "score": 0.873,
      "reason": {
        "primary": "club_similarity",
        "details": "Similar to clubs you follow (similarity: 0.65)",
        "features": {
          "content_similarity": 0.65,
          "temporal_score": 0.80,
          "user_affinity": 0.92,
          "popularity": 0.45
        }
      }
    }
  ],
  "metadata": {
    "model_version": "0.1.0",
    "computed_at": "2025-12-03T10:15:30Z",
    "total_candidates": 45,
    "computation_time_ms": 23,
    "user_follows_clubs": 5
  }
}
```

---

#### 3. Get Configuration
**GET** `/config`

Retrieve current model configuration (read-only).

**Response**:
```json
{
  "model": {
    "version": "0.1.0",
    "type": "hybrid"
  },
  "scoring_weights": {
    "club_membership_match": 0.30,
    "content_similarity": 0.25,
    "temporal_score": 0.15,
    "user_past_behavior": 0.20,
    "club_popularity": 0.10
  }
}
```

---

#### 4. Update Configuration (Admin)
**PUT** `/config`

Update model configuration (requires API key).

**Headers**:
```
X-API-Key: your-secret-api-key
```

**Request Body**:
```json
{
  "scoring_weights": {
    "club_membership_match": 0.35,
    "content_similarity": 0.25
  }
}
```

**Response**:
```json
{
  "status": "updated",
  "timestamp": "2025-12-03T10:15:30Z"
}
```

---

#### 5. Reload Configuration (Admin)
**POST** `/reload-config`

Reload configuration from `config.json` (requires API key).

**Headers**:
```
X-API-Key: your-secret-api-key
```

**Response**:
```json
{
  "status": "reloaded",
  "timestamp": "2025-12-03T10:15:30Z",
  "version": "0.1.0"
}
```

---

#### 6. Service Statistics
**GET** `/stats`

Get service usage statistics.

**Response**:
```json
{
  "total_requests": 1523,
  "avg_latency_ms": 45.3,
  "last_request_time": "2025-12-03T10:15:30Z",
  "model_version": "0.1.0"
}
```

## Configuration

Edit `config.json` to adjust model behavior:

### Scoring Weights
```json
"scoring_weights": {
  "club_membership_match": 0.30,    // User follows event's club
  "content_similarity": 0.25,        // TF-IDF similarity
  "temporal_score": 0.15,            // Upcoming events preferred
  "user_past_behavior": 0.20,        // Past attendance/favorites
  "club_popularity": 0.10            // Club member/event count
}
```

### Content Settings
```json
"content_settings": {
  "tfidf_max_features": 200,         // Max TF-IDF features
  "min_similarity": 0.1,             // Minimum similarity threshold
  "use_turkish_stopwords": true
}
```

### Temporal Settings
```json
"temporal_settings": {
  "decay_days": 30,                  // Temporal decay period
  "prefer_upcoming": true,           // Prioritize upcoming events
  "max_days_ahead": 90,              // Max future days
  "recency_weight": 0.7              // Weight for recency
}
```

## Testing

### Manual API Testing

Using `curl`:

```bash
# Health check
curl http://localhost:5000/api/v1/health

# Get recommendations
curl -X POST http://localhost:5000/api/v1/recommend \
  -H "Content-Type: application/json" \
  -d '{"userId": 1, "limit": 5}'

# Get config
curl http://localhost:5000/api/v1/config

# Get stats
curl http://localhost:5000/api/v1/stats
```

Using PowerShell:

```powershell
# Health check
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/health" -Method Get

# Get recommendations
$body = @{
    userId = 1
    limit = 5
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/v1/recommend" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

## Troubleshooting

### Database Connection Issues

1. **Error: "Failed to connect to database"**
   - Check `DB_CONNECTION_STRING` in `.env`
   - Verify SQL Server is running
   - Test connection using SQL Server Management Studio

2. **Error: "Login failed for user"**
   - Ensure Windows Authentication is enabled
   - Or add SQL authentication to connection string:
     ```
     Server=localhost\SQLEXPRESS;Database=UniMeet;User Id=sa;Password=yourpassword;TrustServerCertificate=True
     ```

### Import Errors

1. **ModuleNotFoundError**
   - Ensure all dependencies are installed: `pip install -r requirements.txt`
   - Check Python version: `python --version` (should be 3.9+)

### Performance Issues

1. **Slow recommendations**
   - Check database query performance
   - Reduce `tfidf_max_features` in config
   - Enable database connection pooling

2. **High memory usage**
   - Reduce club cache TTL
   - Lower `pool_size` in config

## Deployment

### Windows Service (Production)

Use NSSM (Non-Sucking Service Manager):

1. Download NSSM: https://nssm.cc/download
2. Install service:
   ```cmd
   nssm install UniMeetRecommender "C:\Python39\python.exe" "C:\path\to\UniMeetRecommender\app.py"
   nssm set UniMeetRecommender AppDirectory "C:\path\to\UniMeetRecommender"
   nssm start UniMeetRecommender
   ```

### Linux Service (systemd)

Create `/etc/systemd/system/unimeet-recommender.service`:

```ini
[Unit]
Description=UniMeet Recommendation Service
After=network.target

[Service]
User=unimeet
WorkingDirectory=/opt/unimeet-recommender
Environment="PATH=/opt/unimeet-recommender/venv/bin"
ExecStart=/opt/unimeet-recommender/venv/bin/gunicorn -w 4 -b 127.0.0.1:5000 app:app
Restart=always

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable unimeet-recommender
sudo systemctl start unimeet-recommender
```

## Monitoring

### Logs

Logs are written to stdout in JSON format. Redirect to file:

```bash
python app.py > recommender.log 2>&1
```

### Log Format

```json
{
  "timestamp": "2025-12-03T10:15:30Z",
  "level": "INFO",
  "message": "Request completed: recommend",
  "module": "app",
  "userId": 123,
  "action": "recommend",
  "latency_ms": 45,
  "result_count": 10
}
```

## License

Internal use only - UniMeet Project

## Support

For issues or questions, contact the development team.
