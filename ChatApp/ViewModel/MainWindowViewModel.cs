using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatApp.Model;
using ChatApp.ViewModel.Commands;
using ChatApp.View;
using System.Collections.ObjectModel;



namespace ChatApp.ViewModel
{
 public class MainWindowViewModel : INotifyPropertyChanged //INotifyPropertyChanged allows data binding to update the UI when properties change
    {
        private readonly ConversationStorage _storage;
        private List<Conversation> _allConversations = new();
        public ObservableCollection<Conversation> Conversations { get; }
            = new ObservableCollection<Conversation>();
        private string _searchText = "";
        public string SearchText // Text entered in the search box
        {
            get => _searchText;
            set
            {
                _searchText = value;
                Raise(nameof(SearchText));
                ApplySearch();
            }
        }
        private void ApplySearch()
        {
            Conversations.Clear();

            var filtered = _allConversations
                .Where(c =>
                    c.PeerName.Contains(_searchText,
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.LastUpdated);

            foreach (var convo in filtered)
                Conversations.Add(convo);
        }
        private Conversation? _selectedConversation; // Currently selected conversation in the UI
        public Conversation? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                _selectedConversation = value;
                Raise(nameof(SelectedConversation));
                OpenConversation();
            }
        }
private void OpenConversation()
{
    if (_selectedConversation == null)
        return;

    var chat = new ChatWindow();
    chat.DataContext = new ChatWindowViewModel(
        _selectedConversation,
        _storage);

    chat.Show();
}


        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _myName = "";
        public string MyName
        {
            get => _myName;
            set { _myName = value; Raise(nameof(MyName)); }
        }

        private int _listenPort;
        public int ListenPort
        {
            get => _listenPort;
            set { _listenPort = value; Raise(nameof(ListenPort)); }
        }

        private string _peerIP = "";
        public string PeerIP
        {
            get => _peerIP;
            set { _peerIP = value; Raise(nameof(PeerIP)); }
        }

        private int _peerPort;
        public int PeerPort
        {
            get => _peerPort;
            set { _peerPort = value; Raise(nameof(PeerPort)); }
        }

        private string _connectionStatus = "Not connected";
        public string ConnectionStatus  // Status message to show current connection state
        {
            get => _connectionStatus;
            set { _connectionStatus = value; Raise(nameof(ConnectionStatus)); }
        }


        public ICommand StartListeningCommand { get; }  // This command handles starting the listener
        public ICommand ConnectCommand { get; }         // This command handles sending invites


        private readonly NetworkingService _net;

        public MainWindowViewModel()
        {
            _storage = new ConversationStorage();

            _allConversations = _storage.Conversations
                .OrderByDescending(c => c.LastUpdated)
                .ToList();

            foreach (var convo in _allConversations)
                Conversations.Add(convo);



            _net = new NetworkingService();
            _net.OnStatus += msg => ConnectionStatus = msg;
            _net.OnInviteReceived += name =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var popup = new InvitePopup();
                    popup.DataContext = new InvitePopupViewModel(
                        name,
                        _net,
                        _storage,
                        MyName,
                        () => popup.Close()
                    );

                    popup.Show();
                });
            };
            //_net.OnInviteAccepted += () => ConnectionStatus = "[SUCCESS] Invite accepted!";
            _net.OnInviteAccepted += (peerName) =>
            {
                ConnectionStatus = $"Connected to {peerName}";

                var conversation = new Conversation(peerName); // we save chat history per peer using LINQ to find existing conversation
                _allConversations.Add(conversation);
                ApplySearch();
                _storage.SaveAll();

                App.Current.Dispatcher.Invoke(() =>
                {
                    var chat = new ChatWindow();
                    chat.DataContext = new ChatWindowViewModel(
                        _net,
                        conversation,
                        _storage,
                        MyName);

                    chat.Show();
                });
            };



            _net.OnInviteRejected += () => ConnectionStatus = "[INFO] Invite rejected.";
            StartListeningCommand = new RelayCommand(async () =>
            {
                ConnectionStatus = "Starting listener...";
                await _net.StartListeningAsync(ListenPort);
            });

            ConnectCommand = new RelayCommand(async () =>   //This command handles sending invites
            {
                ConnectionStatus = $"Connecting to {PeerIP}:{PeerPort}...";
                await _net.ConnectAsync(PeerIP, PeerPort);

                // After connected send invite
                await _net.SendJsonAsync(new
                {
                    type = "invite",
                    name = MyName
                });

                //ConnectionStatus = "[INVITE] Sent invite.";
            });

        }
    }
}
