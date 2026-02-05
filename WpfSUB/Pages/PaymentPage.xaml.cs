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
    public partial class PaymentPage : Page
    {
        private ObservableCollection<Payment> _payments;
        private AppDbContext _context;

        public PaymentPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadPayments();
            FilterComboBox.SelectedIndex = 0;
        }

        private void LoadPayments()
        {
            var payments = _context.Payments
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Client)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Publication)
                .Include(p => p.Operator)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            _payments = new ObservableCollection<Payment>(payments);
            PaymentsListView.ItemsSource = _payments;
            UpdateStatistics();
            UpdateButtonStates();
        }

        private void UpdateStatistics()
        {
            int totalPayments = _payments.Count;
            int confirmedPayments = _payments.Count(p => p.PaymentStatus == "подтвержден");
            int pendingPayments = _payments.Count(p => p.PaymentStatus == "ожидает_подтверждения");
            decimal totalAmount = _payments.Where(p => p.PaymentStatus == "подтвержден")
                .Sum(p => p.Amount);
            decimal pendingAmount = _payments.Where(p => p.PaymentStatus == "ожидает_подтверждения")
                .Sum(p => p.Amount);

            StatsTextBlock.Text = $"Всего платежей: {totalPayments} | " +
                                 $"Подтверждено: {confirmedPayments} | " +
                                 $"Ожидает: {pendingPayments}\n" +
                                 $"Сумма подтвержденных: {totalAmount:C} | " +
                                 $"Сумма ожидающих: {pendingAmount:C}";
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = PaymentsListView.SelectedItem != null;
            bool isPending = false;
            bool isConfirmed = false;

            if (hasSelection && PaymentsListView.SelectedItem is Payment selectedPayment)
            {
                isPending = selectedPayment.PaymentStatus == "ожидает_подтверждения";
                isConfirmed = selectedPayment.PaymentStatus == "подтвержден";
            }

            ConfirmButton.IsEnabled = hasSelection && isPending;
            RejectButton.IsEnabled = hasSelection && isPending;
            RefundButton.IsEnabled = hasSelection && isConfirmed;
            ViewDetailsButton.IsEnabled = hasSelection;
        }

        private void PaymentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            NavigationService.Navigate(new PaymentFormPage());
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentsListView.SelectedItem is Payment selectedPayment)
            {
                var payment = _context.Payments
                    .Include(p => p.Subscription)
                    .FirstOrDefault(p => p.Id == selectedPayment.Id);

                if (payment == null) return;

                if (MessageBox.Show($"Подтвердить платеж №{payment.ReceiptNumber}?\n" +
                                   $"Сумма: {payment.Amount:C}\n" +
                                   $"Клиент: {payment.Subscription?.Client?.FullName}",
                    "Подтверждение платежа", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    payment.PaymentStatus = "подтвержден";
                    payment.ProcessedDate = DateTime.Now;

                    // Обновляем статус подписки
                    if (payment.Subscription != null)
                    {
                        payment.Subscription.IsFullyPaid = true;
                        payment.Subscription.PaidDate = DateTime.Now;
                        payment.Subscription.Status = "оплачена";

                        // Устанавливаем даты начала и окончания
                        if (!payment.Subscription.ActualStartDate.HasValue)
                        {
                            payment.Subscription.ActualStartDate = GetNextFirstOfMonth();
                            payment.Subscription.ActualEndDate = payment.Subscription.ActualStartDate.Value
                                .AddMonths(payment.Subscription.PeriodMonths)
                                .AddDays(-1);
                        }

                        payment.Subscription.Status = "активна";
                    }

                    _context.SaveChanges();

                    MessageBox.Show("Платеж подтвержден, подписка активирована",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadPayments();
                }
            }
        }

        private DateTime GetNextFirstOfMonth()
        {
            var today = DateTime.Today;
            return new DateTime(today.Year, today.Month, 1).AddMonths(1);
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentsListView.SelectedItem is Payment selectedPayment)
            {
                var payment = _context.Payments.Find(selectedPayment.Id);
                if (payment == null) return;

                if (MessageBox.Show($"Отклонить платеж №{payment.ReceiptNumber}?\n" +
                                   $"Сумма: {payment.Amount:C}",
                    "Отклонение платежа", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    payment.PaymentStatus = "отклонен";
                    _context.SaveChanges();

                    MessageBox.Show("Платеж отклонен",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadPayments();
                }
            }
        }

        private void Refund_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentsListView.SelectedItem is Payment selectedPayment)
            {
                var payment = _context.Payments
                    .Include(p => p.Subscription)
                    .FirstOrDefault(p => p.Id == selectedPayment.Id);

                if (payment == null) return;

                if (MessageBox.Show($"Выполнить возврат платежа №{payment.ReceiptNumber}?\n" +
                                   $"Сумма: {payment.Amount:C}\n" +
                                   $"Клиент: {payment.Subscription?.Client?.FullName}\n\n" +
                                   "Внимание: Это отменит подписку клиента!",
                    "Возврат платежа", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    payment.PaymentStatus = "возвращен";

                    // Отменяем подписку
                    if (payment.Subscription != null)
                    {
                        payment.Subscription.IsFullyPaid = false;
                        payment.Subscription.PaidDate = null;
                        payment.Subscription.Status = "отменена";
                    }

                    _context.SaveChanges();

                    MessageBox.Show("Платеж возвращен, подписка отменена",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadPayments();
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPayments();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                var currentFilter = (FilterComboBox.SelectedItem as ComboBoxItem)?.Tag as string;
                ApplyFilter(currentFilter);
            }
            else
            {
                var filtered = _payments.Where(p =>
                    p.ReceiptNumber.ToLower().Contains(searchText) ||
                    p.Subscription?.Client?.FullName.ToLower().Contains(searchText) == true ||
                    p.Subscription?.Publication?.Title.ToLower().Contains(searchText) == true ||
                    p.BankTransactionId?.ToLower().Contains(searchText) == true)
                    .ToList();

                PaymentsListView.ItemsSource = new ObservableCollection<Payment>(filtered);
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentsListView.SelectedItem is Payment selectedPayment)
            {
                var payment = _context.Payments
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s.Client)
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s.Publication)
                    .Include(p => p.Operator)
                    .FirstOrDefault(p => p.Id == selectedPayment.Id);

                if (payment == null) return;

                string details = $"Детали платежа:\n\n" +
                               $"Квитанция №: {payment.ReceiptNumber}\n" +
                               $"Дата платежа: {payment.PaymentDate:dd.MM.yyyy HH:mm}\n" +
                               $"Сумма: {payment.Amount:C}\n" +
                               $"Способ оплаты: {payment.PaymentMethod}\n" +
                               $"Статус: {payment.PaymentStatus}\n\n" +
                               $"Клиент: {payment.Subscription?.Client?.FullName}\n" +
                               $"Издание: {payment.Subscription?.Publication?.Title}\n" +
                               $"Период: {payment.Subscription?.PeriodMonths} мес.\n" +
                               $"Общая сумма подписки: {payment.Subscription?.TotalPrice:C}\n" +
                               $"Оператор: {payment.Operator?.FullName ?? "Не указан"}";

                if (payment.ProcessedDate.HasValue)
                {
                    details += $"\nОбработан: {payment.ProcessedDate:dd.MM.yyyy HH:mm}";
                }

                if (!string.IsNullOrEmpty(payment.BankTransactionId))
                {
                    details += $"\nID транзакции: {payment.BankTransactionId}";
                }

                MessageBox.Show(details, "Детали платежа",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string filter = selectedItem.Tag as string;
                ApplyFilter(filter);
            }
        }

        private void ApplyFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                PaymentsListView.ItemsSource = _payments;
            }
            else
            {
                var filtered = _payments.Where(p => p.PaymentStatus == filter).ToList();
                PaymentsListView.ItemsSource = new ObservableCollection<Payment>(filtered);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var paymentsToExport = PaymentsListView.ItemsSource as ObservableCollection<Payment> ?? _payments;

                // Используем StringBuilder вместо конкатенации строк
                System.Text.StringBuilder exportBuilder = new System.Text.StringBuilder();

                exportBuilder.AppendLine("Отчет по платежам системы подписки");
                exportBuilder.AppendLine($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm}");
                exportBuilder.AppendLine("=================================");
                exportBuilder.AppendLine();

                decimal totalAmount = 0;

                foreach (var payment in paymentsToExport)
                {
                    exportBuilder.AppendLine($"Квитанция: {payment.ReceiptNumber}");
                    exportBuilder.AppendLine($"Дата: {payment.PaymentDate:dd.MM.yyyy HH:mm}");
                    exportBuilder.AppendLine($"Сумма: {payment.Amount:C}");
                    exportBuilder.AppendLine($"Способ: {payment.PaymentMethod}");
                    exportBuilder.AppendLine($"Статус: {payment.PaymentStatus}");
                    exportBuilder.AppendLine($"Клиент: {payment.Subscription?.Client?.FullName}");
                    exportBuilder.AppendLine($"Издание: {payment.Subscription?.Publication?.Title}");
                    exportBuilder.AppendLine("---------------------------------");

                    if (payment.PaymentStatus == "подтвержден")
                    {
                        totalAmount += payment.Amount;
                    }
                }

                exportBuilder.AppendLine($"\nИтого подтвержденных платежей: {totalAmount:C}");

                string exportText = exportBuilder.ToString();

                // Используем WPF SaveFileDialog вместо Windows Forms
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt",
                    FileName = $"Платежи_{DateTime.Now:yyyyMMdd_HHmm}.txt"
                };

                // В WPF используется bool? результат вместо DialogResult
                if (saveDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, exportText);
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