using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class PaymentFormPage : Page
    {
        private AppDbContext _context;
        private Payment _payment;
        private Subscription _selectedSubscription;

        public PaymentFormPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _payment = new Payment();
            LoadData();
            DataContext = _payment;
        }

        public PaymentFormPage(Subscription subscription)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _payment = new Payment();
            _selectedSubscription = subscription;
            LoadData();
            DataContext = _payment;

            // Автозаполнение для конкретной подписки
            if (subscription != null)
            {
                SubscriptionComboBox.SelectedItem = subscription;
                AmountTextBox.Text = subscription.TotalPrice.ToString("F2");
                _payment.Amount = subscription.TotalPrice;
            }
        }

        private void LoadData()
        {
            // Загружаем подписки, ожидающие оплаты
            var subscriptions = _context.Subscriptions
                .Include(s => s.Client)
                .Include(s => s.Publication)
                .Where(s => s.Status == "ожидает_оплаты" || s.Status == "оформлена")
                .OrderByDescending(s => s.CreatedDate)
                .ToList();

            SubscriptionComboBox.ItemsSource = subscriptions;

            // Загружаем способы оплаты
            PaymentMethodComboBox.SelectedIndex = 0;

            // Загружаем операторов
            var operators = _context.Operators.OrderBy(o => o.FullName).ToList();
            OperatorComboBox.ItemsSource = operators;

            // Генерируем номер квитанции
            GenerateReceiptNumber();
        }

        private void GenerateReceiptNumber()
        {
            string prefix = "REC";
            string date = DateTime.Now.ToString("yyyyMMdd");
            string random = new Random().Next(1000, 9999).ToString();
            ReceiptNumberTextBox.Text = $"{prefix}-{date}-{random}";
            _payment.ReceiptNumber = ReceiptNumberTextBox.Text;
        }

        private bool ValidateForm()
        {
            // Проверка подписки
            if (_selectedSubscription == null)
            {
                MessageBox.Show("Выберите подписку для оплаты",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                SubscriptionComboBox.Focus();
                return false;
            }

            // Проверка суммы
            if (_payment.Amount <= 0)
            {
                MessageBox.Show("Сумма платежа должна быть больше 0",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return false;
            }

            // Проверка соответствия суммы
            if (_payment.Amount != _selectedSubscription.TotalPrice)
            {
                var result = MessageBox.Show($"Сумма платежа ({_payment.Amount:C}) не соответствует " +
                                           $"стоимости подписки ({_selectedSubscription.TotalPrice:C}).\n" +
                                           "Продолжить?",
                    "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    return false;
            }

            // Проверка номера квитанции
            if (string.IsNullOrWhiteSpace(_payment.ReceiptNumber))
            {
                MessageBox.Show("Введите номер квитанции",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReceiptNumberTextBox.Focus();
                return false;
            }

            // Проверка уникальности номера квитанции
            var existingPayment = _context.Payments
                .FirstOrDefault(p => p.ReceiptNumber == _payment.ReceiptNumber);

            if (existingPayment != null)
            {
                MessageBox.Show("Квитанция с таким номером уже существует:\n" +
                               $"ID платежа: {existingPayment.Id}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка способа оплаты
            if (string.IsNullOrWhiteSpace(_payment.PaymentMethod))
            {
                MessageBox.Show("Выберите способ оплаты",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PaymentMethodComboBox.Focus();
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
                // Создаем платеж
                _payment.SubscriptionId = _selectedSubscription.Id;
                _payment.PaymentDate = DateTime.Now;
                _payment.PaymentStatus = "ожидает_подтверждения";

                // Устанавливаем оператора, если выбран
                if (OperatorComboBox.SelectedItem is Operator selectedOperator)
                {
                    _payment.OperatorId = selectedOperator.Id;
                }

                _context.Payments.Add(_payment);

                // Обновляем статус подписки
                _selectedSubscription.Status = "ожидает_оплаты";
                _context.Subscriptions.Update(_selectedSubscription);

                _context.SaveChanges();

                string message = $"Платеж зарегистрирован успешно!\n\n" +
                               $"Квитанция №: {_payment.ReceiptNumber}\n" +
                               $"Сумма: {_payment.Amount:C}\n" +
                               $"Подписка: {_selectedSubscription.Publication?.Title}\n" +
                               $"Клиент: {_selectedSubscription.Client?.FullName}\n\n" +
                               $"Статус: {_payment.PaymentStatus}\n" +
                               $"Для активации подписки необходимо подтвердить платеж в разделе 'Платежи'.";

                MessageBox.Show(message, "Платеж зарегистрирован",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new PaymentPage());
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

        private void SubscriptionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SubscriptionComboBox.SelectedItem is Subscription subscription)
            {
                _selectedSubscription = subscription;
                AmountTextBox.Text = subscription.TotalPrice.ToString("F2");
                _payment.Amount = subscription.TotalPrice;

                // Показываем информацию о подписке
                SubscriptionInfoTextBlock.Text = $"Клиент: {subscription.Client?.FullName}\n" +
                                               $"Издание: {subscription.Publication?.Title}\n" +
                                               $"Период: {subscription.PeriodMonths} мес.\n" +
                                               $"Дата начала: {subscription.PlannedStartDate:dd.MM.yyyy}\n" +
                                               $"Требуемая сумма: {subscription.TotalPrice:C}";
            }
        }

        private void GenerateTransactionId_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentMethodComboBox.SelectedItem is string method &&
                (method == "карта_онлайн" || method == "банковский_перевод"))
            {
                string transactionId = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
                TransactionIdTextBox.Text = transactionId;
                _payment.BankTransactionId = transactionId;
            }
            else
            {
                MessageBox.Show("ID транзакции требуется только для онлайн платежей и банковских переводов",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GenerateReceiptNumber_Click(object sender, RoutedEventArgs e)
        {
            GenerateReceiptNumber();
        }
    }
}