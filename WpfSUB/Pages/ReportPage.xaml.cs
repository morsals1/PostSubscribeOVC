using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;
using System.Collections.Generic;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;

namespace WpfSUB.Pages
{
    public partial class ReportPage : Page
    {
        private AppDbContext _context;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private string _currentReportType;
        private List<object> _reportData;
        private ComboBox _categoryFilterComboBox;
        private ComboBox _publicationFilterComboBox;
        private ComboBox _statusFilterComboBox;
        private ComboBox _paymentMethodFilterComboBox;
        private ComboBox _paymentStatusFilterComboBox;
        private ComboBox _regionFilterComboBox;
        private DatePicker _monthPicker;
        private DatePicker _customStartDatePicker;
        private DatePicker _customEndDatePicker;
        private GridViewColumnHeader _lastHeaderClicked;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private StackPanel _datePickerPanel;
        private Label _startDateLabel;
        private Label _endDateLabel;

        public ReportPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            ReportTypeComboBox.SelectedIndex = 0;
            PeriodComboBox.SelectedIndex = 2;
            GroupByComboBox.SelectedIndex = 6; // "Нет" по умолчанию
            SortByComboBox.SelectedIndex = 0;
            SortByComboBox.Visibility = Visibility.Collapsed;
            UpdatePeriod();
            UpdateReportTitle();
            UpdateAdditionalFilters();
            UpdateSortingVisibility();
            UpdatePeroidVisibility();
        }

        private void UpdatePeriod()
        {
            var today = DateTime.Today;

            if (PeriodComboBox.SelectedItem is ComboBoxItem selectedPeriod)
            {
                string period = selectedPeriod.Content.ToString();

                switch (period)
                {
                    case "Сегодня":
                        _startDate = today;
                        _endDate = today;
                        break;
                    case "Вчера":
                        _startDate = today.AddDays(-1);
                        _endDate = today.AddDays(-1);
                        break;
                    case "Текущая неделя":
                        var diff = today.DayOfWeek - DayOfWeek.Monday;
                        if (diff < 0) diff += 7;
                        _startDate = today.AddDays(-diff);
                        _endDate = _startDate.Value.AddDays(6);
                        break;
                    case "Текущий месяц":
                        _startDate = new DateTime(today.Year, today.Month, 1);
                        _endDate = _startDate.Value.AddMonths(1).AddDays(-1);
                        break;
                    case "Прошлый месяц":
                        _startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                        _endDate = _startDate.Value.AddMonths(1).AddDays(-1);
                        break;
                    case "Произвольный период":
                        // Обновим фильтры чтобы добавить DatePicker
                        UpdateAdditionalFilters();
                        UpdateReportInfo();
                        return;
                }
            }

            UpdateAdditionalFilters(); // Обновляем фильтры чтобы скрыть DatePicker
            UpdateReportInfo();
        }

        private void UpdateReportInfo()
        {
            if (_startDate.HasValue && _endDate.HasValue)
            {
                ReportInfoText.Text = $"Период: {_startDate.Value:dd.MM.yyyy} - {_endDate.Value:dd.MM.yyyy}";
            }
            else
            {
                ReportInfoText.Text = "Выберите период";
            }
        }

