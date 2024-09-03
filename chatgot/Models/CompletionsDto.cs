namespace chatgot.Models
{
    public class CompletionsDto
    {
        public List<Message> messages { get; set; } = new();
        public bool stream { get; set; } = false;
        public string model { get; set; } = string.Empty;
        public double? temperature { get; set; } = 0.8;
        public double? presencePenalty { get; set; }
        public double? frequencypenalty { get; set; }
        public double? topp { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
