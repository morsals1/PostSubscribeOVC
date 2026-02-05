using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class CategoryFormPage : Page
    {
        private AppDbContext _context;
        private Category _category;
        private bool _isEditMode = false;

        public CategoryFormPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _category = new Category();
            DataContext = _category;
        }

        public CategoryFormPage(Category editCategory)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _category = _context.Categories
                .FirstOrDefault(c => c.Id == editCategory.Id) ?? editCategory;
            _isEditMode = true;

            DataContext = _category;
            Title = "Редактирование категории";
            SaveButton.Content = "Обновить";
        }

        private bool ValidateForm()
        {
            // Проверка названия
            if (string.IsNullOrWhiteSpace(_category.Name) || _category.Name.Length < 2)
            {
                MessageBox.Show("Введите название категории (не менее 2 символов)",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            // Проверка уникальности
            var existingCategory = _context.Categories
                .FirstOrDefault(c => c.Id != _category.Id &&
                                    c.Name.ToLower() == _category.Name.ToLower());

            if (existingCategory != null)
            {
                MessageBox.Show("Категория с таким названием уже существует:\n" +
                               $"ID: {existingCategory.Id}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                if (_isEditMode)
                {
                    _context.Categories.Update(_category);
                }
                else
                {
                    _category.CreatedDate = DateTime.Now;
                    _context.Categories.Add(_category);
                }

                _context.SaveChanges();

                string message = _isEditMode
                    ? "Категория обновлена"
                    : "Новая категория добавлена";

                MessageBox.Show(message, "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new CategoryPage());
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

        private void GenerateName_Click(object sender, RoutedEventArgs e)
        {
            string[] sampleNames =
            {
                "Газеты", "Журналы", "Научные издания", "Технические издания",
                "Детские издания", "Спортивные издания", "Деловые издания",
                "Культурные издания", "Образовательные издания", "Развлекательные издания"
            };

            var random = new Random();
            NameTextBox.Text = sampleNames[random.Next(sampleNames.Length)];
        }
    }
}