        private void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAdditionalFilters();
            UpdateReportTitle();
            UpdateSortingVisibility();
            UpdatePeroidVisibility();
        }

        private void UpdateSortingVisibility()
        {
            // Показываем сортировку только для отчетов, где она имеет смысл
            if (ReportTypeComboBox.SelectedIndex == 1 || // Активные подписки
                ReportTypeComboBox.SelectedIndex == 2 || // Подписки к оплате
                ReportTypeComboBox.SelectedIndex == 5 || // Статистика клиентов
                ReportTypeComboBox.SelectedIndex == 7 || // Подписки по времени
                ReportTypeComboBox.SelectedIndex == 8 || // Платежи по времени
                ReportTypeComboBox.SelectedIndex == 9)   // Клиенты по регионам
            {
                SortByComboBox.Visibility = Visibility.Visible;
                SortByLabel.Visibility = Visibility.Visible; // Скрываем лейбл
            }
            else
            {
                SortByComboBox.Visibility = Visibility.Collapsed;
                SortByLabel.Visibility = Visibility.Collapsed; // Скрываем лейбл
            }
        }

        private void UpdatePeroidVisibility()
        {
            if (
                ReportTypeComboBox.SelectedIndex == 1 || // Активные подписки
                ReportTypeComboBox.SelectedIndex == 2 || // Подписки к оплате
                ReportTypeComboBox.SelectedIndex == 3 ||
                ReportTypeComboBox.SelectedIndex == 4 ||
                ReportTypeComboBox.SelectedIndex == 7 || // Подписки по времени
                ReportTypeComboBox.SelectedIndex == 8)
            {
                PeriodComboBox.Visibility = Visibility.Visible;
                PeriodByLabel.Visibility = Visibility.Visible;
            }
            else
            {
                PeriodComboBox.Visibility = Visibility.Collapsed;
                PeriodByLabel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateAdditionalFilters()
        {
            AdditionalFiltersPanel.Children.Clear();

            // Сбрасываем только контролы фильтров
            _categoryFilterComboBox = null;
            _publicationFilterComboBox = null;
            _statusFilterComboBox = null;
            _paymentMethodFilterComboBox = null;
            _paymentStatusFilterComboBox = null;
            _regionFilterComboBox = null;
            _monthPicker = null;

            if (ReportTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentReportType = selectedItem.Content.ToString();

                // Добавляем фильтры в зависимости от типа отчета
                if (ReportTypeComboBox.SelectedIndex == 0 || ReportTypeComboBox.SelectedIndex == 1 ||
                    ReportTypeComboBox.SelectedIndex == 2 || ReportTypeComboBox.SelectedIndex == 6)
                {
                    AddCategoryFilter();
                }
                if (ReportTypeComboBox.SelectedIndex == 1 || ReportTypeComboBox.SelectedIndex == 2)
                {
                    AddPublicationFilter();
                }
                if (ReportTypeComboBox.SelectedIndex == 3)
                {
                    AddMonthPickerFilter();
                }
                if (ReportTypeComboBox.SelectedIndex == 4)
                {
                    AddPaymentMethodFilter();
                    AddPaymentStatusFilter();
                }
                if (ReportTypeComboBox.SelectedIndex == 7 || ReportTypeComboBox.SelectedIndex == 8)
                {
                    AddDateRangeControl();
                }
                if (ReportTypeComboBox.SelectedIndex == 9)
                {
                    AddRegionFilter();
                }
            }

            // Если выбран произвольный период, добавляем DatePicker ПОСЛЕ основных фильтров
            if (PeriodComboBox.SelectedIndex == 5)
            {
                AddDatePickerControl();
            }
        }

        // Новый метод для добавления DatePicker
        private void AddDatePickerControl()
        {
            if (_datePickerPanel == null)
            {
                _datePickerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 0) // Отступ сверху чтобы отделить от других фильтров
                };

                _startDateLabel = new Label
                {
                    Content = "С:",
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                _customStartDatePicker = new DatePicker
                {
                    Width = 120,
                    Margin = new Thickness(0, 0, 20, 0),
                    SelectedDate = DateTime.Today.AddMonths(-1)
                };
                _customStartDatePicker.SelectedDateChanged += (s, e) =>
                {
                    _startDate = _customStartDatePicker.SelectedDate;
                    UpdateReportInfo();
                };

                _endDateLabel = new Label
                {
                    Content = "По:",
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                _customEndDatePicker = new DatePicker
                {
                    Width = 120,
                    SelectedDate = DateTime.Today
                };
                _customEndDatePicker.SelectedDateChanged += (s, e) =>
                {
                    _endDate = _customEndDatePicker.SelectedDate;
                    UpdateReportInfo();
                };

                _datePickerPanel.Children.Add(_startDateLabel);
                _datePickerPanel.Children.Add(_customStartDatePicker);
                _datePickerPanel.Children.Add(_endDateLabel);
                _datePickerPanel.Children.Add(_customEndDatePicker);
            }

            // Добавляем DatePicker в конец панели
            AdditionalFiltersPanel.Children.Add(_datePickerPanel);

            _startDate = _customStartDatePicker.SelectedDate;
            _endDate = _customEndDatePicker.SelectedDate;
            UpdateReportInfo();
        }

        private void AddCategoryFilter()
        {
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            var categoryList = new List<Category>();
            categoryList.Add(new Category { Id = 0, Name = "Все категории" });
            categoryList.AddRange(categories);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var label = new Label
            {
                Content = "Категория:",
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _categoryFilterComboBox = new ComboBox
            {
                Width = 200,
                DisplayMemberPath = "Name",
                ItemsSource = categoryList,
                SelectedIndex = 0
            };

            panel.Children.Add(label);
            panel.Children.Add(_categoryFilterComboBox);
            AdditionalFiltersPanel.Children.Add(panel);
        }

        private void AddPublicationFilter()
        {
            var publications = _context.Publications.Where(p => p.IsAvailable).OrderBy(p => p.Title).ToList();
            var publicationList = new List<Publication>();
            publicationList.Add(new Publication { Id = 0, Title = "Все издания" });
            publicationList.AddRange(publications);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var label = new Label
            {
                Content = "Издание:",
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _publicationFilterComboBox = new ComboBox
            {
                Width = 250,
                DisplayMemberPath = "Title",
                ItemsSource = publicationList,
                SelectedIndex = 0
            };

            panel.Children.Add(label);
            panel.Children.Add(_publicationFilterComboBox);
            AdditionalFiltersPanel.Children.Add(panel);
        }

        private void AddPaymentMethodFilter()
        {
            var methods = new[] { "Все", "наличные", "карта_отделение", "карта_онлайн", "банковский_перевод" };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var label = new Label
            {
                Content = "Способ оплаты:",
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _paymentMethodFilterComboBox = new ComboBox
            {
                Width = 150,
                ItemsSource = methods,
                SelectedIndex = 0
            };

            panel.Children.Add(label);
            panel.Children.Add(_paymentMethodFilterComboBox);
            AdditionalFiltersPanel.Children.Add(panel);
        }

        private void AddPaymentStatusFilter()
        {
            var statuses = new[] { "Все", "ожидает_подтверждения", "подтвержден", "отклонен" };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var label = new Label
            {
                Content = "Статус платежа:",
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _paymentStatusFilterComboBox = new ComboBox
            {
                Width = 150,
                ItemsSource = statuses,
                SelectedIndex = 0
            };

            panel.Children.Add(label);
            panel.Children.Add(_paymentStatusFilterComboBox);
            AdditionalFiltersPanel.Children.Add(panel);
        }

        private void AddMonthPickerFilter()
        {
            _monthPicker = new DatePicker
            {
                Width = 150,
                SelectedDate = GetNextMonthForPrintOrder(),
                VerticalAlignment = VerticalAlignment.Center
            };
            _monthPicker.SelectedDateChanged += (s, e) =>
            {
                if (_monthPicker.SelectedDate.HasValue)
                {
                    _startDate = new DateTime(_monthPicker.SelectedDate.Value.Year, _monthPicker.SelectedDate.Value.Month, 1);
                    _endDate = _startDate.Value.AddMonths(1).AddDays(-1);
                    UpdateReportInfo();
                }
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var label = new Label
            {
                Content = "Месяц поставки:",
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(label);
            panel.Children.Add(_monthPicker);
            AdditionalFiltersPanel.Children.Add(panel);

            if (_monthPicker.SelectedDate.HasValue)
            {
                _startDate = new DateTime(_monthPicker.SelectedDate.Value.Year, _monthPicker.SelectedDate.Value.Month, 1);
                _endDate = _startDate.Value.AddMonths(1).AddDays(-1);
                UpdateReportInfo();
            }
        }

        private void AddRegionFilter()
        {
            var clients = _context.Clients.ToList();
            var regions = clients
                .Select(c =>
                {
                    if (string.IsNullOrEmpty(c.Address)) return null;
                    var parts = c.Address.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length > 0 ? parts[0].Trim() : null;
                })
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .OrderBy(a => a)
                .ToList();
            var regionList = new List<string> { "Все регионы" };
            regionList.AddRange(regions);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var label = new Label
            {
                Content = "Регион:",
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _regionFilterComboBox = new ComboBox
            {
                Width = 200,
                ItemsSource = regionList,
                SelectedIndex = 0
            };

            panel.Children.Add(label);
            panel.Children.Add(_regionFilterComboBox);
            AdditionalFiltersPanel.Children.Add(panel);
        }

        private void AddDateRangeControl()
        {
            if (!_startDate.HasValue) _startDate = DateTime.Today.AddMonths(-1);
            if (!_endDate.HasValue) _endDate = DateTime.Today;

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var startPicker = new DatePicker
            {
                Width = 120,
                Margin = new Thickness(10, 0, 5, 0),
                SelectedDate = _startDate
            };
            startPicker.SelectedDateChanged += (s, e) =>
            {
                _startDate = startPicker.SelectedDate;
                UpdateReportInfo();
            };

            var endPicker = new DatePicker
            {
                Width = 120,
                Margin = new Thickness(5, 0, 0, 0),
                SelectedDate = _endDate
            };
            endPicker.SelectedDateChanged += (s, e) =>
            {
                _endDate = endPicker.SelectedDate;
                UpdateReportInfo();
            };

            panel.Children.Add(new Label { Content = "С:", VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(startPicker);
            panel.Children.Add(new Label { Content = "По:", Margin = new Thickness(10, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(endPicker);

            AdditionalFiltersPanel.Children.Add(panel);
        }

        private DateTime GetNextMonthForPrintOrder()
        {
            var today = DateTime.Today;
            if (today.Day > 15) return today.AddMonths(2);
            else return today.AddMonths(1);
        }

        private void UpdateReportTitle()
        {
            if (ReportTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                ReportTitleText.Text = selectedItem.Content.ToString();
            }
        }

        private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePeriod();
            UpdateAdditionalFilters();
        }

        private void GroupByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработка изменения группировки
        }

        private void SortByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySorting();
        }

        private void ApplySorting()
        {
            if (_reportData == null || _reportData.Count == 0 || ReportDataGrid.ItemsSource == null)
                return;

            var dataView = CollectionViewSource.GetDefaultView(ReportDataGrid.ItemsSource);
            if (dataView == null) return;

            dataView.SortDescriptions.Clear();

            if (SortByComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string sortOption = selectedItem.Content.ToString();

                switch (sortOption)
                {
                    case "По дате (убыв.)":
                        AddSortDescription(dataView, "Дата", ListSortDirection.Descending);
                        AddSortDescription(dataView, "Дата_оформления", ListSortDirection.Descending);
                        AddSortDescription(dataView, "Дата_регистрации", ListSortDirection.Descending);
                        break;
                    case "По дате (возр.)":
                        AddSortDescription(dataView, "Дата", ListSortDirection.Ascending);
                        AddSortDescription(dataView, "Дата_оформления", ListSortDirection.Ascending);
                        AddSortDescription(dataView, "Дата_регистрации", ListSortDirection.Ascending);
                        break;
                    case "По сумме (убыв.)":
                        AddSortDescription(dataView, "Сумма", ListSortDirection.Descending);
                        AddSortDescription(dataView, "Общая_сумма", ListSortDirection.Descending);
                        AddSortDescription(dataView, "TotalPrice", ListSortDirection.Descending);
                        break;
                    case "По сумме (возр.)":
                        AddSortDescription(dataView, "Сумма", ListSortDirection.Ascending);
                        AddSortDescription(dataView, "Общая_сумма", ListSortDirection.Ascending);
                        AddSortDescription(dataView, "TotalPrice", ListSortDirection.Ascending);
                        break;
                    case "По количеству (убыв.)":
                        AddSortDescription(dataView, "Количество", ListSortDirection.Descending);
                        AddSortDescription(dataView, "Всего_подписок", ListSortDirection.Descending);
                        AddSortDescription(dataView, "Подписок_всего", ListSortDirection.Descending);
                        break;
                }
            }

            dataView.Refresh();
        }

        private void AddSortDescription(ICollectionView dataView, string propertyName, ListSortDirection direction)
        {
            if (PropertyExists(_reportData[0], propertyName))
            {
                dataView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private bool PropertyExists(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return false;

            var type = obj.GetType();
            return type.GetProperty(propertyName) != null;
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidatePeriod())
                {
                    MessageBox.Show("Выберите корректный период", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                switch (ReportTypeComboBox.SelectedIndex)
                {
                    case 0: GeneratePriceListReport(); break;
                    case 1: GenerateActiveSubscriptionsReport(); break;
                    case 2: GeneratePendingSubscriptionsReport(); break;
                    case 3: GeneratePrintOrderReport(); break;
                    case 4: GenerateReceiptsReport(); break;
                    case 5: GenerateClientsStatisticsReport(); break;
                    case 6: GeneratePublicationsStatisticsReport(); break;
                    case 7: GenerateSubscriptionsByTimeReport(); break;
                    case 8: GeneratePaymentsByTimeReport(); break;
                    case 9: GenerateClientsByRegionReport(); break;
                }

                ApplySorting();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidatePeriod()
        {
            // Для некоторых отчетов период не обязателен
            if (ReportTypeComboBox.SelectedIndex == 0 || // Прайс-лист
                ReportTypeComboBox.SelectedIndex == 3 || // Заказ в типографию
                ReportTypeComboBox.SelectedIndex == 5 || // Статистика клиентов
                ReportTypeComboBox.SelectedIndex == 6 || // Статистика изданий
                ReportTypeComboBox.SelectedIndex == 9)   // Клиенты по регионам
            {
                return true;
            }

            return _startDate.HasValue && _endDate.HasValue && _startDate <= _endDate;
        }

        private void GeneratePriceListReport()
        {
            int? selectedCategoryId = null;
            if (_categoryFilterComboBox != null && _categoryFilterComboBox.SelectedItem is Category selectedCategory && selectedCategory.Id != 0)
            {
                selectedCategoryId = selectedCategory.Id;
            }

            var query = _context.Publications
                .Include(p => p.Category)
                .Where(p => p.IsAvailable);

            if (selectedCategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == selectedCategoryId.Value);
            }

            var publications = query
                .OrderBy(p => p.Category.Name)
                .ThenBy(p => p.Title)
                .Select(p => new
                {
                    Категория = p.Category.Name,
                    Название = p.Title,
                    Издатель = p.Publisher,
                    ISSN = p.ISSN,
                    Периодичность = p.Periodicity,
                    Цена_мес = p.MonthlyPrice,
                    Цена_квартал = p.QuarterlyPrice,
                    Цена_год = p.YearlyPrice,
                    Доступно = p.IsAvailable ? "Да" : "Нет"
                })
                .ToList();

            _reportData = publications.Cast<object>().ToList();
            ReportDataGrid.ItemsSource = publications;
            ReportInfoText.Text = $"Прайс-лист. Всего изданий: {publications.Count}";
        }

        private void GenerateActiveSubscriptionsReport()
        {
            var today = DateTime.Today;

            // Получаем выбранные фильтры
            int? selectedCategoryId = null;
            if (_categoryFilterComboBox != null && _categoryFilterComboBox.SelectedItem is Category selectedCategory && selectedCategory.Id != 0)
            {
                selectedCategoryId = selectedCategory.Id;
            }

            int? selectedPublicationId = null;
            if (_publicationFilterComboBox != null && _publicationFilterComboBox.SelectedItem is Publication selectedPublication && selectedPublication.Id != 0)
            {
                selectedPublicationId = selectedPublication.Id;
            }

            string selectedStatus = null;
            if (_statusFilterComboBox != null && _statusFilterComboBox.SelectedItem is string status && status != "Все")
            {
                selectedStatus = status;
            }

            var query = _context.Subscriptions
                .Include(s => s.Client)
                .Include(s => s.Publication)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // Если выбран конкретный статус, фильтруем по нему
            if (!string.IsNullOrEmpty(selectedStatus))
            {
                query = query.Where(s => s.Status == selectedStatus);

                // Для активных подписок добавляем проверку по датам
                if (selectedStatus == "активна")
                {
                    query = query.Where(s => s.ActualStartDate <= today && s.ActualEndDate >= today);
                }
            }
            else
            {
                // По умолчанию показываем все статусы кроме "ожидает_оплаты"
                query = query.Where(s => s.Status != "ожидает_оплаты");
            }

            if (selectedPublicationId.HasValue)
            {
                query = query.Where(s => s.PublicationId == selectedPublicationId.Value);
            }

            if (selectedCategoryId.HasValue)
            {
                query = query.Where(s => s.Publication.CategoryId == selectedCategoryId.Value);
            }

            // Если указан период, фильтруем по дате создания
            if (_startDate.HasValue && _endDate.HasValue)
            {
                query = query.Where(s => s.CreatedDate >= _startDate && s.CreatedDate <= _endDate);
            }

            var subscriptions = query
                .OrderByDescending(s => s.CreatedDate)
                .Select(s => new
                {
                    Дата_оформления = s.CreatedDate,
                    Клиент = s.Client.FullName,
                    Адрес = s.Client.Address,
                    Телефон = s.Client.Phone,
                    Издание = s.Publication.Title,
                    Категория = s.Publication.Category.Name,
                    Период = $"{s.PeriodMonths} мес.",
                    Начало = s.ActualStartDate,
                    Окончание = s.ActualEndDate,
                    Статус = s.Status,
                    Сумма = s.TotalPrice,
                    Оплачена = s.IsFullyPaid ? "Да" : "Нет",
                    Дата_оплаты = s.PaidDate
                })
                .ToList();

            _reportData = subscriptions.Cast<object>().ToList();
            ReportDataGrid.ItemsSource = subscriptions;

            string statusText = string.IsNullOrEmpty(selectedStatus) ? "Все (кроме ожидающих оплаты)" : selectedStatus;
            ReportInfoText.Text = $"Подписки (статус: {statusText}). Всего: {subscriptions.Count}";
        }

        private void GeneratePendingSubscriptionsReport()
        {
            // Получаем выбранные фильтры
            int? selectedCategoryId = null;
            if (_categoryFilterComboBox != null && _categoryFilterComboBox.SelectedItem is Category selectedCategory && selectedCategory.Id != 0)
            {
                selectedCategoryId = selectedCategory.Id;
            }

            int? selectedPublicationId = null;
            if (_publicationFilterComboBox != null && _publicationFilterComboBox.SelectedItem is Publication selectedPublication && selectedPublication.Id != 0)
            {
                selectedPublicationId = selectedPublication.Id;
            }

            string selectedStatus = null;
            if (_statusFilterComboBox != null && _statusFilterComboBox.SelectedItem is string status && status != "Все")
            {
                selectedStatus = status;
            }
            else
            {
                // По умолчанию ищем подписки к оплате
                selectedStatus = "ожидает_оплаты";
            }

            var query = _context.Subscriptions
                .Include(s => s.Client)
                .Include(s => s.Publication)
                .ThenInclude(p => p.Category)
                .Where(s => s.Status == selectedStatus);

            if (selectedPublicationId.HasValue)
            {
                query = query.Where(s => s.PublicationId == selectedPublicationId.Value);
            }

            if (selectedCategoryId.HasValue)
            {
                query = query.Where(s => s.Publication.CategoryId == selectedCategoryId.Value);
            }

            // Если указан период, фильтруем по дате создания
            if (_startDate.HasValue && _endDate.HasValue)
            {
                query = query.Where(s => s.CreatedDate >= _startDate && s.CreatedDate <= _endDate);
            }

            var subscriptions = query
                .OrderByDescending(s => s.CreatedDate)
                .Select(s => new
                {
                    Дата_оформления = s.CreatedDate,
                    Клиент = s.Client.FullName,
                    Телефон = s.Client.Phone,
                    Email = s.Client.Email,
                    Издание = s.Publication.Title,
                    Категория = s.Publication.Category.Name,
                    Период = $"{s.PeriodMonths} мес.",
                    Сумма = s.TotalPrice,
                    Срок_начала = s.PlannedStartDate,
                    Статус = s.Status,
                    Срок_оплаты = s.PaymentDeadline
                })
                .ToList();

            _reportData = subscriptions.Cast<object>().ToList();
            ReportDataGrid.ItemsSource = subscriptions;
            ReportInfoText.Text = $"Подписки к оплате. Всего: {subscriptions.Count}";
        }

        private void GeneratePrintOrderReport()
        {
            if (!_startDate.HasValue)
            {
                _startDate = GetNextMonthForPrintOrder();
            }

            var targetMonth = _startDate.Value;
            var startOfMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Получаем выбранные фильтры
            int? selectedCategoryId = null;
            if (_categoryFilterComboBox != null && _categoryFilterComboBox.SelectedItem is Category selectedCategory && selectedCategory.Id != 0)
            {
                selectedCategoryId = selectedCategory.Id;
            }

            var query = _context.Subscriptions
                .Include(s => s.Publication)
                .ThenInclude(p => p.Category)
                .Where(s => s.Status == "активна" || s.Status == "оплачена")
                .Where(s => s.ActualStartDate <= endOfMonth && s.ActualEndDate >= startOfMonth);

            if (selectedCategoryId.HasValue)
            {
                query = query.Where(s => s.Publication.CategoryId == selectedCategoryId.Value);
            }

            var subscriptions = query.ToList();

            var printOrder = subscriptions
                .GroupBy(s => new {
                    s.PublicationId,
                    s.Publication.Title,
                    s.Publication.ISSN,
                    s.Publication.Publisher,
                    s.Publication.Periodicity,
                    CategoryName = s.Publication.Category.Name
                })
                .Select(g => new
                {
                    Издание = g.Key.Title,
                    Категория = g.Key.CategoryName,
                    ISSN = g.Key.ISSN,
                    Издатель = g.Key.Publisher,
                    Количество_подписок = g.Count(),
                    Периодичность = g.Key.Periodicity
                })
                .OrderBy(x => x.Категория)
                .ThenBy(x => x.Издание)
                .ToList();

            _reportData = printOrder.Cast<object>().ToList();
            ReportDataGrid.ItemsSource = printOrder;
            ReportInfoText.Text = $"Заказ в типографию на {targetMonth:MMMM yyyy}. Изданий: {printOrder.Count}";
        }

        private void GenerateReceiptsReport()
        {
            if (!_startDate.HasValue || !_endDate.HasValue)
            {
                MessageBox.Show("Выберите период для отчета по квитанциям", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получаем выбранные фильтры
            string selectedPaymentMethod = null;
            if (_paymentMethodFilterComboBox != null && _paymentMethodFilterComboBox.SelectedItem is string method && method != "Все")
            {
                selectedPaymentMethod = method;
            }

            string selectedPaymentStatus = null;
            if (_paymentStatusFilterComboBox != null && _paymentStatusFilterComboBox.SelectedItem is string status && status != "Все")
            {
                selectedPaymentStatus = status;
            }

            var query = _context.Payments
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Client)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Publication)
                .Include(p => p.Operator)
                .Where(p => p.PaymentDate >= _startDate && p.PaymentDate <= _endDate);

            if (selectedPaymentMethod != null)
            {
                query = query.Where(p => p.PaymentMethod == selectedPaymentMethod);
            }

            if (selectedPaymentStatus != null)
            {
                query = query.Where(p => p.PaymentStatus == selectedPaymentStatus);
            }

            var receipts = query
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    Квитанция = p.ReceiptNumber,
                    Дата = p.PaymentDate,
                    Клиент = p.Subscription.Client.FullName,
                    Издание = p.Subscription.Publication.Title,
                    Сумма = p.Amount,
                    Способ_оплаты = p.PaymentMethod,
                    Статус_платежа = p.PaymentStatus,
                    Оператор = p.Operator.FullName ?? "Не указан"
                })
                .ToList();

            _reportData = receipts.Cast<object>().ToList();
            ReportDataGrid.ItemsSource = receipts;
            ReportInfoText.Text = $"Квитанции за период {_startDate.Value:dd.MM.yyyy} - {_endDate.Value:dd.MM.yyyy}. Всего: {receipts.Count}";
        }

        private void GenerateClientsStatisticsReport()
        {
            var clients = _context.Clients
                .Include(c => c.Subscriptions)
                .ThenInclude(s => s.Publication)
                .OrderByDescending(c => c.Subscriptions.Count)
                .Select(c => new
                {
                    ФИО = c.FullName,
                    Адрес = c.Address,
                    Телефон = c.Phone,
                    Email = c.Email,
                    Дата_регистрации = c.RegistrationDate,
                    Всего_подписок = c.Subscriptions.Count,
                    Активных = c.Subscriptions.Count(s => s.Status == "активна"),
                    Ожидают_оплаты = c.Subscriptions.Count(s => s.Status == "ожидает_оплаты"),
                    Оплачены = c.Subscriptions.Count(s => s.Status == "оплачена"),
                    Завершенных = c.Subscriptions.Count(s => s.Status == "завершена"),
                    Общая_сумма = c.Subscriptions.Sum(s => s.TotalPrice)
                })
                .ToList();

            _reportData = clients.Cast<object>().ToList();
            ReportDataGrid.ItemsSource = clients;
            ReportInfoText.Text = $"Статистика клиентов. Всего: {clients.Count}";
        }

        private void GeneratePublicationsStatisticsReport()
        {
            // Получаем выбранные фильтры
            int? selectedCategoryId = null;
            if (_categoryFilterComboBox != null && _categoryFilterComboBox.SelectedItem is Category selectedCategory && selectedCategory.Id != 0)
            {
                selectedCategoryId = selectedCategory.Id;
            }

            var query = _context.Publications
                .Include(p => p.Category)
                .Include(p => p.Subscriptions)
                .AsQueryable();

            if (selectedCategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == selectedCategoryId.Value);
            }

            var publications = query
                .OrderByDescending(p => p.Subscriptions.Count)
                .Select(p => new
                {
                    Название = p.Title,
                    Категория = p.Category.Name,
                    Издатель = p.Publisher,
                    ISSN = p.ISSN,
                    Периодичность = p.Periodicity,
                    Подписок_всего = p.Subscriptions.Count,
                    Подписок_активных = p.Subscriptions.Count(s => s.Status == "активна"),
                    Подписок_ожидают = p.Subscriptions.Count(s => s.Status == "ожидает_оплаты"),
                    Подписок_оплачены = p.Subscriptions.Count(s => s.Status == "оплачена"),
                    Общий_доход = p.Subscriptions.Sum(s => s.TotalPrice),
                    Доступно = p.IsAvailable ? "Да" : "Нет"
                })
                .ToList();

            _reportData = publications.Cast<object>().ToList();
            ReportDataGrid.ItemsSource = publications;
            ReportInfoText.Text = $"Статистика изданий. Всего: {publications.Count}";
        }

        private void GenerateSubscriptionsByTimeReport()
        {
            if (!_startDate.HasValue || !_endDate.HasValue)
            {
                MessageBox.Show("Выберите период для отчета", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var subscriptions = _context.Subscriptions
                .Include(s => s.Client)
                .Include(s => s.Publication)
                .ThenInclude(p => p.Category)
                .Where(s => s.CreatedDate >= _startDate && s.CreatedDate <= _endDate)
                .ToList();

            string groupBy = GetGroupBySetting();
            List<object> groupedData;

            if (groupBy == "День")
            {
                groupedData = subscriptions
                    .GroupBy(s => s.CreatedDate.Date)
                    .Select(g => new
                    {
                        Дата = g.Key.ToString("dd.MM.yyyy"),
                        Количество = g.Count(),
                        Сумма = g.Sum(s => s.TotalPrice),
                        Активных = g.Count(s => s.Status == "активна"),
                        Ожидают = g.Count(s => s.Status == "ожидает_оплаты")
                    })
                    .OrderBy(x => x.Дата)
                    .Cast<object>()
                    .ToList();
            }
            else if (groupBy == "Неделя")
            {
                groupedData = subscriptions
                    .GroupBy(s => GetWeekOfYear(s.CreatedDate))
                    .Select(g => new
                    {
                        Неделя = $"Неделя {g.Key}",
                        Количество = g.Count(),
                        Сумма = g.Sum(s => s.TotalPrice),
                        Активных = g.Count(s => s.Status == "активна"),
                        Ожидают = g.Count(s => s.Status == "ожидает_оплаты")
                    })
                    .OrderBy(x => x.Неделя)
                    .Cast<object>()
                    .ToList();
            }
            else if (groupBy == "Месяц")
            {
                groupedData = subscriptions
                    .GroupBy(s => new { Year = s.CreatedDate.Year, Month = s.CreatedDate.Month })
                    .Select(g => new
                    {
                        Месяц = $"{g.Key.Month:D2}.{g.Key.Year}",
                        Количество = g.Count(),
                        Сумма = g.Sum(s => s.TotalPrice),
                        Активных = g.Count(s => s.Status == "активна"),
                        Ожидают = g.Count(s => s.Status == "ожидает_оплаты")
                    })
                    .OrderBy(x => x.Месяц)
                    .Cast<object>()
                    .ToList();
            }
            else if (groupBy == "Год")
            {
                groupedData = subscriptions
                    .GroupBy(s => s.CreatedDate.Year)
                    .Select(g => new
                    {
                        Год = $"{g.Key} год",
                        Количество = g.Count(),
                        Сумма = g.Sum(s => s.TotalPrice),
                        Активных = g.Count(s => s.Status == "активна"),
                        Ожидают = g.Count(s => s.Status == "ожидает_оплаты")
                    })
                    .OrderBy(x => x.Год)
                    .Cast<object>()
                    .ToList();
            }
            else if (groupBy == "Категория")
            {
                groupedData = subscriptions
                    .GroupBy(s => s.Publication.Category.Name)
                    .Select(g => new
                    {
                        Категория = g.Key,
                        Количество = g.Count(),
                        Сумма = g.Sum(s => s.TotalPrice),
                        Активных = g.Count(s => s.Status == "активна"),
                        Ожидают = g.Count(s => s.Status == "ожидает_оплаты")
                    })
                    .OrderByDescending(x => x.Количество)
                    .Cast<object>()
                    .ToList();
            }
            else if (groupBy == "Статус")
            {
                groupedData = subscriptions
                    .GroupBy(s => s.Status)
                    .Select(g => new
                    {
                        Статус = g.Key,
                        Количество = g.Count(),
                        Сумма = g.Sum(s => s.TotalPrice)
                    })
                    .OrderByDescending(x => x.Количество)
                    .Cast<object>()
                    .ToList();
            }
            else
            {
                groupedData = subscriptions
                    .Select(s => new
                    {
                        Дата = s.CreatedDate,
                        ID = s.Id,
                        Клиент = s.Client.FullName,
                        Издание = s.Publication.Title,
                        Категория = s.Publication.Category.Name,
                        Период = $"{s.PeriodMonths} мес.",
                        Сумма = s.TotalPrice,
                        Статус = s.Status,
                        Начало = s.ActualStartDate,
                        Окончание = s.ActualEndDate
                    })
                    .OrderByDescending(x => x.Дата)
                    .Cast<object>()
                    .ToList();
            }

            _reportData = groupedData;
            ReportDataGrid.ItemsSource = groupedData;
            ReportInfoText.Text = $"Подписки за период {_startDate.Value:dd.MM.yyyy} - {_endDate.Value:dd.MM.yyyy}. Всего: {subscriptions.Count}";
        }

        private int GetWeekOfYear(DateTime date)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private void GeneratePaymentsByTimeReport()
        {
            if (!_startDate.HasValue || !_endDate.HasValue)
            {
                MessageBox.Show("Выберите период для отчета", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var payments = _context.Payments
                .Include(p => p.Subscription)
                .ThenInclude(s => s.Client)
                .Include(p => p.Subscription)
                .ThenInclude(s => s.Publication)
                .Where(p => p.PaymentDate >= _startDate && p.PaymentDate <= _endDate)
                .ToList();

            string groupBy = GetGroupBySetting();
            List<object> groupedData;

            if (groupBy == "День")
            {
                groupedData = payments
                    .GroupBy(p => p.PaymentDate.Date)
                    .Select(g => new
                    {
                        Дата = g.Key.ToString("dd.MM.yyyy"),
                        Количество = g.Count(),
                        Сумма = g.Sum(p => p.Amount),
                        Подтверждено = g.Count(p => p.PaymentStatus == "подтвержден"),
                        Ожидает = g.Count(p => p.PaymentStatus == "ожидает_подтверждения")
                    })
                    .OrderBy(x => x.Дата)
                    .Cast<object>()
                    .ToList();
            }
            else if (groupBy == "Неделя")
            {
                groupedData = payments
                    .GroupBy(p => GetWeekOfYear(p.PaymentDate))
                    .Select(g => new
                    {
                        Неделя = $"Неделя {g.Key}",
                        Количество = g.Count(),
                        Сумма = g.Sum(p => p.Amount),
                        Подтверждено = g.Count(p => p.PaymentStatus == "подтвержден"),
                        Ожидает = g.Count(p => p.PaymentStatus == "ожидает_подтверждения")
                    })
                    .OrderBy(x => x.Неделя)
                    .Cast<object>()
                    .ToList();
            }
            else if (groupBy == "Месяц")
            {
                groupedData = payments
                    .GroupBy(p => new { Year = p.PaymentDate.Year, Month = p.PaymentDate.Month })
                    .Select(g => new
                    {
                        Месяц = $"{g.Key.Month:D2}.{g.Key.Year}",
                        Количество = g.Count(),
                        Сумма = g.Sum(p => p.Amount),
                        Подтверждено = g.Count(p => p.PaymentStatus == "подтвержден"),
                        Ожидает = g.Count(p => p.PaymentStatus == "ожидает_подтверждения")
                    })
                    .OrderBy(x => x.Месяц)
                    .Cast<object>()
                    .ToList();
            }
            else if (groupBy == "Категория")
            {
                groupedData = payments
                    .GroupBy(p => p.Subscription.Publication.Category.Name)
                    .Select(g => new
                    {
                        Категория = g.Key,
                        Количество = g.Count(),
                        Сумма = g.Sum(p => p.Amount),
                        Подтверждено = g.Count(p => p.PaymentStatus == "подтвержден")
                    })
                    .OrderByDescending(x => x.Сумма)
                    .Cast<object>()
                    .ToList();
            }
            else
            {
                groupedData = payments
                    .Select(p => new
                    {
                        Дата = p.PaymentDate,
                        Квитанция = p.ReceiptNumber,
                        Клиент = p.Subscription.Client.FullName,
                        Издание = p.Subscription.Publication.Title,
                        Сумма = p.Amount,
                        Способ_оплаты = p.PaymentMethod,
                        Статус_платежа = p.PaymentStatus
                    })
                    .OrderByDescending(x => x.Дата)
                    .Cast<object>()
                    .ToList();
            }

            _reportData = groupedData;
            ReportDataGrid.ItemsSource = groupedData;
            ReportInfoText.Text = $"Платежи за период {_startDate.Value:dd.MM.yyyy} - {_endDate.Value:dd.MM.yyyy}. Всего: {payments.Count}";
        }

        private void GenerateClientsByRegionReport()
        {
            var clients = _context.Clients.Include(c => c.Subscriptions).ToList();

            // Получаем выбранный регион
            string selectedRegion = null;
            if (_regionFilterComboBox != null && _regionFilterComboBox.SelectedItem is string region && region != "Все регионы")
            {
                selectedRegion = region;
            }

            var clientsWithRegion = clients
                .Select(c => new
                {
                    Client = c,
                    Region = string.IsNullOrEmpty(c.Address) ? "Не указан" :
                             c.Address.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .FirstOrDefault()?.Trim() ?? "Не указан"
                })
                .ToList();

            if (selectedRegion != null)
            {
                clientsWithRegion = clientsWithRegion
                    .Where(c => c.Region == selectedRegion)
                    .ToList();
            }

            var groupedByRegion = clientsWithRegion
                .GroupBy(c => c.Region)
                .Select(g => new
                {
                    Регион = g.Key,
                    Количество_клиентов = g.Count(),
                    Всего_подписок = g.Sum(c => c.Client.Subscriptions.Count),
                    Активных_подписок = g.Sum(c => c.Client.Subscriptions.Count(s => s.Status == "активна")),
                    Ожидают_оплаты = g.Sum(c => c.Client.Subscriptions.Count(s => s.Status == "ожидает_оплаты")),
                    Общая_сумма = g.Sum(c => c.Client.Subscriptions.Sum(s => s.TotalPrice))
                })
                .OrderByDescending(x => x.Количество_клиентов)
                .ToList();

            _reportData = groupedByRegion.Cast<object>().ToList();
            ReportDataGrid.ItemsSource = groupedByRegion;
            ReportInfoText.Text = $"Клиенты по регионам. Всего клиентов: {clientsWithRegion.Count}";
        }

        private string GetGroupBySetting()
        {
            return GroupByComboBox.SelectedItem is ComboBoxItem selectedItem ? selectedItem.Content.ToString() : "Нет";
        }

        private void UpdateSummary()
        {
            if (_reportData == null || _reportData.Count == 0)
            {
                SummaryText.Text = "Нет данных";
                return;
            }

            decimal totalAmount = 0;
            int totalCount = _reportData.Count;

            if (_reportData.Count > 0)
            {
                var firstItem = _reportData[0];
                var type = firstItem.GetType();

                // Ищем свойства для суммы
                var sumProperties = new[] { "Сумма", "Общая_сумма", "TotalPrice", "Amount", "Цена_мес", "Цена_квартал", "Цена_год" };

                foreach (var propName in sumProperties)
                {
                    var sumProperty = type.GetProperty(propName);
                    if (sumProperty != null)
                    {
                        totalAmount = _reportData.Cast<object>().Sum(item =>
                        {
                            var value = sumProperty.GetValue(item);
                            if (value is decimal) return (decimal)value;
                            if (value is double) return (decimal)(double)value;
                            if (value is int) return (decimal)(int)value;
                            return 0;
                        });
                        break;
                    }
                }
            }

            SummaryText.Text = $"Всего записей: {totalCount}\nОбщая сумма: {totalAmount:C}";
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_reportData == null || _reportData.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt|CSV файлы (*.csv)|*.csv",
                    FileName = $"Отчет_{DateTime.Now:yyyyMMdd_HHmm}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var reportContent = new System.Text.StringBuilder();
                    reportContent.AppendLine($"Отчет: {ReportTitleText.Text}");
                    reportContent.AppendLine($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}");

                    if (_startDate.HasValue && _endDate.HasValue)
                    {
                        reportContent.AppendLine($"Период: {_startDate.Value:dd.MM.yyyy} - {_endDate.Value:dd.MM.yyyy}");
                    }

                    reportContent.AppendLine();

                    if (_reportData.Count > 0)
                    {
                        var firstItem = _reportData[0];
                        var type = firstItem.GetType();
                        var properties = type.GetProperties();

                        // Используем точку с запятой как разделитель для CSV
                        string separator = saveDialog.FileName.EndsWith(".csv") ? ";" : "\t";

                        reportContent.AppendLine(string.Join(separator, properties.Select(p => p.Name)));

                        foreach (var item in _reportData)
                        {
                            var values = properties.Select(p =>
                            {
                                var value = p.GetValue(item);
                                return value?.ToString()?.Replace("\n", " ").Replace("\r", " ") ?? "";
                            });
                            reportContent.AppendLine(string.Join(separator, values));
                        }
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, reportContent.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show($"Данные экспортированы в файл: {saveDialog.FileName}", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearReport_Click(object sender, RoutedEventArgs e)
        {
            ReportDataGrid.ItemsSource = null;
            ReportInfoText.Text = "Выберите тип отчета и период";
            SummaryText.Text = "Итоги:";
            _reportData = null;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
            else NavigationService.Navigate(new MainPage());
        }

        private void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Экспорт в PDF будет реализован в следующей версии", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Печать будет реализована в следующей версии", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}