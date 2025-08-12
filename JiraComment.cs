using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraData
{
    
    public class JiraComment
    {
        public string StoryKey {  get; set; }
        public string Author { get; set; }
        public string Body { get; set; }
        public string Created { get; set; }
        public string Updated { get; set; }
    }
    
}
