using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class PublicationFormPage : Page
    {
        private AppDbContext _context;
        private Publication _publication;
        private bool _isEditMode = false;

        public PublicationFormPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _publication = new Publication();
            LoadData();
            DataContext = _publication;
        }

        public PublicationFormPage(Publication editPublication)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _publication = _context.Publications
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == editPublication.Id) ?? editPublication;
            _isEditMode = true;

            LoadData();
            DataContext = _publication;
            Title = "Редактирование издания";
            SaveButton.Content = "Обновить";
        }

        private void LoadData()
        {
            // Загружаем категории
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            CategoryComboBox.ItemsSource = categories;

            if (_isEditMode && _publication.CategoryId > 0)
            {
                CategoryComboBox.SelectedValue = _publication.CategoryId;
            }
            else if (categories.Any())
            {
                CategoryComboBox.SelectedIndex = 0;
            }

            // Заполняем список периодичности
            if (_isEditMode && !string.IsNullOrEmpty(_publication.Periodicity))
            {
                // Находим нужный ComboBoxItem по содержанию
                foreach (ComboBoxItem item in PeriodicityComboBox.Items)
                {
                    if (item.Content.ToString() == _publication.Periodicity)
                    {
                        PeriodicityComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                PeriodicityComboBox.SelectedIndex = 2; // ежемесячно по умолчанию
            }

            // Устанавливаем даты действия цен
            if (_isEditMode && _publication.PriceValidFrom.HasValue)
            {
                PriceValidFromDatePicker.SelectedDate = _publication.PriceValidFrom;
            }
            else
            {
                PriceValidFromDatePicker.SelectedDate = DateTime.Today;
            }

            if (_isEditMode && _publication.PriceValidTo.HasValue)
            {
                PriceValidToDatePicker.SelectedDate = _publication.PriceValidTo;
            }
            else
            {
                PriceValidToDatePicker.SelectedDate = DateTime.Today.AddYears(1);
            }
        }

        private bool ValidateForm()
        {
            // Проверка названия
            if (string.IsNullOrWhiteSpace(_publication.Title) || _publication.Title.Length < 2)
            {
                MessageBox.Show("Введите название издания (не менее 2 символов)",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return false;
            }

            // Проверка издателя
            if (string.IsNullOrWhiteSpace(_publication.Publisher))
            {
                MessageBox.Show("Введите название издателя",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PublisherTextBox.Focus();
                return false;
            }

            // Проверка категории
            if (CategoryComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return false;
            }

            // Проверка периодичности
            if (PeriodicityComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите периодичность",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PeriodicityComboBox.Focus();
                return false;
            }

            // Проверка цены
            if (_publication.MonthlyPrice <= 0)
            {
                MessageBox.Show("Цена за месяц должна быть больше 0",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                MonthlyPriceTextBox.Focus();
                return false;
            }

            // Проверка ISSN (опционально)
            if (!string.IsNullOrWhiteSpace(_publication.ISSN))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(_publication.ISSN,
                    @"^\d{4}-\d{4}$"))
                {
                    MessageBox.Show("ISSN должен быть в формате XXXX-XXXX (4 цифры, тире, 4 цифры)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ISSNTextBox.Focus();
                    return false;
                }

                // Проверка уникальности ISSN
                var existingPublication = _context.Publications
                    .FirstOrDefault(p => p.Id != _publication.Id &&
                                        p.ISSN == _publication.ISSN);

                if (existingPublication != null)
                {
                    MessageBox.Show("Издание с таким ISSN уже существует:\n" +
                                   $"{existingPublication.Title} (ID: {existingPublication.Id})",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                // Обновляем выбранные значения
                if (CategoryComboBox.SelectedItem is Category selectedCategory)
                {
                    _publication.CategoryId = selectedCategory.Id;
                    _publication.Category = selectedCategory;
                }
                if (PeriodicityComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    _publication.Periodicity = selectedItem.Content.ToString();
                }
                else if (PeriodicityComboBox.SelectedValue != null)
                {
                    _publication.Periodicity = PeriodicityComboBox.SelectedValue.ToString();
                }
                else
                {
                    // Значение по умолчанию
                    _publication.Periodicity = "ежемесячно";
                }

                _publication.PriceValidFrom = PriceValidFromDatePicker.SelectedDate;
                _publication.PriceValidTo = PriceValidToDatePicker.SelectedDate;

                if (!_isEditMode)
                {
                    _publication.CreatedDate = DateTime.Now;
                    _publication.IsAvailable = true;
                }

                if (_isEditMode)
                {
                    _context.Publications.Update(_publication);
                }
                else
                {
                    _context.Publications.Add(_publication);
                }
                        if (string.IsNullOrWhiteSpace(_publication.Description))
        {
            _publication.Description = "Без описания";
        }

                _context.SaveChanges();

                string message = _isEditMode
                    ? "Данные издания обновлены"
                    : "Новое издание добавлено";

                MessageBox.Show(message, "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new PublicationPage());
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

        private void CalculatePrices_Click(object sender, RoutedEventArgs e)
        {
            if (_publication.MonthlyPrice > 0)
            {
                _publication.QuarterlyPrice = _publication.MonthlyPrice * 3 * 0.95m; // 5% скидка
                _publication.YearlyPrice = _publication.MonthlyPrice * 12 * 0.90m; // 10% скидка

                QuarterlyPriceTextBox.Text = _publication.QuarterlyPrice?.ToString("F2") ?? "";
                YearlyPriceTextBox.Text = _publication.YearlyPrice?.ToString("F2") ?? "";

                MessageBox.Show("Цены за квартал и год рассчитаны автоматически с учетом скидок",
                    "Расчет цен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GenerateISSN_Click(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            string issn = $"{random.Next(1000, 9999)}-{random.Next(1000, 9999)}";
            ISSNTextBox.Text = issn;
            _publication.ISSN = issn;
        }
    }
}