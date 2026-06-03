using PubSub.models;

namespace PubSub.models
{
    public class LoveLetter
    {
        public Person? Sender { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderCity { get; set; } = string.Empty;
        public int SenderAge { get; set; }
        public string SenderPhone { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
