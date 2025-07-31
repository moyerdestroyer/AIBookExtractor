using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AIBookExtractor.Views.Controls
{
    public partial class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string Message { get; set; } = "";

        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        public static async Task Show(Window owner, string title, string message)
        {
            var dialog = new MessageBox
            {
                Title = title,
                Message = message
            };
            await dialog.ShowDialog(owner);
        }
    }
}