using chatgot.Models;

namespace chatgot.Models.ChatgotModels
{
    public class ChatgotResponse
    {
        public ChatgotResponseData data { get; set; }
    }

    public class ChatgotResponseData 
    {
        public string content { get; set; }
    }
}