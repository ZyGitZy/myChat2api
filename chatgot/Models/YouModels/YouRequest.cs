namespace chatgot.Models.YouModels
{
    public class YouRequest
    {
        public int page { get; set; } = 1;
        public int count { get; set; } = 10;
        public string safeSearch { get; set; } = "Moderate";
        public string mkt { get; set; } = "zh-CN";
        public string domain { get; set; } = "youchat";
        public bool use_personalization_extraction { get; set; } = true;
        public string queryTraceId { get; set; } = "";
        public string chatId { get; set; }
        public string conversationTurnId { get; set; }
        public int pastChatLength { get; set; }
        public string selectedChatMode { get; set; } = "custom";
        public string selectedAiModel { get; set; }
        public string traceId { get; set; }
        public string q { get; set; }
        public string chat { get; set; }
    }

    public class Answer
    {
        public string question { get; set; }
        public string answer { get; set; }
    }
}
