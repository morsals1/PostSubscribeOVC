using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using WpfSUB.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;

namespace WpfSUB.Pages
{
    public partial class PaymentPage : Page
    {
        private ObservableCollection<Payment> _payments;

        public PaymentPage()
        {
            InitializeComponent();
            LoadPayments();
        }

        private void LoadPayments()
        {
            using (var context = new AppDbContext())
            {
                var payments = context.Payments
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s.Client)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();

                _payments = new ObservableCollection<Payment>(payments);
                PaymentsListView.ItemsSource = _payments;
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = PaymentsListView.SelectedItem != null;
            bool isPending = false;

            if (hasSelection && PaymentsListView.SelectedItem is Payment selectedPayment)
            {
                isPending = selectedPayment.PaymentStatus == "ожидает_подтверждения";
            }

            ConfirmButton.IsEnabled = hasSelection && isPending;
            RejectButton.IsEnabled = hasSelection && isPending;
        }

        private void PaymentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            MessageBox.Show("Добавление платежа - будет реализовано");
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentsListView.SelectedItem is Payment selectedPayment)
            {
                using (var context = new AppDbContext())
                {
                    var payment = context.Payments.Find(selectedPayment.Id);
                    if (payment != null)
                    {
                        payment.PaymentStatus = "подтвержден";
                        context.SaveChanges();

                        MessageBox.Show("Платеж подтвержден", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadPayments();
                    }
                }
            }
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentsListView.SelectedItem is Payment selectedPayment)
            {
                using (var context = new AppDbContext())
                {
                    var payment = context.Payments.Find(selectedPayment.Id);
                    if (payment != null)
                    {
                        payment.PaymentStatus = "отклонен";
                        context.SaveChanges();

                        MessageBox.Show("Платеж отклонен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadPayments();
                    }
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPayments();
        }
    }
}