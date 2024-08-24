using System.Reflection;

namespace chatgot.Models
{
    public class GotConversationDto
    {
        public Model Model { get; set; }
        public List<GotMessage> Messages { get; set; }
        public string NetworkModelId { get; set; }
        public string Type { get; set; }
        public string Timezone { get; set; }
    }

    public class GotMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class Model
    {
        public string Id { get; set; }
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Placeholder { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public string Level { get; set; }
        public string ContentLength { get; set; }
        public bool DefaultRec { get; set; }
        public string PicConv { get; set; }
    }

}
