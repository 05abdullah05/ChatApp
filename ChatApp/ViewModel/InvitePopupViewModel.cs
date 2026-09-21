using ChatApp.Model;
using ChatApp.View;
using ChatApp.ViewModel.Commands;
using System.Windows.Input;

namespace ChatApp.ViewModel
{
    public class InvitePopupViewModel
    {
        public string IncomingName { get; }
        public ICommand AcceptCommand { get; }
        public ICommand RejectCommand { get; }

        private readonly NetworkingService _net;
        private readonly Invoker _closeAction;
        private readonly string _myName;
        private readonly ConversationStorage _storage;


        public delegate void Invoker();

        public InvitePopupViewModel(string incomingName, NetworkingService net, ConversationStorage storage, string myName,Invoker close)
        {
            IncomingName = incomingName;
            _net = net;
            _storage = storage;
            _myName = myName;
            _closeAction = close;

            AcceptCommand = new RelayCommand(async () =>
            {
                await _net.SendJsonAsync(new
                {
                    type = "accept",
                    name = _myName
                });

                App.Current.Dispatcher.Invoke(() =>
                {
                    var conversation = _storage.GetOrCreate(IncomingName);
                    var chat = new ChatWindow();
                    chat.DataContext = new ChatWindowViewModel(
                        _net,
                        conversation,
                        _storage,
                        _myName);

                    chat.Show();
                });

                _closeAction();
            });


            RejectCommand = new RelayCommand(async () =>
            {
                await _net.SendJsonAsync(new { type = "reject" });
                _closeAction();
            });
        }
    }
}
