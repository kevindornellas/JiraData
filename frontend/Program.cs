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
    httpResponse.Headers["Content-Type"] = "text/event-stream";
    httpResponse.Headers["Cache-Control"] = "no-cache";
    httpResponse.Headers["X-Accel-Buffering"] = "no";
    try
    {
        var apiBaseUrl = config["JiraDataApi:BaseUrl"] ?? "http://jiradata-api-service:5000";
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromHours(2);

        using var backendResponse = await client.GetAsync(
            $"{apiBaseUrl}/api/jira/sync/stream?startDate={Uri.EscapeDataString(startDate ?? "")}&endDate={Uri.EscapeDataString(endDate ?? "")}",
            HttpCompletionOption.ResponseHeadersRead);

        if (!backendResponse.IsSuccessStatusCode)
        {
            await httpResponse.WriteAsync($"data: ERROR: Backend returned {(int)backendResponse.StatusCode} - is the backend image up to date?\n\n");
            await httpResponse.WriteAsync("event: done\ndata: Failed.\n\n");
            await httpResponse.Body.FlushAsync();
            return;
        }

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
    }
    catch (Exception ex)
    {
        await httpResponse.WriteAsync($"data: ERROR: {ex.Message}\n\n");
        await httpResponse.Body.FlushAsync();
    }
});

app.MapGet("/api/cleanup/stream", async (HttpResponse httpResponse, IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    httpResponse.Headers["Content-Type"] = "text/event-stream";
    httpResponse.Headers["Cache-Control"] = "no-cache";
    httpResponse.Headers["X-Accel-Buffering"] = "no";
    try
    {
        var apiBaseUrl = config["JiraDataApi:BaseUrl"] ?? "http://jiradata-api-service:5000";
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromHours(2);

        using var backendResponse = await client.GetAsync(
            $"{apiBaseUrl}/api/jira/cleanup/stream",
            HttpCompletionOption.ResponseHeadersRead);

        if (!backendResponse.IsSuccessStatusCode)
        {
            await httpResponse.WriteAsync($"data: ERROR: Backend returned {(int)backendResponse.StatusCode} - is the backend image up to date?\n\n");
            await httpResponse.WriteAsync("event: done\ndata: Failed.\n\n");
            await httpResponse.Body.FlushAsync();
            return;
        }

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
    }
    catch (Exception ex)
    {
        await httpResponse.WriteAsync($"data: ERROR: {ex.Message}\n\n");
        await httpResponse.Body.FlushAsync();
    }
});

app.MapRazorPages();

app.Run();
