namespace chatgot.Models.MerlinModels
{
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
