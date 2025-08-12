using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Collections.Generic;
using JiraData;

public class JiraApiClient
{
    private readonly string _jiraBaseUrl;
    private readonly string _username;
    private readonly string _apiToken;
    private readonly string _sqlConnectionString;

    private static readonly HttpClient client = new HttpClient();

    public JiraApiClient(string jiraBaseUrl, string username, string apiToken)
    {
        _jiraBaseUrl = jiraBaseUrl;
        _username = username;
        _apiToken = apiToken;
        // Authenticate with the Jira API using Basic Authentication
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization",
            $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_username}:{_apiToken}"))}");

    }

    // Method to get issues from Jira API
    public async Task<List<JiraIssue>> GetIssuesFromJiraAsync(string jqlQuery)
    {
        string url = $"{_jiraBaseUrl}/rest/api/2/search?jql={Uri.EscapeDataString(jqlQuery)}";

        try
        {
            // Send GET request to Jira API
            var response = await client.GetStringAsync(url);
            dynamic jsonResponse = JsonConvert.DeserializeObject(response);

            List<JiraIssue> issues = new List<JiraIssue>();

            foreach (var issue in jsonResponse.issues)
            {
                string key = issue.key;
                Console.WriteLine(key);
                var developerField = issue.fields.customfield_10084;
                string developer = CleanField(developerField);
                var assigneeField = issue.fields.assignee;
                var comments = await GetCommentsForIssueAsync(key);
                List<JiraHistory> history = await GetHistoryForIssueAsync(key);
                JiraIssue jiraIssue = new JiraIssue
                {
                    StoryKey = issue.key,
                    Description = issue.fields.issuetype.name,
                    Status = issue.fields.status.name,
                    Assignee = CleanField(assigneeField),
                    Created = issue.fields.created,
                    Updated = issue.fields.updated,
                    CompletedDate = issue.fields.resolutiondate,
                    StoryPoints = issue.fields.customfield_10024,
                    QAPoints = issue.fields.customfield_10198,
                    Sprint = GetCount(issue.fields.customfield_10020?.ToString()),
                    Parent = issue.fields.customfield_10014,
                    Developer = developer,
                    Comments = getDeveloperComments(developer, comments),
                    History = history,
                    Tester = CleanField(issue.fields.customfield_10037)

                };

                issues.Add(jiraIssue);
            }

            return issues;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving data from Jira: {ex.Message}");
            return new List<JiraIssue>();
        }
    }

    private List<JiraComment> getDeveloperComments(string dev, List<JiraComment> comments)
    {
        List<JiraComment> devComments = new List<JiraComment>();

        foreach (JiraComment comment in comments)
        {
            if (comment.Author == dev)
            {
                devComments.Add(comment);
            }
        }


        return devComments;
    }

    public int GetCount(string sprintInput)
    {
        var input = sprintInput;
        return sprintInput.Split("Elastic Sprint").Length - 1;
    }

    public string? CleanField(dynamic devField)
    {
        if (devField == null)
        {
            return "";
        } else
        {
            return devField.displayName;
        }
    }

    private async Task<List<JiraComment>> GetCommentsForIssueAsync(string issueKey)
    {
        string url = $"{_jiraBaseUrl}/rest/api/2/issue/{issueKey}/comment";

        try
        {
            // Send GET request to Jira API to retrieve comments
            var response = await client.GetStringAsync(url);
            dynamic jsonResponse = JsonConvert.DeserializeObject(response);

            List<JiraComment> comments = new List<JiraComment>();

            foreach (var comment in jsonResponse.comments)
            {
                JiraComment jiraComment = new JiraComment
                {
                    StoryKey = issueKey,
                    Author = comment.author.displayName,
                    Body = comment.body,
                    Created = comment.created,
                    Updated = comment.updated
                };

                comments.Add(jiraComment);
            }

            return comments;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving comments for issue {issueKey}: {ex.Message}");
            return new List<JiraComment>();
        }
    }

    private async Task<List<JiraHistory>> GetHistoryForIssueAsync(string issueKey)
    {
        string url = $"{_jiraBaseUrl}/rest/api/2/issue/{issueKey}/changelog";

        try
        {


            var response = await client.GetStringAsync(url);
            dynamic jsonResponse = JsonConvert.DeserializeObject(response);
            Console.WriteLine(jsonResponse.values.Count + " histories found for issue " + issueKey);
            dynamic history = new List<JiraHistory>();
            if (jsonResponse == null || jsonResponse.values == null)
            {
                Console.WriteLine($"No history found for issue {issueKey}");
                return new List<JiraHistory>();
            }

            foreach (var historyItem in jsonResponse.values)
            {
                JiraHistory jiraHistory = new JiraHistory
                {
                    StoryKey = issueKey,
                    Author = historyItem.author.displayName,
                    Created = historyItem.created,
                    Items = new List<JiraHistoryItem>()
                };

                // Collect the field changes in the changelog
                foreach (var item in historyItem.items)
                {
                    //if (item.field != "description")
                    //{
                        JiraHistoryItem historyItemObj = new JiraHistoryItem
                        {
                            Field = item.field,
                            From = item.fromString,
                            To = item.toString
                        };

                        jiraHistory.Items.Add(historyItemObj);
                    //}
                    //else
                    //{
                    //    JiraHistoryItem historyItemObj = new JiraHistoryItem
                    //    {
                    //        Field = item.field,
                    //        From = "",
                    //        To = ""
                    //    };

                    //    jiraHistory.Items.Add(historyItemObj);
                    //}
                }

                history.Add(jiraHistory);
            }

            return history;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving history for issue {issueKey}: {ex.Message}");
            return new List<JiraHistory>();
        }
    }

    public class JiraChangeLog
    {
        [JsonProperty("values")]
        public History[] Histories { get; set; }
    }

    public class History
    {
        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("items")]
        public Item[] Items { get; set; }
    }

    public class Item 
    {
        [JsonProperty("Field")]
        public string Field { get; set; }
        [JsonProperty("fromString")]
        public string FromString { get; set; }

        [JsonProperty("toStirng")]
        public string ToStirng { get; set; }
    }



}



