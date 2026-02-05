using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class ClientFormPage : Page
    {
        private AppDbContext _context;
        private Client _client;
        private bool _isEditMode = false;

        public ClientFormPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _client = new Client();
            _client.Profile = new ClientProfile();
            DataContext = _client;
        }

        public ClientFormPage(Client editClient)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _client = _context.Clients
                .Include(c => c.Profile)
                .FirstOrDefault(c => c.Id == editClient.Id) ?? editClient;
            _isEditMode = true;

            DataContext = _client;
            Title = "Редактирование клиента";
            SaveButton.Content = "Обновить";
        }

        private bool ValidateForm()
        {
            // Проверка ФИО
            if (string.IsNullOrWhiteSpace(_client.FullName) || _client.FullName.Length < 3)
            {
                MessageBox.Show("ФИО должно содержать не менее 3 символов",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return false;
            }

            // Проверка адреса
            if (string.IsNullOrWhiteSpace(_client.Address))
            {
                MessageBox.Show("Введите адрес клиента",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                AddressTextBox.Focus();
                return false;
            }

            // Проверка паспортных данных
            if (!string.IsNullOrWhiteSpace(_client.PassportSeries))
            {
                if (_client.PassportSeries.Length != 4 || !_client.PassportSeries.All(char.IsDigit))
                {
                    MessageBox.Show("Серия паспорта должна состоять из 4 цифр",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PassportSeriesTextBox.Focus();
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_client.PassportNumber))
            {
                if (_client.PassportNumber.Length != 6 || !_client.PassportNumber.All(char.IsDigit))
                {
                    MessageBox.Show("Номер паспорта должен состоять из 6 цифр",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PassportNumberTextBox.Focus();
                    return false;
                }
            }

            // Проверка уникальности паспорта
            if (!string.IsNullOrWhiteSpace(_client.PassportSeries) &&
                !string.IsNullOrWhiteSpace(_client.PassportNumber))
            {
                var existingClient = _context.Clients
                    .FirstOrDefault(c => c.Id != _client.Id &&
                                        c.PassportSeries == _client.PassportSeries &&
                                        c.PassportNumber == _client.PassportNumber);

                if (existingClient != null)
                {
                    MessageBox.Show("Клиент с такими паспортными данными уже существует:\n" +
                                   $"{existingClient.FullName} (ID: {existingClient.Id})",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            // Проверка email
            if (!string.IsNullOrWhiteSpace(_client.Email) && !IsValidEmail(_client.Email))
            {
                MessageBox.Show("Введите корректный email адрес",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                if (_isEditMode)
                {
                    _context.Clients.Update(_client);
                }
                else
                {
                    _client.RegistrationDate = DateTime.Now;
                    _context.Clients.Add(_client);
                }

                _context.SaveChanges();

                string message = _isEditMode
                    ? "Данные клиента обновлены"
                    : "Новый клиент добавлен";

                MessageBox.Show(message, "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new ClientPage());
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Ошибка сохранения в базу данных: {ex.InnerException?.Message ?? ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void GeneratePassport_Click(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            PassportSeriesTextBox.Text = random.Next(1000, 9999).ToString();
            PassportNumberTextBox.Text = random.Next(100000, 999999).ToString();
        }

        private void ClearProfile_Click(object sender, RoutedEventArgs e)
        {
            _client.Profile.Phone = "";
            _client.Profile.Bio = "";
            _client.Profile.Preferences = "";
            _client.Profile.AvatarUrl = "";
            _client.Profile.Birthday = null;

            MessageBox.Show("Поля профиля очищены", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}