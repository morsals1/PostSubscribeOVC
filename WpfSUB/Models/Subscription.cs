using System;
using System.Collections.ObjectModel;
using WpfSUB.Models;

namespace WpfSUB.Models
{
    public class Subscription : ObservableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        // Связи
        private int _clientId;
        public int ClientId
        {
            get => _clientId;
            set => SetProperty(ref _clientId, value);
        }

        private Client _client;
        public Client Client
        {
            get => _client;
            set => SetProperty(ref _client, value);
        }

        private int _publicationId;
        public int PublicationId
        {
            get => _publicationId;
            set => SetProperty(ref _publicationId, value);
        }

        private Publication _publication;
        public Publication Publication
        {
            get => _publication;
            set => SetProperty(ref _publication, value);
        }

        // Период подписки
        private int _periodMonths;
        public int PeriodMonths
        {
            get => _periodMonths;
            set => SetProperty(ref _periodMonths, value);
        }

        // Цены
        private decimal _monthlyPrice;
        public decimal MonthlyPrice
        {
            get => _monthlyPrice;
            set => SetProperty(ref _monthlyPrice, value);
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        // Даты
        private DateTime _createdDate;
        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        private DateTime? _plannedStartDate;
        public DateTime? PlannedStartDate
        {
            get => _plannedStartDate;
            set => SetProperty(ref _plannedStartDate, value);
        }

        private DateTime? _actualStartDate;
        public DateTime? ActualStartDate
        {
            get => _actualStartDate;
            set => SetProperty(ref _actualStartDate, value);
        }

        private DateTime? _plannedEndDate;
        public DateTime? PlannedEndDate
        {
            get => _plannedEndDate;
            set => SetProperty(ref _plannedEndDate, value);
        }

        private DateTime? _actualEndDate;
        public DateTime? ActualEndDate
        {
            get => _actualEndDate;
            set => SetProperty(ref _actualEndDate, value);
        }

        // Статус
        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        // Оплата
        private bool _isFullyPaid;
        public bool IsFullyPaid
        {
            get => _isFullyPaid;
            set => SetProperty(ref _isFullyPaid, value);
        }

        private DateTime? _paidDate;
        public DateTime? PaidDate
        {
            get => _paidDate;
            set => SetProperty(ref _paidDate, value);
        }

        private DateTime? _paymentDeadline;
        public DateTime? PaymentDeadline
        {
            get => _paymentDeadline;
            set => SetProperty(ref _paymentDeadline, value);
        }

        // Связи
        private ObservableCollection<Payment> _payments;
        public ObservableCollection<Payment> Payments
        {
            get => _payments;
            set => SetProperty(ref _payments, value);
        }

        private ObservableCollection<Delivery> _deliveries;
        public ObservableCollection<Delivery> Deliveries
        {
            get => _deliveries;
            set => SetProperty(ref _deliveries, value);
        }

        private ObservableCollection<SubscriptionServiceLink> _serviceLinks;
        public ObservableCollection<SubscriptionServiceLink> ServiceLinks
        {
            get => _serviceLinks;
            set => SetProperty(ref _serviceLinks, value);
        }

        public Subscription()
        {
            Payments = new ObservableCollection<Payment>();
            Deliveries = new ObservableCollection<Delivery>();
            ServiceLinks = new ObservableCollection<SubscriptionServiceLink>();
            CreatedDate = DateTime.Now;
            Status = "оформлена";
            IsFullyPaid = false;
        }

        // Методы
        public void CalculateDates()
        {
            if (PlannedStartDate.HasValue && PeriodMonths > 0)
            {
                PlannedEndDate = PlannedStartDate.Value.AddMonths(PeriodMonths).AddDays(-1);
            }
        }

        public bool CanBeActivated()
        {
            return Status == "оплачена" &&
                   IsFullyPaid &&
                   ActualStartDate.HasValue &&
                   ActualStartDate <= DateTime.Today;
        }
    }
}