using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using WpfSUB.Models;
using WpfSUB.Services;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace WpfSUB.Pages
{
    public partial class CategoryPage : Page
    {
        private ObservableCollection<Category> _categories;

        public CategoryPage()
        {
            InitializeComponent();
            LoadCategories();
        }

        private void LoadCategories()
        {
            using (var context = new Data.AppDbContext())
            {
                var categories = context.Categories
                    .Include(c => c.Publications)
                    .OrderBy(c => c.Name)
                    .ToList();

                _categories = new ObservableCollection<Category>(categories);
                CategoriesListView.ItemsSource = _categories;
                UpdateButtonStates();
            }
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
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Добавление категории - будет реализовано");
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesListView.SelectedItem is Category selectedCategory)
            {
                MessageBox.Show($"Редактирование категории: {selectedCategory.Name} - будет реализовано");
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesListView.SelectedItem is Category selectedCategory)
            {
                if (selectedCategory.Publications?.Count > 0)
                {
                    MessageBox.Show("Нельзя удалить категорию, в которой есть издания",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"Удалить категорию \"{selectedCategory.Name}\"?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new Data.AppDbContext())
                    {
                        context.Categories.Remove(selectedCategory);
                        context.SaveChanges();
                        _categories.Remove(selectedCategory);
                    }
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCategories();
        }
    }
}