using System.Net;

namespace XYVR.Core;

public class RetryHttpClientHelper
{
    private const long WaitThisMuchOnTooManyRequestsSeconds = 80;
    
    private readonly HttpClient _httpClient;

    public RetryHttpClientHelper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        return await DoSendAsync(request, CancellationToken.None);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await DoSendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> DoSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                var result = await _httpClient.SendAsync(request, cancellationToken);
                if (!result.IsSuccessStatusCode)
                {
                    if ((int)result.StatusCode / 100 == 5)
                    {
                        XYVRLogging.WriteLine(this, $"Got status code {result.StatusCode}");
                        retryCount++;
                        await Delay(cancellationToken, NextRetryDelay(retryCount), retryCount);
                    }
                    else if (result.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        XYVRLogging.WriteLine(this, $"Got status code {result.StatusCode} (Too Many Requests)");
                        retryCount++;
                        // Always wait at least this specified number of seconds.
                        await Delay(cancellationToken, TimeSpan.FromSeconds(WaitThisMuchOnTooManyRequestsSeconds), retryCount);
                    }
                }
                
                return result;
            }
            catch (HttpRequestException e)
            {
                XYVRLogging.ErrorWriteLine(this, e);
                
                retryCount++;
                await Delay(cancellationToken, NextRetryDelay(retryCount), retryCount);
            }
        }
    }

    private static Task Delay(CancellationToken cancellationToken, TimeSpan nextRetryDelay, int retryCount)
    {
        XYVRLogging.WriteLine(typeof(RetryHttpClientHelper), $"Will retry after {nextRetryDelay.TotalSeconds} seconds (retry #{retryCount})");
        return Task.Delay(nextRetryDelay, cancellationToken);
    }

    public static TimeSpan NextRetryDelay(int previousRetryCount)
    {
        return previousRetryCount switch
        {
            0 => TimeSpan.Zero,
            1 => TimeSpan.FromSeconds(2),
            2 => TimeSpan.FromSeconds(10),
            3 => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromSeconds(new Random().Next(60, 80))
        };
    }
}