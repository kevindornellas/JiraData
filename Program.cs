
using JiraData;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();

var app = builder.Build();

app.MapPost("/api/jira/sync", async (string? startDate, string? endDate) =>
{
    try
    {
        var configuration = app.Configuration;
        
        var jiraBaseUrl = Environment.GetEnvironmentVariable("Jira__BaseUrl") ?? configuration["Jira:BaseUrl"];
        var username = Environment.GetEnvironmentVariable("Jira__Username") ?? configuration["Jira:Username"];
        var apiToken = Environment.GetEnvironmentVariable("Jira__ApiToken") ?? configuration["Jira:ApiToken"];
        var sqlConnectionString = Environment.GetEnvironmentVariable("Sql__ConnectionString") ?? configuration["Sql:ConnectionString"];
        
        // Use provided dates or fall back to config
        if (string.IsNullOrEmpty(startDate))
            startDate = Environment.GetEnvironmentVariable("Jira__StartDate") ?? configuration["Jira:StartDate"];
        if (string.IsNullOrEmpty(endDate))
            endDate = Environment.GetEnvironmentVariable("Jira__EndDate") ?? configuration["Jira:EndDate"];
        
        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
            return Results.BadRequest("startDate and endDate parameters are required");
        
        DateTime startDateParsed = DateTime.Parse(startDate);
        DateTime endDateParsed = DateTime.Parse(endDate);
        
        var jiraApiClient = new JiraApiClient(jiraBaseUrl, username, apiToken);
        TimeSpan difference = endDateParsed - startDateParsed;
        var days = difference.TotalDays;
        int issuesProcessed = 0;
        
        for (int i = 0; i < days; i++)
        {
            string start = startDateParsed.AddDays(i).ToString("yyyy-MM-dd");
            string end = startDateParsed.AddDays(i + 1).ToString("yyyy-MM-dd");
            string jqlQuery = "project In (\"Elastic Admin\", \"Elastic Product Development\") and statuscategory = Complete and statuscategorychangeddate > " + start + "  and statuscategorychangeddate < " + end + "  and (resolution Is EMPTY or resolution in (Done, Declined)) and type in (Story, Task, Bug) ORDER BY key ASC, created DESC";
            
            List<JiraIssue> issues = await jiraApiClient.GetIssuesFromJiraAsync(jqlQuery);
            SQLInserter sqlInserter = new SQLInserter(sqlConnectionString);
            
            foreach (var issue in issues)
            {
                sqlInserter.InsertIssuesToSQL(issue);
                issuesProcessed++;
            }
        }
        
        return Results.Ok(new { message = $"Successfully synced Jira data from {startDate} to {endDate}", issuesProcessed });
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
});

app.MapGet("/api/jira/latest-date", (string? connectionString) =>
{
    try
    {
        var configuration = app.Configuration;
        var sqlConnectionString = connectionString ?? (Environment.GetEnvironmentVariable("Sql__ConnectionString") ?? configuration["Sql:ConnectionString"]);
        
        if (string.IsNullOrEmpty(sqlConnectionString))
            return Results.BadRequest("SQL connection string is required");
        
        var latestDate = GetLatestInsertionDate(sqlConnectionString);
        
        if (latestDate == null)
            return Results.Ok(new { latestDate = (string?)null, message = "No data found in database" });
        
        return Results.Ok(new { latestDate = latestDate.Value.ToString("yyyy-MM-dd") });
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
});

app.MapGet("/api/jira/status", () =>
{
    return Results.Ok(new 
    { 
        status = "ready", 
        message = "Jira Data Sync API is ready. Use POST /api/jira/sync?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD to sync data." 
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

static DateTime? GetLatestInsertionDate(string connectionString)
{
    using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
    connection.Open();
    var query = "SELECT MAX(CAST(CompletedDate AS DATE)) FROM JiraIssue WHERE CompletedDate IS NOT NULL";
    using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
    
    var result = command.ExecuteScalar();
    if (result == null || result is DBNull)
        return null;
    
    return (DateTime)result;
}

app.Run();



