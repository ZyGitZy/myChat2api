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
}
