using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class ClientPage : Page
    {
        private ObservableCollection<Client> _clients;
        private AppDbContext _context;

        public ClientPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadClients();
        }

        private void LoadClients()
        {
            var clients = _context.Clients
                .Include(c => c.Profile)
                .Include(c => c.Subscriptions)
                .ThenInclude(s => s.Publication)
                .OrderBy(c => c.FullName)
                .ToList();

            _clients = new ObservableCollection<Client>(clients);
            ClientsListView.ItemsSource = _clients;
            UpdateStatistics();
            UpdateButtonStates();
        }

        private void UpdateStatistics()
        {
            int totalClients = _clients.Count;
            int activeSubscriptions = _clients.Sum(c => c.Subscriptions.Count(s => s.Status == "активна"));
            decimal totalRevenue = _clients.Sum(c =>
                c.Subscriptions.Where(s => s.IsFullyPaid).Sum(s => s.TotalPrice));

            StatsTextBlock.Text = $"Всего клиентов: {totalClients} | " +
                                 $"Активных подписок: {activeSubscriptions} | " +
                                 $"Общая выручка: {totalRevenue:C}";
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = ClientsListView.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
            ViewSubscriptionsButton.IsEnabled = hasSelection;
            CreateSubscriptionButton.IsEnabled = hasSelection;
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
                if (selectedClient.Subscriptions?.Any() == true)
                {
                    MessageBox.Show("Нельзя удалить клиента, у которого есть подписки.\n" +
                                   "Сначала удалите или передайте подписки.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"Удалить клиента \"{selectedClient.FullName}\"?\n" +
                                           $"Паспорт: {selectedClient.PassportSeries} {selectedClient.PassportNumber}",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _context.Clients.Remove(selectedClient);
                    _context.SaveChanges();
                    _clients.Remove(selectedClient);

                    MessageBox.Show("Клиент удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadClients();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                ClientsListView.ItemsSource = _clients;
            }
            else
            {
                var filtered = _clients.Where(c =>
                    c.FullName.ToLower().Contains(searchText) ||
                    c.Phone?.ToLower().Contains(searchText) == true ||
                    c.Email?.ToLower().Contains(searchText) == true ||
                    (c.PassportSeries + c.PassportNumber).Contains(searchText))
                    .ToList();

                ClientsListView.ItemsSource = new ObservableCollection<Client>(filtered);
            }
        }

        private void ViewSubscriptions_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsListView.SelectedItem is Client selectedClient)
            {
                var subscriptions = _context.Subscriptions
                    .Include(s => s.Publication)
                    .Include(s => s.Payments)
                    .Where(s => s.ClientId == selectedClient.Id)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToList();

                string subscriptionInfo = $"Подписки клиента: {selectedClient.FullName}\n\n";

                if (!subscriptions.Any())
                {
                    subscriptionInfo += "Нет подписок";
                }
                else
                {
                    foreach (var subscription in subscriptions)
                    {
                        string statusIcon = subscription.Status switch
                        {
                            "активна" => "✅",
                            "ожидает_оплаты" => "⏳",
                            "оплачена" => "💰",
                            "завершена" => "🏁",
                            _ => "📄"
                        };

                        subscriptionInfo += $"{statusIcon} {subscription.Publication.Title}\n" +
                                          $"  Период: {subscription.PeriodMonths} мес.\n" +
                                          $"  Сумма: {subscription.TotalPrice:C}\n" +
                                          $"  Статус: {subscription.Status}\n" +
                                          $"  Дата: {subscription.CreatedDate:dd.MM.yyyy}\n\n";
                    }
                }

                MessageBox.Show(subscriptionInfo, "Подписки клиента",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CreateSubscription_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsListView.SelectedItem is Client selectedClient)
            {
                var formPage = new SubscriptionFormPage();
                formPage.SetClient(selectedClient);
                NavigationService.Navigate(formPage);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем наличие клиентов
                if (_clients == null || _clients.Count == 0)
                {
                    MessageBox.Show("Нет клиентов для экспорта", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Используем StringBuilder для эффективной конкатенации
                System.Text.StringBuilder exportBuilder = new System.Text.StringBuilder();

                exportBuilder.AppendLine("Список клиентов системы подписки");
                exportBuilder.AppendLine($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm}");
                exportBuilder.AppendLine("=================================");
                exportBuilder.AppendLine();

                foreach (var client in _clients)
                {
                    int subscriptionCount = client.Subscriptions?.Count ?? 0;

                    exportBuilder.AppendLine($"Клиент: {client.FullName}");
                    exportBuilder.AppendLine($"Адрес: {client.Address}");
                    exportBuilder.AppendLine($"Телефон: {client.Phone}");
                    exportBuilder.AppendLine($"Email: {client.Email}");
                    exportBuilder.AppendLine($"Паспорт: {client.PassportSeries} {client.PassportNumber}");
                    exportBuilder.AppendLine($"Дата регистрации: {client.RegistrationDate:dd.MM.yyyy}");
                    exportBuilder.AppendLine($"Подписок: {subscriptionCount}");
                    exportBuilder.AppendLine("---------------------------------");
                }

                string exportText = exportBuilder.ToString();

                // Используем WPF SaveFileDialog
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                    FilterIndex = 1,
                    FileName = $"Клиенты_{DateTime.Now:yyyyMMdd_HHmm}.txt",
                    DefaultExt = ".txt",
                    Title = "Экспорт клиентов"
                };

                // В WPF ShowDialog() возвращает bool?
                if (saveDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, exportText, System.Text.Encoding.UTF8);

                    MessageBox.Show($"Данные экспортированы в файл:\n{saveDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}