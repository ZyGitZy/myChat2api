namespace chatgot.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class Merlin: TargetDto
    {
        public Action action { get; set; }
        public List<ActiveThreadSnippet> activeThreadSnippet { get; set; }
        public string chatId { get; set; }
        public string language { get; set; }
        public object metadata { get; set; }
        public string mode { get; set; }
        public string model { get; set; }
        public PersonaConfig personaConfig { get; set; }
    }

    public class Action
    {
        public MerlinMessage message { get; set; }
        public string type { get; set; }
    }

    public class MerlinMessage
    {
        public List<string> attachments { get; set; }
        public string content { get; set; }
        public MerlinMetadata metadata { get; set; }
        public string parentId { get; set; }
        public string role { get; set; }
    }

    public class MerlinMetadata
    {
        public string context { get; set; }
    }

    public class ActiveThreadSnippet
    {
        public List<string> attachments { get; set; }
        public string content { get; set; }
        public string id { get; set; }
        public MerlinMetadata metadata { get; set; }
        public string parentId { get; set; }
        public string role { get; set; }
        public string status { get; set; }
        public int activeChildIdx { get; set; }
        public int totalChildren { get; set; }
        public int idx { get; set; }
        public int totSiblings { get; set; }
    }

    public class PersonaConfig
    {
    }

    public class MerlinResponse 
    {
        public string status { get; set; }

        public DataModel data { get; set; }
    }

    public class DataModel
    {
        public string content { get; set; }

        public string eventType { get; set; }
    }

}
