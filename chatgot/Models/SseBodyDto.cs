namespace chatgot.Models
{
    public class SseBodyDto
    {
        public string Id { get; set; } = "";

        public int Retry { get; set; } = 1000;

        public string Event { get; set; } = "";
        public CompletionResponseDto? Data { get; set; }
    }
}
