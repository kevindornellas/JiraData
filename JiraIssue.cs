using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraData
{
    public class JiraIssue
    {
        public string StoryKey { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Assignee { get; set; }
        public string? Created { get; set; }
        public string? Updated { get; set; }
        public int? StoryPoints { get; set; }
        public int? QAPoints { get; internal set; }
        public string? Parent { get; internal set; }
        public int? Sprint { get; internal set; }
        public string? CompletedDate { get; internal set; }
        public string? Developer { get; internal set; }
        public string? Tester { get; internal set; }
        public required List<JiraComment> Comments { get; set; }
        public List<JiraHistory>? History { get; internal set; }
    }
}
