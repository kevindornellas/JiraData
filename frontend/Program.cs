var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapGet("/healthz", () => Results.Ok("healthy"));

app.MapGet("/api/sync/stream", async (string? startDate, string? endDate, HttpResponse httpResponse, IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    var apiBaseUrl = config["JiraDataApi:BaseUrl"] ?? "http://jiradata-api-service:5000";
    httpResponse.Headers["Content-Type"] = "text/event-stream";
    httpResponse.Headers["Cache-Control"] = "no-cache";
    httpResponse.Headers["X-Accel-Buffering"] = "no";

    var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromHours(2);

    using var backendResponse = await client.GetAsync(
        $"{apiBaseUrl}/api/jira/sync/stream?startDate={Uri.EscapeDataString(startDate ?? "")}&endDate={Uri.EscapeDataString(endDate ?? "")}",
        HttpCompletionOption.ResponseHeadersRead);

    using var stream = await backendResponse.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream && !httpResponse.HttpContext.RequestAborted.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync();
        if (line != null)
        {
            await httpResponse.WriteAsync(line + "\n");
            await httpResponse.Body.FlushAsync();
        }
    }
});

app.MapGet("/api/cleanup/stream", async (HttpResponse httpResponse, IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    var apiBaseUrl = config["JiraDataApi:BaseUrl"] ?? "http://jiradata-api-service:5000";
    httpResponse.Headers["Content-Type"] = "text/event-stream";
    httpResponse.Headers["Cache-Control"] = "no-cache";
    httpResponse.Headers["X-Accel-Buffering"] = "no";

    var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromHours(2);

    using var backendResponse = await client.GetAsync(
        $"{apiBaseUrl}/api/jira/cleanup/stream",
        HttpCompletionOption.ResponseHeadersRead);

    using var stream = await backendResponse.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream && !httpResponse.HttpContext.RequestAborted.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync();
        if (line != null)
        {
            await httpResponse.WriteAsync(line + "\n");
            await httpResponse.Body.FlushAsync();
        }
    }
});

app.MapRazorPages();

app.Run();
