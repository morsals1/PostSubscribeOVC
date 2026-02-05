using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;
using System.Text;

namespace WpfSUB.Pages
{
    public partial class CategoryPage : Page
    {
        private ObservableCollection<Category> _categories;
        private AppDbContext _context;

        public CategoryPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadCategories();
        }

        private void LoadCategories()
        {
            var categories = _context.Categories
                .Include(c => c.Publications)
                .OrderBy(c => c.Name)
                .ToList();

            _categories = new ObservableCollection<Category>(categories);
            CategoriesListView.ItemsSource = _categories;
            UpdateStatistics();
            UpdateButtonStates();
        }

        private void UpdateStatistics()
        {
            int totalCategories = _categories.Count;
            int totalPublications = _categories.Sum(c => c.Publications?.Count ?? 0);

            StatsTextBlock.Text = $"Всего категорий: {totalCategories} | " +
                                 $"Всего изданий: {totalPublications}";
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = CategoriesListView.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private void CategoriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            NavigationService.Navigate(new CategoryFormPage());
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesListView.SelectedItem is Category selectedCategory)
            {
                NavigationService.Navigate(new CategoryFormPage(selectedCategory));
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesListView.SelectedItem is Category selectedCategory)
            {
                if (selectedCategory.Publications?.Any() == true)
                {
                    MessageBox.Show($"Нельзя удалить категорию, в которой есть издания!\n" +
                                   $"Всего изданий: {selectedCategory.Publications.Count}\n" +
                                   $"Сначала удалите или переместите издания.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"Удалить категорию \"{selectedCategory.Name}\"?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _context.Categories.Remove(selectedCategory);
                    _context.SaveChanges();
                    _categories.Remove(selectedCategory);

                    MessageBox.Show("Категория удалена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCategories();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                CategoriesListView.ItemsSource = _categories;
            }
            else
            {
                var filtered = _categories.Where(c =>
                    c.Name.ToLower().Contains(searchText))
                    .ToList();

                CategoriesListView.ItemsSource = new ObservableCollection<Category>(filtered);
            }
        }
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем текст для экспорта
                StringBuilder exportBuilder = new StringBuilder();

                exportBuilder.AppendLine("Список категорий системы подписки");
                exportBuilder.AppendLine($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm}");
                exportBuilder.AppendLine("=================================");
                exportBuilder.AppendLine();

                foreach (var category in _categories)
                {
                    int publicationCount = category.Publications?.Count ?? 0;

                    exportBuilder.AppendLine($"Категория: {category.Name}");
                    exportBuilder.AppendLine($"Дата создания: {category.CreatedDate:dd.MM.yyyy}");
                    exportBuilder.AppendLine($"Изданий: {publicationCount}");
                    exportBuilder.AppendLine("---------------------------------");
                }

                string exportText = exportBuilder.ToString();

                // Используем WPF SaveFileDialog
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                    FilterIndex = 1,
                    FileName = $"Категории_{DateTime.Now:yyyyMMdd_HHmm}.txt",
                    DefaultExt = ".txt",
                    Title = "Экспорт категорий",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                // В WPF ShowDialog() возвращает bool?, а не DialogResult
                if (saveDialog.ShowDialog() == true)
                {
                    // Сохраняем файл
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