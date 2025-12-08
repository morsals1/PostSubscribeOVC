using System.Windows;
using System.Windows.Controls;

namespace WpfSUB.Pages
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

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
            NavigationService.Navigate(new ReportPage("pricelist"));
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

        private void Receipts_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ReportPage("receipts"));
        }
    }
}