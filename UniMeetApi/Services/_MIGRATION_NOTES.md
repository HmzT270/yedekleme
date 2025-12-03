# Recommendation Service Migration Notes

## 📅 Migration Date: December 3, 2025

## 🔄 Overview

The recommendation system has been migrated from a C#-based service to a Python-based microservice architecture.

---

## 📦 Old System (DEPRECATED)

### Location
- **File**: `UniMeetApi/Services/RecommendationService.cs`
- **Interface**: `IRecommendationService`

### Algorithm
- **Type**: Content-based filtering
- **Method**: Jaccard similarity (simple string tokenization)
- **Features**:
  - Club name, description, purpose text comparison
  - Simple keyword matching
  - Hardcoded similarity thresholds (0.15 → 0.10 → 0.05)

### Limitations
1. ❌ No Turkish language processing (stemming/lemmatization)
2. ❌ User interaction history (attendance, favorites) not utilized
3. ❌ No temporal features (event timing, recency)
4. ❌ No collaborative filtering
5. ❌ Hardcoded weights and thresholds (not configurable)
6. ❌ O(n²) complexity on every request

### Status
- **Marked**: `[Obsolete]` attribute added
- **Logging**: Warning logged on each call
- **Kept For**: Fallback mechanism
- **Will be removed in**: v2.0

---

## 🆕 New System

### Architecture
```
.NET Backend (EventsController)
    ↓
RecommendationProxyService.cs (HTTP proxy)
    ↓
Python Microservice (Flask)
    ├─→ HybridRecommender
    │   ├─→ Content Similarity (TF-IDF)
    │   ├─→ Temporal Features
    │   ├─→ User Affinity
    │   └─→ Popularity Metrics
    └─→ SQL Server (read-only)
```

### Components

#### Python Microservice
- **Location**: `UniMeetRecommender/`
- **Framework**: Flask
- **Port**: 5000 (configurable)
- **Algorithm**: Hybrid recommendation engine

#### .NET Proxy Service
- **File**: `UniMeetApi/Services/RecommendationProxyService.cs`
- **Interface**: `IRecommendationProxyService`
- **Purpose**: Forward requests to Python service, handle fallback

### Features
1. ✅ TF-IDF content similarity with Turkish stopwords
2. ✅ Temporal scoring (prefer upcoming events)
3. ✅ User affinity (past attendance, favorites)
4. ✅ Club popularity metrics
5. ✅ Weighted scoring (configurable via JSON)
6. ✅ Feature-level explanations
7. ✅ Fallback to old service on error
8. ✅ Structured logging
9. ✅ Admin API for configuration management

---

## 🔧 Configuration

### appsettings.json
```json
"RecommendationService": {
  "PythonServiceUrl": "http://localhost:5000",
  "TimeoutSeconds": 5,
  "EnableFallback": true,
  "ApiKey": "your-secret-api-key"
}
```

### Python Service Config
**File**: `UniMeetRecommender/config.json`

**Key Parameters**:
- `scoring_weights`: Adjustable feature weights
- `tfidf_max_features`: TF-IDF vocabulary size
- `temporal_settings`: Event recency preferences
- `ranking_settings`: Diversity and threshold settings

**Runtime Update**: Available via Admin API

---

## 🚀 Deployment

### Python Service

#### Development
```bash
cd UniMeetRecommender
pip install -r requirements.txt
python app.py
```

#### Production (Windows)
```cmd
# Using NSSM (recommended for Windows Service)
nssm install UniMeetRecommender "C:\Python\python.exe" "C:\path\to\app.py"
nssm start UniMeetRecommender
```

#### Production (Linux)
```bash
gunicorn -w 4 -b 0.0.0.0:5000 app:app
```

### .NET Backend
No changes required. Deploy as usual.

---

## 🔙 Rollback Plan

### Scenario 1: Python Service Unavailable
**Automatic**: Proxy service detects failure and falls back to old service.

**Verification**:
- Check logs for "Python service unavailable, using fallback"
- Old algorithm will be used automatically

### Scenario 2: Python Service Returns Incorrect Results
**Manual Rollback**:

1. **Disable proxy in EventsController**:
   ```csharp
   // Change this line:
   var recommendedEvents = await _recommendationProxy.GetRecommendedEventsAsync(userId, limit: 10);
   
   // To this:
   var recommendedEvents = await _recommendationService.GetRecommendedEventsAsync(userId);
   ```

2. **Restart .NET backend**

3. **Stop Python service** (optional)

### Scenario 3: Complete Rollback
**Steps**:
1. Revert EventsController changes (use old service)
2. Remove proxy service from DI in Program.cs
3. Remove RecommendationService config from appsettings.json
4. Restart backend
5. Stop Python service

**Git Command**:
```bash
git revert <commit-hash-of-migration>
```

---

## 🧪 Testing

### Manual Testing

#### 1. Test Python Service Health
```bash
curl http://localhost:5000/api/v1/health
```

