namespace chatgot.Models
{
    public class GotCompletionResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public long? Created { get; set; }
        public string? Model { get; set; }
        public string? System_Fingerprint { get; set; }
        public List<Choice>? Choices { get; set; }
    }

    public class GotChoice
    {
        public int? Index { get; set; }
        public GotDelta? Delta { get; set; }
        public object? Logprobs { get; set; } // 需要根据实际数据结构来定义
        public string? FinishReason { get; set; }
    }

    public class GotDelta
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }
}