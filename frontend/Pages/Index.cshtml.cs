using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JiraDataFrontend.Pages;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexModel> _logger;

    public DateTime? LatestDate { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SyncResult { get; set; }
    public bool ApiHealthy { get; set; } = false;
    public bool IsSyncing { get; set; } = false;
    public string DefaultStartDate { get; set; } = "";
    public string DefaultEndDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

    public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<IndexModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        await LoadLatestDate();
        await CheckApiHealth();
        SetDefaultDates();
    }

    public async Task OnPostAsync(string startDate, string endDate)
    {
        await LoadLatestDate();
        SetDefaultDates();

        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
        {
            ErrorMessage = "Start date and end date are required.";
            return;
        }

        try
        {
            IsSyncing = true;
            var apiBaseUrl = _configuration["JiraDataApi:BaseUrl"] ?? "http://jiradata-api-service:5000";
            var client = _httpClientFactory.CreateClient();

            var syncUrl = $"{apiBaseUrl}/api/jira/sync?startDate={startDate}&endDate={endDate}";
            var response = await client.PostAsync(syncUrl, null);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                SyncResult = content;
                StatusMessage = $"✓ Sync completed successfully for {startDate} to {endDate}. Issues processed.";
                await LoadLatestDate(); // Refresh the latest date
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Sync failed with status {response.StatusCode}: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync");
            ErrorMessage = $"An error occurred during sync: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task LoadLatestDate()
    {
        try
        {
            var apiBaseUrl = _configuration["JiraDataApi:BaseUrl"] ?? "http://jiradata-api-service:5000";
            var client = _httpClientFactory.CreateClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var response = await client.GetAsync($"{apiBaseUrl}/api/jira/latest-date", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = System.Text.Json.JsonDocument.Parse(content);
                var root = json.RootElement;

                if (root.TryGetProperty("latestDate", out var latestDateElement) && 
                    !string.IsNullOrEmpty(latestDateElement.GetString()))
                {
                    if (DateTime.TryParse(latestDateElement.GetString(), out var date))
                    {
                        LatestDate = date;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve latest date from API");
        }
    }

    private async Task CheckApiHealth()
    {
        try
        {
            var apiBaseUrl = _configuration["JiraDataApi:BaseUrl"] ?? "http://jiradata-api-service:5000";
            var client = _httpClientFactory.CreateClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var response = await client.GetAsync($"{apiBaseUrl}/health", cts.Token);
            ApiHealthy = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API health check failed");
            ApiHealthy = false;
        }
    }

    private void SetDefaultDates()
    {
        if (LatestDate.HasValue)
        {
            DefaultStartDate = LatestDate.Value.AddDays(1).ToString("yyyy-MM-dd");
        }
        else
        {
            DefaultStartDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
        }
    }
}
