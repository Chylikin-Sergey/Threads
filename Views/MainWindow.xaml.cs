using MahApps.Metro.Controls;
using System.Windows.Controls;
using System.Windows.Input;

namespace Threads
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        public MainWindow ()
        {
            InitializeComponent();

        }

        private void Window_MouseDown (object sender, MouseButtonEventArgs e) => Keyboard.ClearFocus();

        private void TextBox_PreviewGotKeyboardFocus (object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;

        }

        private void TextBox_MouseDoubleClick (object sender, MouseButtonEventArgs e)
        {
            ((TextBox)sender).IsReadOnly = false;
        }

        private void TextBox_KeyDown (object sender, KeyEventArgs e)
        {
            if(e.Key.Equals(Key.Enter))
            {
                Keyboard.ClearFocus();
            }
        }
    }
}
