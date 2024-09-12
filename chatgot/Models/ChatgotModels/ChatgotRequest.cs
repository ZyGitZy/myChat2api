using System.Reflection;
using chatgot.Models;

namespace chatgot.Models.ChatgotModels
{
    public class ChatgotRequest
    {
        public string clId { get; set; }

        private string _model;
        public string model
        {
            get
            {
                if (_model.Contains("gpt")) 
                {
                    return $"openai/{_model}";
                }

                if (_model.Contains("claude")) 
                {
                    return $"anthropic/{_model}";
                }

                return _model;
            }
            set {
                _model = value;
            }
        }
        public string prompt { get; set; }
        public string webAccess { get; set; }
        public string timezone { get; set; }
    }



    public class Model
    {
        public string id { get; set; }
        public string owner { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string icon { get; set; }
        public string placeholder { get; set; }
        public string description { get; set; }
        public int order { get; set; }
        public string type { get; set; }
        public string level { get; set; }
        public string contentLength { get; set; }
        public bool defaultRec { get; set; }
        public string picConv { get; set; }
    }

}
