namespace chatgot.Models.NotdiamondModels
{
    public class NotdiamondRequest : CompletionsDto
    {
        public Povider provider { get; set; }
    }

    public class Povider 
    {
        public string model { get; set; }

        public string provider { get; set; }
    }
}
