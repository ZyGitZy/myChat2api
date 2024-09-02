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
}