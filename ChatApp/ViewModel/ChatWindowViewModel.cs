using ChatApp.Model;
using ChatApp.ViewModel.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace ChatApp.ViewModel
{
    public class ChatWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? OnBuzz;

        private void Raise(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly NetworkingService? _net;
        private readonly Conversation _conversation;
        private readonly ConversationStorage _storage;
        private readonly string _myName;
        private readonly bool _isHistoryMode;

        public ObservableCollection<ChatMessage> Messages { get; }

        private string _outgoingText = "";
        public string OutgoingText
        {
            get => _outgoingText;
            set { _outgoingText = value; Raise(nameof(OutgoingText)); }
        }

        public ICommand SendCommand { get; }
        public ICommand BuzzCommand { get; }


        // ===============================
        // LIVE CHAT CONSTRUCTOR
        // ===============================
        public ChatWindowViewModel(
            NetworkingService net,
            Conversation conversation,
            ConversationStorage storage,
            string myName)
        {
            _net = net;
            _conversation = conversation;
            _storage = storage;
            _myName = myName;
            _isHistoryMode = false;

            Messages = new ObservableCollection<ChatMessage>(conversation.Messages);

            SendCommand = new RelayCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(OutgoingText))
                    return;

                var msg = new ChatMessage(_myName, OutgoingText, isMine: true);

                _conversation.AddMessage(msg);
                Messages.Add(msg);
                _storage.SaveAll();

                await _net.SendMessageAsync(_myName, OutgoingText);
                OutgoingText = "";
            });
            BuzzCommand = new RelayCommand(async () =>
            {
                if (_net == null)
                    return;

                await _net.SendBuzzAsync();
            });


            _net.OnMessageReceived += (sender, text) =>
            {
                var msg = new ChatMessage(sender, text, isMine: false);

                App.Current.Dispatcher.Invoke(() =>
                {
                    _conversation.AddMessage(msg);
                    Messages.Add(msg);
                    _storage.SaveAll();
                });
            };

            _net.OnBuzzReceived += () =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    OnBuzz?.Invoke();
                });
            };


            _net.OnDisconnected += () =>
            {
                _storage.SaveAll();
            };
        }

        // ===============================
        // HISTORY VIEW CONSTRUCTOR
        // ===============================
        public ChatWindowViewModel(
            Conversation conversation,
            ConversationStorage storage)
        {
            _conversation = conversation;
            _storage = storage;
            _isHistoryMode = true;

            Messages = new ObservableCollection<ChatMessage>(conversation.Messages);

            SendCommand = new RelayCommand(() => { }, () => false);
        }
    }
}
