using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MediaTrust.Report.Accessors;

public sealed class GeminiAiAccessor : IAiAccessor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAiAccessor> _logger;

    public GeminiAiAccessor(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiAiAccessor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateExplanationAsync(
        string detectorName,
        double score,
        string details,
        CancellationToken ct)
    {
        try
        {
            var apiKey = _configuration["Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured.");
            }

            var client =
                _httpClientFactory.CreateClient("gemini");

            // --------------------------------------------------
            // Build AI Prompt
            // --------------------------------------------------
            var prompt = $"""
                You are explaining the result of a media integrity detector.

                Detector: {detectorName}
                Risk Score: {score:P0}
                Technical Details: {details}

                Explain the result clearly for a non-technical user.

                Important:
                - The risk score is a heuristic score, not a probability that the file is fake.
                - Do not claim that the media is manipulated unless the detector proves it.
                - Explain which technical indicators affected the score.
                - Keep the explanation concise, around 2-3 sentences.
                """;

            // --------------------------------------------------
            // Gemini Request With Retry
            // --------------------------------------------------
            HttpResponseMessage? response = null;

            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        "v1beta/models/gemini-3.1-flash-lite:generateContent");

                request.Headers.Add(
                    "x-goog-api-key",
                    apiKey);

                request.Content = JsonContent.Create(new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                });

                // --------------------------------------------------
                // Per-Attempt Timeout
                // --------------------------------------------------
                using var timeoutCts =
                    CancellationTokenSource.CreateLinkedTokenSource(ct);

                timeoutCts.CancelAfter(
                    TimeSpan.FromSeconds(15));

                try
                {
                    response = await client.SendAsync(
                        request,
                        timeoutCts.Token);
                }
                catch (OperationCanceledException)
                    when (!ct.IsCancellationRequested)
                {
                    if (attempt == maxAttempts)
                    {
                        throw new TimeoutException(
                            $"Gemini did not respond after {maxAttempts} attempts.");
                    }

                    _logger.LogWarning(
                        "Gemini request timed out. Retry attempt {Attempt}/{MaxAttempts}",
                        attempt,
                        maxAttempts);

                    var timeoutDelaySeconds = attempt == 1 ? 1 : 2;

                    await Task.Delay(
                        TimeSpan.FromSeconds(timeoutDelaySeconds),
                        ct);

                    continue;
                }

                // Success - stop retrying
                if (response.IsSuccessStatusCode)
                {
                    break;
                }

                var shouldRetry =
                    response.StatusCode ==
                    HttpStatusCode.ServiceUnavailable;

                // Do not retry other errors,
                // or if this was the final attempt
                if (!shouldRetry || attempt == maxAttempts)
                {
                    var errorBody =
                        await response.Content.ReadAsStringAsync(ct);

                    throw new HttpRequestException(
                        $"Gemini API returned {(int)response.StatusCode} " +
                        $"({response.StatusCode}). Response: {errorBody}",
                        null,
                        response.StatusCode);
                }

                _logger.LogWarning(
                    "Gemini returned {StatusCode}. Retry attempt {Attempt}/{MaxAttempts}",
                    response.StatusCode,
                    attempt,
                    maxAttempts);

                response.Dispose();
                response = null;

                // 1 second, then 2 seconds
                var retryDelaySeconds = attempt == 1 ? 1 : 2;

                await Task.Delay(
                    TimeSpan.FromSeconds(retryDelaySeconds),
                    ct);
            }

            if (response == null)
            {
                throw new InvalidOperationException(
                    "Gemini did not return a response.");
            }

            // --------------------------------------------------
            // Read Gemini Response
            // --------------------------------------------------
            var jsonText =
                await response.Content.ReadAsStringAsync(ct);

            using var json =
                JsonDocument.Parse(jsonText);

            var text =
                json.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            response.Dispose();

            return text ??
                   "No AI explanation generated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate Gemini AI explanation");

            throw;
        }
    }
}