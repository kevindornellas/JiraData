using JiraData;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public class SQLInserter
{
    private readonly string _connectionString;

    public SQLInserter(string connectionString)
    {
        _connectionString = connectionString;
    }

    

    public void InsertIssuesToSQL(JiraIssue issue)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var query = @"INSERT INTO JiraIssue (StoryKey, Description, Status, Assignee, Created, Updated, StoryPoints, QAPoints, Parent, Sprint, CompletedDate, Developer, Tester)
                     VALUES (@StoryKey, @Description, @Status, @Assignee, @Created, @Updated, @StoryPoints, @QAPoints, @Parent, @Sprint, @CompletedDate, @Developer, @Tester)";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@StoryKey", issue.StoryKey);
        command.Parameters.AddWithValue("@Description", issue.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Status", issue.Status ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Assignee", issue.Assignee ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Created", issue.Created ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Updated", issue.Updated ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StoryPoints", issue.StoryPoints ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@QAPoints", issue.QAPoints ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Parent", issue.Parent ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Sprint", issue.Sprint ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CompletedDate", issue.CompletedDate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Developer", issue.Developer ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Tester", issue.Tester ?? (object)DBNull.Value);
        command.ExecuteNonQuery();

        foreach (var comment in issue.Comments)
        {
            InsertJiraComment(comment);
        }

        if (issue.History != null)
        {
            foreach (var history in issue.History)
            {
                foreach (var item in history.Items)
                {
                    InsertJiraHistoryItem(history.StoryKey, history.Author, history.Created, item);
                }
            }
        }
    }
    private void InsertJiraComment(JiraComment comment)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var query = @"INSERT INTO JiraComment (StoryKey, Author, Body, Created, Updated) VALUES (@StoryKey, @Author, @Body, @Created, @Updated)";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@StoryKey", comment.StoryKey);
        command.Parameters.AddWithValue("@Author", comment.Author);
        command.Parameters.AddWithValue("@Body", comment.Body);
        command.Parameters.AddWithValue("@Created", comment.Created);
        command.Parameters.AddWithValue("@Updated", comment.Updated);
        command.ExecuteNonQuery();
    }



    private void InsertJiraHistory(JiraHistory history)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var query = @"INSERT INTO JiraHistory (StoryKey, Author, Created) VALUES (@StoryKey, @Author, @Created)";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@StoryKey", history.StoryKey);
        command.Parameters.AddWithValue("@Author", history.Author);
        command.Parameters.AddWithValue("@Created", history.Created);
        command.ExecuteNonQuery();

        foreach (var item in history.Items)
        {
            InsertJiraHistoryItem(history.StoryKey, history.Author, history.Created, item);
        }
    }

    private void InsertJiraHistoryItem(string storyKey, string author, string created, JiraHistoryItem item)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var query = @"INSERT INTO JiraHistoryItem (StoryKey, Author, Created, Field, FromValue, ToValue) 
                     VALUES (@StoryKey, @Author, @Created, @Field, @FromValue, @ToValue)";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@StoryKey", storyKey);
        command.Parameters.AddWithValue("@Author", author);
        command.Parameters.AddWithValue("@Created", created);
        command.Parameters.AddWithValue("@Field", item.Field);
        command.Parameters.AddWithValue("@FromValue", item.From ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ToValue", item.To ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }
}
