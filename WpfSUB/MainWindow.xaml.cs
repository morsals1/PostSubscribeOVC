using System.Windows;
using WpfSUB.Pages;
using WpfSUB.Services;

namespace WpfSUB
{
    public partial class MainWindow : Window
    {
        private SubscriptionAutoProcessor _autoProcessor;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _autoProcessor = new SubscriptionAutoProcessor();

            MainFrame.Navigate(new LoginPage());
        }

        private void MainWindow_Closed(object sender, System.EventArgs e)
        {
            _autoProcessor?.Dispose();
        }
    }
}