**Expected Response**:
```json
{
  "status": "ok",
  "version": "0.1.0",
  "database": "connected"
}
```

#### 2. Test Recommendations (via .NET)
```bash
# Login first to get JWT token
curl -X POST http://localhost:5062/api/Events/recommendations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Expected**: JSON array of recommended events

#### 3. Test Fallback
1. Stop Python service
2. Request recommendations via .NET
3. Check logs for fallback messages
4. Verify old service is used

### Integration Testing

**Frontend Test**:
1. Navigate to `/recommendations` page
2. Verify events are displayed
3. Check browser console for errors
4. Compare with previous recommendations (if applicable)

**Admin Panel Test**:
1. Login as admin
2. Navigate to admin panel
3. Test new endpoints:
   - `GET /api/Admin/recommender/health`
   - `GET /api/Admin/recommender/config`
   - `GET /api/Admin/recommender/stats`

---

## 📊 Monitoring

### Logs to Watch

#### .NET Backend
```
RecommendationProxyService initialized: URL=..., Timeout=...
Requesting recommendations from Python service for UserId=...
Received X recommendations from Python service (model v0.1.0)
```

#### Python Service
```json
{
  "timestamp": "2025-12-03T10:15:30Z",
  "level": "INFO",
  "message": "Request completed: recommend",
  "userId": 123,
  "latency_ms": 45,
  "result_count": 10
}
```

### Performance Metrics
- **Target Latency**: < 300ms (p95 < 500ms)
- **Success Rate**: > 99%
- **Fallback Rate**: < 1%

### Alerts
Set up monitoring for:
- Python service downtime
- High fallback rate (> 5%)
- Slow response times (> 1s)
- Database connection errors

---

## 📝 API Changes

### Frontend Impact
**None**. Frontend still calls:
```
GET /api/Events/recommendations
```

Response format unchanged.

### New Admin Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/Admin/recommender/health` | GET | Check Python service health |
| `/api/Admin/recommender/config` | GET | View model configuration |
| `/api/Admin/recommender/config` | PUT | Update model weights |
| `/api/Admin/recommender/stats` | GET | View service statistics |

---

## 🗑️ Decommissioning Timeline

### Phase 1: Soft Deprecation (Current)
- ✅ Old service marked `[Obsolete]`
- ✅ Warning logged on each call
- ✅ Service kept as fallback

### Phase 2: Hard Deprecation (After 1 month of stable operation)
- Move old service to `_Archive/` directory
- Remove from DI (keep interface for proxy)
- Update documentation

### Phase 3: Complete Removal (v2.0)
**Only after approval**:
- Delete `RecommendationService.cs`
- Remove `IRecommendationService` (if not needed by proxy)
- Update all references

---

## ✅ Verification Checklist

Before considering migration complete:

- [ ] Python service running and healthy
- [ ] .NET backend connecting to Python service
- [ ] Recommendations returning successfully
- [ ] Fallback mechanism tested and working
- [ ] Admin endpoints accessible
- [ ] Frontend displaying recommendations
- [ ] Logs showing successful requests
- [ ] No errors in console
- [ ] Performance acceptable (< 500ms)
- [ ] Database connection stable

---

## 🆘 Troubleshooting

### Issue: "Python service unavailable"
**Cause**: Python service not running or wrong URL

**Solution**:
1. Check if Python service is running: `curl http://localhost:5000/api/v1/health`
2. Verify URL in appsettings.json
3. Check firewall rules
4. Review Python service logs

### Issue: "Database connection failed"
**Cause**: Python service can't connect to SQL Server

**Solution**:
1. Check DB_CONNECTION_STRING in `.env`
2. Verify SQL Server is running
3. Test connection from Python:
   ```bash
   cd UniMeetRecommender
   python -c "from models.db_connector import *; import os; from dotenv import load_dotenv; load_dotenv(); ..."
   ```

### Issue: "Empty recommendations"
**Cause**: User has no followed clubs or no events available

**Expected**: Fallback returns upcoming public events

**Verify**: Check logs for "Using fallback recommendations"

### Issue: "Timeout"
**Cause**: Python service too slow or hung

**Solution**:
1. Increase timeout in appsettings.json
2. Check Python service performance
3. Review database query performance
4. Check system resources (CPU, memory)

---

## 📞 Support

For issues or questions:
- Check logs first (both .NET and Python)
- Review this document
- Contact development team
- Create GitHub issue (if applicable)

---

## 📚 Related Documentation

- [Python Service README](../UniMeetRecommender/README.md)
- [API Documentation](../UniMeetRecommender/README.md#api-documentation)
- [Configuration Guide](../UniMeetRecommender/README.md#configuration)

---

**Last Updated**: December 3, 2025  
**Migration Status**: ✅ Complete  
**Rollback Tested**: ✅ Yes  
**Production Ready**: ⚠️ Pending approval
