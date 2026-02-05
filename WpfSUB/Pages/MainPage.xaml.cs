using System.Windows;
using System.Windows.Controls;
using WpfSUB.Services;

namespace WpfSUB.Pages
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверяем авторизацию
            if (!SessionService.IsLoggedIn)
            {
                MessageBox.Show("Требуется авторизация", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.Navigate(new LoginPage());
                return;
            }

            // Обновляем отображение имени оператора
            CurrentOperatorText.Text = SessionService.OperatorFullName;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SessionService.Logout();
                NavigationService.Navigate(new LoginPage());
            }
        }

        // Остальные обработчики событий остаются без изменений
        private void Clients_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ClientPage());
        }

        private void NewClient_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ClientFormPage());
        }

        private void Publications_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PublicationPage());
        }

        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CategoryPage());
        }

        private void PriceList_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ReportPage());
        }

        private void Subscriptions_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SubscriptionPage());
        }

        private void NewSubscription_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SubscriptionFormPage());
        }

        private void Payments_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PaymentPage());
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ReportPage());
        }
    }
}