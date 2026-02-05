using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;
using WpfSUB.Services;

namespace WpfSUB.Pages
{
    public partial class SubscriptionFormPage : Page
    {
        private SubscriptionService _subscriptionService;
        private Subscription _editingSubscription;
        private bool _isEditMode = false;
        private AppDbContext _context;
        private Client _selectedClient;
        private Publication _selectedPublication;

        public SubscriptionFormPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _subscriptionService = new SubscriptionService();
            LoadData();
        }

        // Конструктор для редактирования
        public SubscriptionFormPage(Subscription subscription) : this()
        {
            _editingSubscription = subscription;
            _isEditMode = true;

            // Заполняем форму данными редактируемой подписки
            if (_editingSubscription != null)
            {
                Title = "Редактирование подписки";

                // Находим и выбираем клиента
                if (ClientComboBox.ItemsSource is System.Collections.IList clients)
                {
                    foreach (Client client in clients)
                    {
                        if (client.Id == _editingSubscription.ClientId)
                        {
                            ClientComboBox.SelectedItem = client;
                            _selectedClient = client;
                            break;
                        }
                    }
                }

                // Находим и выбираем издание
                if (PublicationComboBox.ItemsSource is System.Collections.IList publications)
                {
                    foreach (Publication publication in publications)
                    {
                        if (publication.Id == _editingSubscription.PublicationId)
                        {
                            PublicationComboBox.SelectedItem = publication;
                            _selectedPublication = publication;
                            break;
                        }
                    }
                }

                // Выбираем период
                foreach (ComboBoxItem item in PeriodComboBox.Items)
                {
                    if (int.TryParse(item.Content.ToString(), out int months) &&
                        months == _editingSubscription.PeriodMonths)
                    {
                        PeriodComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Устанавливаем дату
                StartDatePicker.SelectedDate = _editingSubscription.PlannedStartDate ??
                                              DateTime.Today.AddMonths(1);

                // Рассчитываем стоимость
                CalculateTotal();

                // Обновляем информацию
                UpdateSubscriptionInfo();
            }
        }

        public void SetClient(Client client)
        {
            if (ClientComboBox.ItemsSource is System.Collections.IList clients)
            {
                foreach (Client c in clients)
                {
                    if (c.Id == client.Id)
                    {
                        ClientComboBox.SelectedItem = c;
                        _selectedClient = c;
                        break;
                    }
                }
            }
        }

        private void LoadData()
        {
            try
            {
                // Загружаем клиентов
                var clients = _context.Clients
                    .OrderBy(c => c.FullName)
                    .ToList();
                ClientComboBox.ItemsSource = clients;

                // Загружаем издания
                var publications = _context.Publications
                    .Include(p => p.Category)
                    .Where(p => p.IsAvailable || _isEditMode)
                    .OrderBy(p => p.Title)
                    .ToList();
                PublicationComboBox.ItemsSource = publications;

                // Загружаем услуги
                var services = _context.AdditionalServices
                    .Where(s => s.IsActive || _isEditMode)
                    .OrderBy(s => s.Name)
                    .ToList();

                ServicesPanel.Children.Clear();
                foreach (var service in services)
                {
                    var checkBox = new CheckBox
                    {
                        Content = $"{service.Name} ({service.Price:C})",
                        Tag = service,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    checkBox.Checked += Service_CheckedChanged;
                    checkBox.Unchecked += Service_CheckedChanged;
                    ServicesPanel.Children.Add(checkBox);
                }

                if (!_isEditMode)
                {
                    if (clients.Any()) ClientComboBox.SelectedIndex = 0;
                    if (publications.Any()) PublicationComboBox.SelectedIndex = 0;
                    PeriodComboBox.SelectedIndex = 2; // 12 месяцев по умолчанию

                    // Устанавливаем дату начала на следующий месяц
                    StartDatePicker.SelectedDate = DateTime.Today.AddMonths(1);
                }

                CalculateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Service_CheckedChanged(object sender, RoutedEventArgs e)
        {
            CalculateTotal();
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            try
            {
                if (PublicationComboBox.SelectedItem is Publication publication &&
                    PeriodComboBox.SelectedItem is ComboBoxItem periodItem &&
                    int.TryParse(periodItem.Content.ToString(), out int months))
                {
                    decimal total = publication.CalculatePriceForPeriod(months);

                    // Добавляем стоимость услуг
                    foreach (CheckBox checkBox in ServicesPanel.Children)
                    {
                        if (checkBox.IsChecked == true && checkBox.Tag is AdditionalService service)
                        {
                            total += service.Price;
                        }
                    }

                    TotalPriceText.Text = $"{total:C}";
                    UpdateSubscriptionInfo();
                }
                else
                {
                    TotalPriceText.Text = "0,00 ₽";
                    SubscriptionInfoText.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSubscriptionInfo()
        {
            if (_selectedClient != null && _selectedPublication != null &&
                PeriodComboBox.SelectedItem is ComboBoxItem periodItem &&
                int.TryParse(periodItem.Content.ToString(), out int months))
            {
                string info = $"Клиент: {_selectedClient.FullName}\n" +
                            $"Издание: {_selectedPublication.Title}\n" +
                            $"Период: {months} месяцев\n" +
                            $"Дата начала: {StartDatePicker.SelectedDate:dd.MM.yyyy}\n" +
                            $"Примерная дата окончания: {StartDatePicker.SelectedDate?.AddMonths(months).AddDays(-1):dd.MM.yyyy}\n" +
                            $"Стоимость: {TotalPriceText.Text}";

                SubscriptionInfoText.Text = info;
            }
            else
            {
                SubscriptionInfoText.Text = "Заполните все поля для просмотра информации";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ClientComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PublicationComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите издание", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!(PeriodComboBox.SelectedItem is ComboBoxItem periodItem) ||
                    !int.TryParse(periodItem.Content.ToString(), out int months) ||
                    months <= 0)
                {
                    MessageBox.Show("Выберите корректный период", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var client = (Client)ClientComboBox.SelectedItem;
                var publication = (Publication)PublicationComboBox.SelectedItem;
                var startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(1);

                if (startDate < DateTime.Today)
                {
                    MessageBox.Show("Дата начала не может быть в прошлом", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Рассчитываем общую сумму
                decimal totalPrice = CalculateTotalPrice();

                if (_isEditMode && _editingSubscription != null)
                {
                    // Обновляем существующую подписку
                    UpdateSubscription(client, publication, months, startDate, totalPrice);
                }
                else
                {
                    // Создаем новую подписку
                    CreateNewSubscription(client, publication, months, startDate, totalPrice);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateTotalPrice()
        {
            if (PublicationComboBox.SelectedItem is Publication publication &&
                PeriodComboBox.SelectedItem is ComboBoxItem periodItem &&
                int.TryParse(periodItem.Content.ToString(), out int months))
            {
                decimal total = publication.CalculatePriceForPeriod(months);

                // Добавляем стоимость услуг
                foreach (CheckBox checkBox in ServicesPanel.Children)
                {
                    if (checkBox.IsChecked == true && checkBox.Tag is AdditionalService service)
                    {
                        total += service.Price;
                    }
                }

                return total;
            }
            return 0;
        }

        private void CreateNewSubscription(Client client, Publication publication, int months, DateTime startDate, decimal totalPrice)
        {
            // Создаем подписку
            var subscription = _subscriptionService.CreateSubscription(
                client.Id,
                publication.Id,
                months,
                startDate,
                totalPrice);

            // Добавляем услуги
            AddServicesToSubscription(subscription.Id, startDate, months);

            MessageBox.Show($"Подписка оформлена успешно!\n" +
                           $"ID: {subscription.Id}\n" +
                           $"Сумма: {subscription.TotalPrice:C}\n" +
                           $"Срок оплаты: {subscription.PaymentDeadline:dd.MM.yyyy}",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            NavigationService.Navigate(new SubscriptionPage());
        }

        private void UpdateSubscription(Client client, Publication publication, int months, DateTime startDate, decimal totalPrice)
        {
            // Получаем полные данные подписки из базы через сервис
            var fullSubscription = _subscriptionService.Subscriptions
                .FirstOrDefault(s => s.Id == _editingSubscription.Id);

            if (fullSubscription == null)
                throw new Exception("Подписка не найдена в базе данных");

            // Обновляем поля
            fullSubscription.ClientId = client.Id;
            fullSubscription.PublicationId = publication.Id;
            fullSubscription.PeriodMonths = months;
            fullSubscription.MonthlyPrice = publication.MonthlyPrice;
            fullSubscription.TotalPrice = totalPrice;
            fullSubscription.PlannedStartDate = startDate;

            // Пересчитываем даты
            fullSubscription.CalculateDates();

            // Обновляем через сервис
            _subscriptionService.UpdateSubscription(fullSubscription);

            // Обновляем услуги
            UpdateServicesForSubscription(_editingSubscription.Id, startDate, months);

            MessageBox.Show($"Подписка ID: {_editingSubscription.Id} обновлена успешно!",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            NavigationService.Navigate(new SubscriptionPage());
        }

        private void AddServicesToSubscription(int subscriptionId, DateTime startDate, int months)
        {
            foreach (CheckBox checkBox in ServicesPanel.Children)
            {
                if (checkBox.IsChecked == true && checkBox.Tag is AdditionalService service)
                {
                    var serviceLink = new SubscriptionServiceLink
                    {
                        SubscriptionId = subscriptionId,
                        ServiceId = service.Id,
                        Price = service.Price,
                        StartDate = startDate,
                        EndDate = startDate.AddMonths(months)
                    };
                    _context.SubscriptionServiceLinks.Add(serviceLink);
                }
            }
            _context.SaveChanges();
        }

        private void UpdateServicesForSubscription(int subscriptionId, DateTime startDate, int months)
        {
            // Удаляем старые услуги
            var oldServices = _context.SubscriptionServiceLinks
                .Where(sl => sl.SubscriptionId == subscriptionId)
                .ToList();
            _context.SubscriptionServiceLinks.RemoveRange(oldServices);

            // Добавляем новые
            AddServicesToSubscription(subscriptionId, startDate, months);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void ClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedClient = ClientComboBox.SelectedItem as Client;
            CalculateTotal();
        }

        private void PublicationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPublication = PublicationComboBox.SelectedItem as Publication;
            CalculateTotal();
        }

        private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotal();
        }
    }
}