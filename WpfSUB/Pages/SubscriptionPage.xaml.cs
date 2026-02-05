using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class SubscriptionPage : Page
    {
        private ObservableCollection<Subscription> _subscriptions;
        private ObservableCollection<Subscription> _allSubscriptions; // Все подписки без фильтрации
        private AppDbContext _context;
        private string _currentSearchText = "";
        private string _currentStatusFilter = "";

        public SubscriptionPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadSubscriptions();
            StatusFilterComboBox.SelectedIndex = 0;
            SearchTextBox.Focus();
        }

        private void LoadSubscriptions()
        {
            var subscriptions = _context.Subscriptions
                .Include(s => s.Client)
                .Include(s => s.Publication)
                .ThenInclude(p => p.Category)
                .Include(s => s.Payments)
                .Include(s => s.Deliveries)
                .OrderByDescending(s => s.CreatedDate)
                .ToList();

            _allSubscriptions = new ObservableCollection<Subscription>(subscriptions);
            _subscriptions = new ObservableCollection<Subscription>(subscriptions);
            SubscriptionsListView.ItemsSource = _subscriptions;
            UpdateStatistics();
            UpdateButtonStates();
            UpdateSearchResults();
        }

        private void ApplyFilters()
        {
            if (_allSubscriptions == null) return;

            var filtered = _allSubscriptions.AsEnumerable();

            // Применяем фильтр по статусу
            if (!string.IsNullOrEmpty(_currentStatusFilter))
            {
                filtered = filtered.Where(s => s.Status == _currentStatusFilter);
            }

            // Применяем поиск по ФИО
            if (!string.IsNullOrWhiteSpace(_currentSearchText))
            {
                string searchText = _currentSearchText.ToLower();
                filtered = filtered.Where(s =>
                    s.Client != null &&
                    s.Client.FullName != null &&
                    s.Client.FullName.ToLower().Contains(searchText));
            }

            _subscriptions.Clear();
            foreach (var subscription in filtered)
            {
                _subscriptions.Add(subscription);
            }

            SubscriptionsListView.ItemsSource = _subscriptions;
            UpdateStatistics();
            UpdateSearchResults();
        }

        private void UpdateSearchResults()
        {
            if (string.IsNullOrWhiteSpace(_currentSearchText))
            {
                SearchResultsBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                SearchResultsBorder.Visibility = Visibility.Visible;
                int count = _subscriptions.Count;
                SearchResultsText.Text = $"Найдено подписок: {count}";
            }
        }

        private void UpdateStatistics()
        {
            int totalSubscriptions = _subscriptions.Count;
            int waitingPayment = _subscriptions.Count(s => s.Status == "ожидает_оплаты");
            int paid = _subscriptions.Count(s => s.Status == "оплачена");
            int active = _subscriptions.Count(s => s.Status == "активна");
            int completed = _subscriptions.Count(s => s.Status == "завершена");
            decimal totalRevenue = _subscriptions.Where(s => s.IsFullyPaid).Sum(s => s.TotalPrice);
            decimal pendingRevenue = _subscriptions.Where(s => s.Status == "ожидает_оплаты").Sum(s => s.TotalPrice);

            StatsTextBlock.Text = $"Всего подписок: {totalSubscriptions} | " +
                                 $"Ожидают оплаты: {waitingPayment} | " +
                                 $"Оплачены: {paid} | " +
                                 $"Активные: {active} | " +
                                 $"Завершены: {completed}\n" +
                                 $"Выручка: {totalRevenue:C} | " +
                                 $"Ожидает оплаты: {pendingRevenue:C}";
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = SubscriptionsListView.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private void SubscriptionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            NavigationService.Navigate(new SubscriptionFormPage());
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (SubscriptionsListView.SelectedItem is Subscription selectedSubscription)
            {
                NavigationService.Navigate(new SubscriptionFormPage(selectedSubscription));
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (SubscriptionsListView.SelectedItem is Subscription selectedSubscription)
            {
                if (selectedSubscription.IsFullyPaid && selectedSubscription.Status == "активна")
                {
                    MessageBox.Show("Нельзя удалить активную оплаченную подписку!\n" +
                                   "Сначала завершите подписку или выполните возврат платежа.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"Удалить подписку ID: {selectedSubscription.Id}?\n" +
                                           $"Клиент: {selectedSubscription.Client?.FullName}\n" +
                                           $"Издание: {selectedSubscription.Publication?.Title}",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var subscription = _context.Subscriptions
                            .Include(s => s.Payments)
                            .Include(s => s.Deliveries)
                            .FirstOrDefault(s => s.Id == selectedSubscription.Id);

                        if (subscription != null)
                        {
                            _context.Payments.RemoveRange(subscription.Payments);
                            _context.Deliveries.RemoveRange(subscription.Deliveries);
                            _context.Subscriptions.Remove(subscription);
                            _context.SaveChanges();

                            // Удаляем из обеих коллекций
                            _allSubscriptions.Remove(selectedSubscription);
                            _subscriptions.Remove(selectedSubscription);

                            MessageBox.Show("Подписка удалена", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSubscriptions();
            SearchTextBox.Text = "";
            _currentSearchText = "";
            _currentStatusFilter = "";
            StatusFilterComboBox.SelectedIndex = 0;
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (SubscriptionsListView.SelectedItem is Subscription selectedSubscription)
            {
                var fullSubscription = _context.Subscriptions
                    .Include(s => s.Client)
                    .Include(s => s.Publication)
                    .ThenInclude(p => p.Category)
                    .Include(s => s.Payments)
                    .Include(s => s.Deliveries)
                    .FirstOrDefault(s => s.Id == selectedSubscription.Id);

                if (fullSubscription == null) return;

                string details = $"Детали подписки:\n\n" +
                               $"ID: {fullSubscription.Id}\n" +
                               $"Клиент: {fullSubscription.Client?.FullName}\n" +
                               $"Издание: {fullSubscription.Publication?.Title}\n" +
                               $"Категория: {fullSubscription.Publication?.Category?.Name}\n" +
                               $"Период: {fullSubscription.PeriodMonths} месяцев\n" +
                               $"Цена за месяц: {fullSubscription.MonthlyPrice:C}\n" +
                               $"Общая стоимость: {fullSubscription.TotalPrice:C}\n" +
                               $"Статус: {fullSubscription.Status}\n" +
                               $"Оплачена: {(fullSubscription.IsFullyPaid ? "Да" : "Нет")}\n" +
                               $"Дата оформления: {fullSubscription.CreatedDate:dd.MM.yyyy}\n" +
                               $"Плановое начало: {fullSubscription.PlannedStartDate:dd.MM.yyyy}\n" +
                               $"Плановое окончание: {fullSubscription.PlannedEndDate:dd.MM.yyyy}\n";

                if (fullSubscription.ActualStartDate.HasValue)
                {
                    details += $"Фактическое начало: {fullSubscription.ActualStartDate:dd.MM.yyyy}\n";
                }

                if (fullSubscription.ActualEndDate.HasValue)
                {
                    details += $"Фактическое окончание: {fullSubscription.ActualEndDate:dd.MM.yyyy}\n";
                }

                if (fullSubscription.PaidDate.HasValue)
                {
                    details += $"Дата оплаты: {fullSubscription.PaidDate:dd.MM.yyyy}\n";
                }

                details += $"\nПлатежи: {fullSubscription.Payments?.Count ?? 0}\n";
                details += $"Доставки: {fullSubscription.Deliveries?.Count ?? 0}";

                MessageBox.Show(details, "Детали подписки",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentStatusFilter = selectedItem.Tag as string;
                ApplyFilters();
            }
        }

        // Обработка поиска при вводе
        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || string.IsNullOrEmpty(SearchTextBox.Text))
            {
                _currentSearchText = SearchTextBox.Text.Trim();
                ApplyFilters();
            }
        }

        // Кнопка очистки поиска
        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            _currentSearchText = "";
            ApplyFilters();
        }

        // Поиск при изменении текста (альтернативный вариант)
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearchText = SearchTextBox.Text.Trim();
            ApplyFilters();
        }
    }
}