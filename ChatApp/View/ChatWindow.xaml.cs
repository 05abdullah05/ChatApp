using ChatApp.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

/* using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; */

namespace ChatApp.View
{
    public partial class ChatWindow : Window
    {
        public ChatWindow()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                if (DataContext is ChatWindowViewModel vm)
                {
                    vm.OnBuzz += ShakeWindow;
                }
            };
        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ChatWindowViewModel vm)
                {
                    if (vm.SendCommand.CanExecute(null))
                        vm.SendCommand.Execute(null);
                }
            }
        }
        private void ShakeWindow()
        {
            var transform = new TranslateTransform();
            RenderTransform = transform;

            var animation = new DoubleAnimation
            {
                From = -0,
                To = 10,
                Duration = TimeSpan.FromMilliseconds(40),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(5)
            };

            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

    }
}
