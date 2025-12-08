using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;
using WpfSUB.Models;
using WpfSUB.Services;

namespace WpfSUB.Pages
{
    public partial class SubscriptionFormPage : Page
    {
        private SubscriptionService _subscriptionService;
        private Subscription _editingSubscription;
        private bool _isEditMode = false;
        private AppDbContext _context;

        public SubscriptionFormPage()
        {
            InitializeComponent();
            _context = new AppDbContext();  // ← СОЗДАЕМ КОНТЕКСТ
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
            }
        }

        private void LoadData()
        {
            try
            {
                // Загружаем клиентов
                var clients = _context.Clients.OrderBy(c => c.FullName).ToList();
                ClientComboBox.ItemsSource = clients;

                // Загружаем издания
                var publications = _context.Publications
                    .Where(p => p.IsAvailable || _isEditMode)
                    .OrderBy(p => p.Title)
                    .ToList();
                PublicationComboBox.ItemsSource = publications;

                // Загружаем услуги
                var services = _context.AdditionalServices
                        .Where(s => s.IsActive || _isEditMode)
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
                        
                        // Устанавливаем дату начала на следующий месяц
                        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(1);
                    }
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
                }
                else
                {
                    TotalPriceText.Text = "0,00 ₽";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

                if (_isEditMode && _editingSubscription != null)
                {
                    // Обновляем существующую подписку через сервис
                    UpdateSubscription(client, publication, months, startDate);
                }
                else
                {
                    // Создаем новую подписку через сервис
                    CreateNewSubscription(client, publication, months, startDate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateNewSubscription(Client client, Publication publication, int months, DateTime startDate)
        {
            // 1. Рассчитываем ОБЩУЮ сумму (публикация + услуги)
            decimal totalPrice = CalculateTotalPrice(); // ← новый метод

            // 2. Создаем подписку с ПРАВИЛЬНОЙ суммой
            var subscription = _subscriptionService.CreateSubscription(
                client.Id,
                publication.Id,
                months,
                startDate,
                totalPrice); // ← передаем общую сумму

            // 3. Добавляем услуги
            AddServicesToSubscription(subscription.Id, startDate, months);

            MessageBox.Show($"Подписка оформлена успешно!\n" +
                           $"ID: {subscription.Id}\n" +
                           $"Сумма: {subscription.TotalPrice:C}\n" +
                           $"Из них услуги: {CalculateServicesTotal():C}",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            NavigationService.Navigate(new SubscriptionPage());
        }

        // Добавьте эти вспомогательные методы в класс:
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

        private decimal CalculateServicesTotal()
        {
            decimal total = 0;
            foreach (CheckBox checkBox in ServicesPanel.Children)
            {
                if (checkBox.IsChecked == true && checkBox.Tag is AdditionalService service)
                {
                    total += service.Price;
                }
            }
            return total;
        }

        private void UpdateSubscription(Client client, Publication publication, int months, DateTime startDate)
        {
            // Получаем полные данные подписки из базы через сервис
            var fullSubscription = _subscriptionService.Subscriptions
                .FirstOrDefault(s => s.Id == _editingSubscription.Id);

            if (fullSubscription == null)
                throw new Exception("Подписка не найдена в базе данных");

            // Рассчитываем ОБЩУЮ сумму (публикация + услуги)
            decimal totalPrice = CalculateTotalPrice(); // ← используем тот же метод

            // Обновляем поля
            fullSubscription.ClientId = client.Id;
            fullSubscription.PublicationId = publication.Id;
            fullSubscription.PeriodMonths = months;
            fullSubscription.MonthlyPrice = publication.MonthlyPrice;
            fullSubscription.TotalPrice = totalPrice; // ← устанавливаем ОБЩУЮ сумму
            fullSubscription.PlannedStartDate = startDate;

            // Пересчитываем даты
            fullSubscription.CalculateDates();

            // Сохраняем статус и другие поля
            fullSubscription.Status = _editingSubscription.Status;
            fullSubscription.IsFullyPaid = _editingSubscription.IsFullyPaid;
            fullSubscription.PaidDate = _editingSubscription.PaidDate;
            fullSubscription.ActualStartDate = _editingSubscription.ActualStartDate;
            fullSubscription.ActualEndDate = _editingSubscription.ActualEndDate;
            fullSubscription.PaymentDeadline = _editingSubscription.PaymentDeadline;

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
            // Теперь используем общий контекст _context
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

        private void PublicationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotal();
        }
    }
}