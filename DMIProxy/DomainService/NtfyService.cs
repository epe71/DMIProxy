namespace DMIProxy.DomainService;

public class NtfyService : INtfyService
{
    private string url = "http://192.168.1.40:1580/DMIProxy";
    private readonly ILogger<EdrService> _logger;
    private readonly HttpClient _httpClient;

    public NtfyService(IHttpClientFactory httpClientFactory, ILogger<EdrService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LongTimeOutClient");
        _logger = logger;
    }

    public async Task<bool> SendNotification(string message)
    {
        try
        {
            var content = new StringContent(message, System.Text.Encoding.UTF8);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send notification: {Message}", ex.Message);
            return false;
        }
    }
}