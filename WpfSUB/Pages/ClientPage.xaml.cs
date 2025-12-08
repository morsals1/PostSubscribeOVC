using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using WpfSUB.Models;
using WpfSUB.Services;
using System.Linq;

namespace WpfSUB.Pages
{
    public partial class ClientPage : Page
    {
        private ClientService _clientService;
        private ObservableCollection<Client> _clients;

        public ClientPage()
        {
            InitializeComponent();
            _clientService = new ClientService();
            LoadClients();
        }

        private void LoadClients()
        {
            _clientService.GetAll();
            _clients = new ObservableCollection<Client>(_clientService.Clients);
            ClientsListView.ItemsSource = _clients;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = ClientsListView.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private void ClientsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new MainPage());
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ClientFormPage());
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsListView.SelectedItem is Client selectedClient)
            {
                NavigationService.Navigate(new ClientFormPage(selectedClient));
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsListView.SelectedItem is Client selectedClient)
            {
                var result = MessageBox.Show($"Удалить клиента {selectedClient.FullName}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _clientService.Remove(selectedClient);
                    _clients.Remove(selectedClient);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadClients();
        }
    }
}