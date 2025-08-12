
using JiraData;
using Microsoft.Extensions.Configuration;
using System;

var configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory()) // Ensure the config file is in the right directory
           .AddJsonFile("appsettings.json")
           .Build();

var jiraBaseUrl = configuration["Jira:BaseUrl"];
var jiraStartDate = Environment.GetEnvironmentVariable("JIRA_STARTDATE") ?? configuration["Jira:StartDate"];
var jiraEndDate = Environment.GetEnvironmentVariable("JIRA_ENDDATE") ?? configuration["Jira:EndDate"];
var username = configuration["Jira:Username"];
var apiToken = configuration["Jira:ApiToken"];
var sqlConnectionString = configuration["Sql:ConnectionString"];
DateTime startDate = DateTime.Parse(jiraStartDate);
DateTime endDate = DateTime.Parse(jiraEndDate);
var jiraApiClient = new JiraApiClient(jiraBaseUrl, username, apiToken);
TimeSpan difference = endDate - startDate;
var days = difference.TotalDays;
for (int i = 0; i < days; i++)
{
    
    string start = startDate.AddDays(i).ToString("yyyy-MM-dd");
    string end = startDate.AddDays(i + 1).ToString("yyyy-MM-dd");
    string jqlQuery = "project In (\"Elastic Product Development\") and statuscategory = Complete and statuscategorychangeddate > " + start +"  and statuscategorychangeddate < " + end + "  and (resolution Is EMPTY or resolution in (Done, Declined)) and type in (Story, Task, Bug) ORDER BY key ASC, created DESC";
    Console.WriteLine("Getting Data");
    // Get issues from Jira
    List<JiraIssue> issues = await jiraApiClient.GetIssuesFromJiraAsync(jqlQuery);
    Console.WriteLine("inserting Data");
    // Insert issues into SQL database
    SQLInserter sqlInserter = new SQLInserter(sqlConnectionString);
    foreach (var issue in issues)
    {
        Console.WriteLine(issue.StoryKey);
        sqlInserter.InsertIssuesToSQL(issue);
    }
    Console.WriteLine($"Issues have been successfully inserted into the database for: {start} - {end}");    
}



