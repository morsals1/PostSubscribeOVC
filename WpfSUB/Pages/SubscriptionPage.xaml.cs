using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using WpfSUB.Models;
using WpfSUB.Services;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class SubscriptionPage : Page
    {
        private SubscriptionService _subscriptionService;
        private ObservableCollection<Subscription> _subscriptions;

        public SubscriptionPage()
        {
            InitializeComponent();
            _subscriptionService = new SubscriptionService();
            LoadSubscriptions();
        }

        private void LoadSubscriptions()
        {
            // Загружаем подписки с включением связей
            using (var context = new AppDbContext())
            {
                var subscriptions = context.Subscriptions
                    .Include(s => s.Client)
                    .Include(s => s.Publication)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToList();

                _subscriptions = new ObservableCollection<Subscription>(subscriptions);
                SubscriptionsListView.ItemsSource = _subscriptions;
            }

            UpdateButtonStates();
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
            // Открываем форму оформления подписки
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
                var result = MessageBox.Show($"Удалить подписку ID: {selectedSubscription.Id}?\n" +
                                          $"Клиент: {selectedSubscription.Client?.FullName}\n" +
                                          $"Издание: {selectedSubscription.Publication?.Title}",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new AppDbContext())
                        {
                            var subscription = context.Subscriptions.Find(selectedSubscription.Id);
                            if (subscription != null)
                            {
                                context.Subscriptions.Remove(subscription);
                                context.SaveChanges();
                                _subscriptions.Remove(selectedSubscription);

                                MessageBox.Show("Подписка удалена", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (System.Exception ex)
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
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            // Можно добавить детальный просмотр подписки
            if (SubscriptionsListView.SelectedItem is Subscription selectedSubscription)
            {
                string details = $"Детали подписки:\n" +
                               $"ID: {selectedSubscription.Id}\n" +
                               $"Клиент: {selectedSubscription.Client?.FullName}\n" +
                               $"Издание: {selectedSubscription.Publication?.Title}\n" +
                               $"Период: {selectedSubscription.PeriodMonths} мес.\n" +
                               $"Сумма: {selectedSubscription.TotalPrice:C}\n" +
                               $"Статус: {selectedSubscription.Status}\n" +
                               $"Оформлена: {selectedSubscription.CreatedDate:dd.MM.yyyy}\n" +
                               $"Оплачена: {(selectedSubscription.IsFullyPaid ? "Да" : "Нет")}";

                MessageBox.Show(details, "Детали подписки",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}