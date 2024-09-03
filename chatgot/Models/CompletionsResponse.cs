using Newtonsoft.Json;

namespace chatgot.Models
{
    public class CompletionsResponse
    {
        public string? id { get; set; }

        [JsonProperty("object")]
        public string? Object { get; set; }
        public long? created { get; set; }
        public string? model { get; set; }
        public string? system_fingerprint { get; set; }
        public List<Choice>? choices { get; set; }
        public object usage { get; set; }
    }

    public class Choice
    {
        public Delta? delta { get; set; }
        public object? logprobs { get; set; }
        public string? finish_reason { get; set; }
        public int? index { get; set; }
    }

    public class Delta
    {
        public string? content { get; set; }
    }
}
