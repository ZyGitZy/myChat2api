using System.Reflection;

namespace chatgot.Models
{
    public class GotConversationDto: TargetDto
    {
        public Model model { get; set; }
        public List<GotMessage> messages { get; set; }
        public string networkModelId { get; set; }
        public string type { get; set; }
        public string timezone { get; set; }
    }

    public class GotMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Model
    {
        public string id { get; set; }
        public string owner { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string icon { get; set; }
        public string placeholder { get; set; }
        public string description { get; set; }
        public int order { get; set; }
        public string type { get; set; }
        public string level { get; set; }
        public string contentLength { get; set; }
        public bool defaultRec { get; set; }
        public string picConv { get; set; }
    }

}
