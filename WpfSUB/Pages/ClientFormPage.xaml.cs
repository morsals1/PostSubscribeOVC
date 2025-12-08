using System.Windows;
using System.Windows.Controls;
using WpfSUB.Models;
using WpfSUB.Services;

namespace WpfSUB.Pages
{
    public partial class ClientFormPage : Page
    {
        private ClientService _clientService;
        private Client _client;
        private bool _isEditMode = false;

        public ClientFormPage()
        {
            InitializeComponent();
            _clientService = new ClientService();
            _client = new Client();
        }

        public ClientFormPage(Client editClient)
        {
            InitializeComponent();
            _clientService = new ClientService();
            _client = editClient;
            _isEditMode = true;

            // Заполняем поля данными клиента
            FullNameTextBox.Text = editClient.FullName;
            AddressTextBox.Text = editClient.Address;
            PhoneTextBox.Text = editClient.Phone;
            EmailTextBox.Text = editClient.Email;
            PassportSeriesTextBox.Text = editClient.PassportSeries;
            PassportNumberTextBox.Text = editClient.PassportNumber;
            IssuedByTextBox.Text = editClient.IssuedBy;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                MessageBox.Show("Введите ФИО клиента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _client.FullName = FullNameTextBox.Text;
            _client.Address = AddressTextBox.Text ?? "";
            _client.Phone = PhoneTextBox.Text ?? "";
            _client.Email = EmailTextBox.Text ?? "";
            _client.PassportSeries = PassportSeriesTextBox.Text ?? "";
            _client.PassportNumber = PassportNumberTextBox.Text ?? "";
            _client.IssuedBy = IssuedByTextBox.Text ?? "";

            // Убедимся, что профиль инициализирован
            if (_client.Profile == null)
            {
                _client.Profile = new ClientProfile();
            }

            // Инициализируем поля профиля
            _client.Profile.Phone ??= "";
            _client.Profile.Bio ??= "";
            _client.Profile.Preferences ??= "";
            _client.Profile.AvatarUrl ??= "";

            if (_isEditMode)
            {
                _clientService.Update(_client);
                MessageBox.Show("Клиент обновлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _clientService.Add(_client);
                MessageBox.Show("Клиент добавлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            NavigationService.GoBack();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}