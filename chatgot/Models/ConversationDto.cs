namespace chatgot.Models
{
    public class ConversationDto
    {
        public List<GotMessage> messages { get; set; } = new();
        public bool stream { get; set; } = false;
        public string model { get; set; } = string.Empty;
        public double? temperature { get; set; }
        public double? presencePenalty { get; set; }
        public double? frequencypenalty { get; set; }
        public double? topp { get; set; }
    }

    public class SystemContent
    {
        public string description { get; set; } = string.Empty;
        public string currentModel { get; set; } = string.Empty;
        public DateTime? currentTime { get; set; }
        public string timezone { get; set; } = string.Empty;
        public string latexInline { get; set; } = string.Empty;
        public string latexBlock { get; set; } = string.Empty;
    }
}
