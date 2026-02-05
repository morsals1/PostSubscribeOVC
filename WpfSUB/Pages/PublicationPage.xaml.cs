using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class PublicationPage : Page
    {
        private ObservableCollection<Publication> _publications;
        private AppDbContext _context;

        public PublicationPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadPublications();
        }

        private void LoadPublications()
        {
            var publications = _context.Publications
                .Include(p => p.Category)
                .Include(p => p.Subscriptions)
                .OrderBy(p => p.Category.Name)
                .ThenBy(p => p.Title)
                .ToList();

            _publications = new ObservableCollection<Publication>(publications);
            PublicationsListView.ItemsSource = _publications;
            UpdateStatistics();
            UpdateButtonStates();
        }

        private void UpdateStatistics()
        {
            int totalPublications = _publications.Count;
            int availablePublications = _publications.Count(p => p.IsAvailable);
            int totalSubscriptions = _publications.Sum(p => p.Subscriptions?.Count ?? 0);

            StatsTextBlock.Text = $"Всего изданий: {totalPublications} | " +
                                 $"Доступно: {availablePublications} | " +
                                 $"Всего подписок: {totalSubscriptions}";
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = PublicationsListView.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
            ToggleAvailabilityButton.IsEnabled = hasSelection;
        }

        private void PublicationsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            NavigationService.Navigate(new PublicationFormPage());
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (PublicationsListView.SelectedItem is Publication selectedPublication)
            {
                NavigationService.Navigate(new PublicationFormPage(selectedPublication));
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (PublicationsListView.SelectedItem is Publication selectedPublication)
            {
                if (selectedPublication.Subscriptions?.Any() == true)
                {
                    int activeSubscriptions = selectedPublication.Subscriptions
                        .Count(s => s.Status == "активна");

                    MessageBox.Show($"Нельзя удалить издание с подписками!\n" +
                                   $"Всего подписок: {selectedPublication.Subscriptions.Count}\n" +
                                   $"Активных: {activeSubscriptions}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"Удалить издание \"{selectedPublication.Title}\"?\n" +
                                           $"Издатель: {selectedPublication.Publisher}\n" +
                                           $"ISSN: {selectedPublication.ISSN}",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _context.Publications.Remove(selectedPublication);
                    _context.SaveChanges();
                    _publications.Remove(selectedPublication);

                    MessageBox.Show("Издание удалено", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPublications();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                PublicationsListView.ItemsSource = _publications;
            }
            else
            {
                var filtered = _publications.Where(p =>
                    p.Title.ToLower().Contains(searchText) ||
                    p.Publisher.ToLower().Contains(searchText) ||
                    p.ISSN?.ToLower().Contains(searchText) == true ||
                    p.Category?.Name.ToLower().Contains(searchText) == true ||
                    p.Description?.ToLower().Contains(searchText) == true)
                    .ToList();

                PublicationsListView.ItemsSource = new ObservableCollection<Publication>(filtered);
            }
        }

        private void ToggleAvailability_Click(object sender, RoutedEventArgs e)
        {
            if (PublicationsListView.SelectedItem is Publication selectedPublication)
            {
                selectedPublication.IsAvailable = !selectedPublication.IsAvailable;
                _context.SaveChanges();

                string status = selectedPublication.IsAvailable ? "доступно" : "недоступно";
                MessageBox.Show($"Издание \"{selectedPublication.Title}\" теперь {status} для подписки",
                    "Статус изменен", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPublications();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем наличие изданий для экспорта
                if (_publications == null || _publications.Count == 0)
                {
                    MessageBox.Show("Нет изданий для экспорта", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Используем StringBuilder для эффективной конкатенации строк
                System.Text.StringBuilder exportBuilder = new System.Text.StringBuilder();

                exportBuilder.AppendLine("Список изданий системы подписки");
                exportBuilder.AppendLine($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm}");
                exportBuilder.AppendLine("=================================");
                exportBuilder.AppendLine();

                foreach (var publication in _publications)
                {
                    int subscriptionCount = publication.Subscriptions?.Count ?? 0;

                    exportBuilder.AppendLine($"Издание: {publication.Title}");
                    exportBuilder.AppendLine($"Категория: {publication.Category?.Name}");
                    exportBuilder.AppendLine($"Издатель: {publication.Publisher}");
                    exportBuilder.AppendLine($"ISSN: {publication.ISSN}");
                    exportBuilder.AppendLine($"Периодичность: {publication.Periodicity}");
                    exportBuilder.AppendLine($"Цена за месяц: {publication.MonthlyPrice:C}");
                    exportBuilder.AppendLine($"Доступно: {(publication.IsAvailable ? "Да" : "Нет")}");
                    exportBuilder.AppendLine($"Подписок: {subscriptionCount}");
                    exportBuilder.AppendLine("---------------------------------");
                }

                string exportText = exportBuilder.ToString();

                // Используем WPF SaveFileDialog (Microsoft.Win32)
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt",
                    FileName = $"Издания_{DateTime.Now:yyyyMMdd_HHmm}.txt"
                };

                // В WPF ShowDialog() возвращает bool? (true при нажатии OK)
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