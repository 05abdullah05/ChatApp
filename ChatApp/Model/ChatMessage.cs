using System;

namespace ChatApp.Model
{
    public class ChatMessage
    {
        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";
        public bool IsMine { get; set; } 
        public DateTime Timestamp { get; set; }

        // Parameterless constructor (needed for JSON deserialization)
        public ChatMessage() { }

        public ChatMessage(string sender, string text, bool isMine = false)
        {
            Sender = sender;
            Text = text;
            IsMine = isMine;
            Timestamp = DateTime.Now;
        }
    }
}
