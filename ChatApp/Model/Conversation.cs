using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    

namespace ChatApp.Model
{
    public class Conversation
    {
        public string PeerName { get; set; }
        public DateTime LastUpdated { get; set; }

        public List<ChatMessage> Messages { get; set; } = new();

        public Conversation() { } // For JSON

        public Conversation(string peerName)
        {
            PeerName = peerName;
            LastUpdated = DateTime.Now;
        }

        public void AddMessage(ChatMessage message)
        {
            Messages.Add(message);
            LastUpdated = message.Timestamp;
        }
    }
}
