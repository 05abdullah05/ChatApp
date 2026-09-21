using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ChatApp.Model
{
    public class ConversationStorage
    {
        private readonly string _filePath;
        private List<Conversation> _conversations = new();

        public IReadOnlyList<Conversation> Conversations => _conversations;

        public ConversationStorage()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChatApp");

            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "conversations.json");

            Load();
        }

        public void Load()
        {
            if (!File.Exists(_filePath))
            {
                _conversations = new();
                return;
            }

            string json = File.ReadAllText(_filePath);
            _conversations =
                JsonSerializer.Deserialize<List<Conversation>>(json)
                ?? new();
        }

        public void SaveAll()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(_conversations, options);
            File.WriteAllText(_filePath, json);
        }

        public Conversation GetOrCreate(string peerName)
        {
            var convo = new Conversation(peerName);
            _conversations.Add(convo);
            return convo;
        }
    }
}
