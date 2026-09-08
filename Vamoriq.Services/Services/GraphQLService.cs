using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vamoriq.Services.Interfaces;

namespace Vamoriq.Services.Services;

public class GraphQLService : IGraphQLService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphQLService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAuthService? _authService;
    private readonly string _baseUrl;

    public GraphQLService(HttpClient httpClient, IConfiguration configuration, ILogger<GraphQLService> logger, IAuthService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _authService = authService;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://api.example.com";

        if (int.TryParse(configuration["ApiSettings:TimeoutSeconds"], out var timeoutSeconds) && timeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }
        else
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }
    }

    public async Task<TResponse?> ExecuteQueryAsync<TResponse>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                query,
                variables
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/graphql")
            {
                Content = content
            };

            var tenant = _configuration["Tenant"] ?? "vamoriq";
            requestMessage.Headers.Add("Tenant", tenant);
            requestMessage.Headers.Add("Connection", "close");
            requestMessage.Headers.Add("Accept-Encoding", "identity");

            if (_authService != null)
            {
                try
                {
                    if (await _authService.IsAuthenticatedAsync(cancellationToken))
                    {
                        var token = await _authService.GetStoredTokenAsync();
                        if (!string.IsNullOrEmpty(token))
                        {
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add authorization header, continuing without auth");
                }
            }

            _logger.LogDebug("Sending GraphQL request to {Url} with tenant: {Tenant}", $"{_baseUrl}/graphql", tenant);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            }
            catch (Exception ex) when (ex.Message?.Contains("unexpected end of stream") == true)
            {

                _logger.LogWarning("GraphQL request failed with AndroidMessageHandler bug (401 response). User not authenticated.");
                return default;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GraphQL request failed with status: {StatusCode}. Response: {Response}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new GraphQLRateLimitException("Rate limit exceeded: Too many requests returned by GraphQL API.");
                }

                return default;
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("GraphQL response received: {ResponseLength} bytes", responseString.Length);

            return JsonSerializer.Deserialize<TResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed in GraphQL: {Message}. Inner: {InnerMessage}",
                ex.Message, ex.InnerException?.Message);
            return default;
        }
        catch (GraphQLRateLimitException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL request failed: {Message}", ex.Message);
            return default;
        }
    }
}

public class GraphQLRateLimitException : InvalidOperationException
{
    public GraphQLRateLimitException(string message) : base(message)
    {
    }
}
