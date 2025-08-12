using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraData
{
    public class JiraHistory
    {
        public string StoryKey { get; set; }
        public string Author { get; set; }
        public string Created { get; set; }
        public List<JiraHistoryItem> Items { get; set; } = new List<JiraHistoryItem>();
    }

    public class JiraHistoryItem
    {
        public string Field { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}
