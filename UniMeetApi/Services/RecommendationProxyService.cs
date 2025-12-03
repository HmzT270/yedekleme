using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UniMeetApi.Services
{
    /// <summary>
    /// Response from Python recommendation service
    /// </summary>
    public class PythonRecommendationResponse
    {
        public List<PythonRecommendation> Recommendations { get; set; } = new();
        public PythonMetadata Metadata { get; set; } = new();
    }

    public class PythonRecommendation
    {
        public int EventId { get; set; }
        public double Score { get; set; }
        public PythonReason Reason { get; set; } = new();
    }

    public class PythonReason
    {
        public string Primary { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public Dictionary<string, double> Features { get; set; } = new();
    }

    public class PythonMetadata
    {
        public string Model_Version { get; set; } = string.Empty;
        public string Computed_At { get; set; } = string.Empty;
        public int Total_Candidates { get; set; }
        public double Computation_Time_Ms { get; set; }
        public bool Fallback { get; set; }
    }

    /// <summary>
    /// Interface for recommendation proxy service
    /// </summary>
    public interface IRecommendationProxyService
    {
        Task<List<Event>> GetRecommendedEventsAsync(int userId, int limit = 10);
        Task<PythonRecommendationResponse> GetDetailedRecommendationsAsync(int userId, int limit = 10);
        Task<bool> CheckHealthAsync();
        Task<object?> GetConfigAsync();
        Task UpdateConfigAsync(object newConfig);
        Task<object?> GetStatsAsync();
    }

    /// <summary>
    /// Proxy service that forwards recommendation requests to Python microservice
    /// </summary>
    public class RecommendationProxyService : IRecommendationProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RecommendationProxyService> _logger;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly string _pythonServiceUrl;
        private readonly int _timeoutSeconds;
        private readonly bool _enableFallback;
        private readonly IRecommendationService _fallbackService;

        public RecommendationProxyService(
            HttpClient httpClient,
            ILogger<RecommendationProxyService> logger,
            AppDbContext context,
            IConfiguration config,
            IRecommendationService fallbackService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
            _config = config;
            _fallbackService = fallbackService;

            // Load configuration
            _pythonServiceUrl = _config["RecommendationService:PythonServiceUrl"] ?? "http://localhost:5000";
            _timeoutSeconds = int.Parse(_config["RecommendationService:TimeoutSeconds"] ?? "5");
            _enableFallback = bool.Parse(_config["RecommendationService:EnableFallback"] ?? "true");

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(_pythonServiceUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

            _logger.LogInformation(
                "RecommendationProxyService initialized: URL={PythonServiceUrl}, Timeout={TimeoutSeconds}s, Fallback={EnableFallback}",
                _pythonServiceUrl, _timeoutSeconds, _enableFallback);
        }

        public async Task<List<Event>> GetRecommendedEventsAsync(int userId, int limit = 10)
        {
            try
            {
                _logger.LogInformation("Requesting recommendations from Python service for UserId={UserId}, Limit={Limit}", 
                    userId, limit);

                // Prepare request payload
                var requestPayload = new
                {
                    userId = userId,
                    limit = limit,
                    context = new
                    {
                        excludeEventIds = new int[] { },
                        filters = new
                        {
                            minDate = DateTime.UtcNow.ToString("o"),
                            maxDate = (string?)null
                        }
                    }
                };

                // Call Python service
                var response = await _httpClient.PostAsJsonAsync("/api/v1/recommend", requestPayload);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Python service returned error status: {StatusCode}, falling back to old service",
                        response.StatusCode);
                    return await GetFallbackRecommendationsAsync(userId, limit);
                }

                // Parse response
                var pythonResponse = await response.Content.ReadFromJsonAsync<PythonRecommendationResponse>();

                if (pythonResponse == null || pythonResponse.Recommendations == null)
                {
                    _logger.LogWarning("Python service returned null response, using fallback");
                    return await GetFallbackRecommendationsAsync(userId, limit);
                }

                _logger.LogInformation(
                    "Received {Count} recommendations from Python service (model v{Version})",
                    pythonResponse.Recommendations.Count,
                    pythonResponse.Metadata.Model_Version);

                // Convert Python event IDs to Event objects
                var eventIds = pythonResponse.Recommendations.Select(r => r.EventId).ToList();
                
                if (!eventIds.Any())
                {
                    _logger.LogInformation("No recommendations returned, using fallback");
                    return await GetFallbackRecommendationsAsync(userId, limit);
                }

                var events = await _context.Events
                    .Where(e => eventIds.Contains(e.EventId))
                    .ToListAsync();

                // Sort by Python's recommendation order (score)
                var eventDict = events.ToDictionary(e => e.EventId);
                var orderedEvents = new List<Event>();
                
                foreach (var rec in pythonResponse.Recommendations)
                {
                    if (eventDict.TryGetValue(rec.EventId, out var evt))
                    {
                        orderedEvents.Add(evt);
                    }
                }

                _logger.LogInformation(
                    "Successfully retrieved {Count} recommended events for UserId={UserId}",
                    orderedEvents.Count, userId);

                return orderedEvents;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "HTTP error calling Python service, falling back to old service: {Message}",
                    ex.Message);
                return await GetFallbackRecommendationsAsync(userId, limit);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex,
                    "Timeout calling Python service ({TimeoutSeconds}s), falling back to old service",
                    _timeoutSeconds);
                return await GetFallbackRecommendationsAsync(userId, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error calling Python service, falling back to old service: {Message}",
                    ex.Message);
                return await GetFallbackRecommendationsAsync(userId, limit);
            }
        }

        public async Task<PythonRecommendationResponse> GetDetailedRecommendationsAsync(int userId, int limit = 10)
        {
            try
            {
                _logger.LogInformation("Requesting detailed recommendations from Python service for UserId={UserId}, Limit={Limit}", 
                    userId, limit);

                // Prepare request payload
                var requestPayload = new
                {
                    userId = userId,
                    limit = limit,
                    context = new
                    {
                        excludeEventIds = new int[] { },
                        filters = new
                        {
                            minDate = DateTime.UtcNow.ToString("o"),
                            maxDate = (string?)null
                        }
                    }
                };

                // Call Python service
                var response = await _httpClient.PostAsJsonAsync("/api/v1/recommend", requestPayload);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Python service returned error status: {StatusCode}, returning empty response",
                        response.StatusCode);
                    return new PythonRecommendationResponse
                    {
                        Recommendations = new List<PythonRecommendation>(),
                        Metadata = new PythonMetadata { Fallback = true }
                    };
                }

                // Parse response
                var pythonResponse = await response.Content.ReadFromJsonAsync<PythonRecommendationResponse>();

                if (pythonResponse == null || pythonResponse.Recommendations == null)
                {
                    _logger.LogWarning("Python service returned null response");
                    return new PythonRecommendationResponse
                    {
                        Recommendations = new List<PythonRecommendation>(),
                        Metadata = new PythonMetadata { Fallback = true }
                    };
                }

                _logger.LogInformation(
                    "Received {Count} detailed recommendations from Python service (model v{Version})",
                    pythonResponse.Recommendations.Count,
                    pythonResponse.Metadata.Model_Version);

                return pythonResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "HTTP error calling Python service for detailed recommendations: {Message}",
                    ex.Message);
                return new PythonRecommendationResponse
                {
                    Recommendations = new List<PythonRecommendation>(),
                    Metadata = new PythonMetadata { Fallback = true }
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex,
                    "Timeout calling Python service for detailed recommendations ({TimeoutSeconds}s)",
                    _timeoutSeconds);
                return new PythonRecommendationResponse
                {
                    Recommendations = new List<PythonRecommendation>(),
                    Metadata = new PythonMetadata { Fallback = true }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error calling Python service for detailed recommendations: {Message}",
                    ex.Message);
                return new PythonRecommendationResponse
                {
                    Recommendations = new List<PythonRecommendation>(),
                    Metadata = new PythonMetadata { Fallback = true }
                };
            }
        }

        private async Task<List<Event>> GetFallbackRecommendationsAsync(int userId, int limit)
        {
            if (!_enableFallback)
            {
                _logger.LogWarning("Fallback is disabled, returning empty list");
                return new List<Event>();
            }

            _logger.LogInformation("Using fallback recommendation service for UserId={UserId}", userId);

            try
            {
                // Use old recommendation service as fallback
                var fallbackEvents = await _fallbackService.GetRecommendedEventsAsync(userId);
                return fallbackEvents.Take(limit).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback service also failed: {Message}", ex.Message);
                
                // Last resort: return upcoming public events
                return await _context.Events
                    .Where(e => !e.IsCancelled && e.IsPublic && e.StartAt >= DateTime.UtcNow)
                    .OrderBy(e => e.StartAt)
                    .Take(limit)
                    .ToListAsync();
            }
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/v1/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed for Python service");
                return false;
            }
        }

        public async Task<object?> GetConfigAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/v1/config");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting config from Python service");
                return null;
            }
        }

        public async Task UpdateConfigAsync(object newConfig)
        {
            try
            {
                var apiKey = _config["RecommendationService:ApiKey"];
                
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/config")
                {
                    Content = JsonContent.Create(newConfig)
                };
                
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Add("X-API-Key", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Updated Python service configuration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating config in Python service");
                throw;
            }
        }

        public async Task<object?> GetStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/v1/stats");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats from Python service");
                return null;
            }
        }
    }
}
