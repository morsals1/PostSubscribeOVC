using System;

namespace WpfSUB.Models
{
    public class SubscriptionServiceLink : ObservableObject
    {
        // Составной ключ
        private int _subscriptionId;
        public int SubscriptionId
        {
            get => _subscriptionId;
            set => SetProperty(ref _subscriptionId, value);
        }

        private Subscription _subscription;
        public Subscription Subscription
        {
            get => _subscription;
            set => SetProperty(ref _subscription, value);
        }

        private int _serviceId;
        public int ServiceId
        {
            get => _serviceId;
            set => SetProperty(ref _serviceId, value);
        }

        private AdditionalService _service;
        public AdditionalService Service
        {
            get => _service;
            set => SetProperty(ref _service, value);
        }

        // Дополнительные данные
        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public SubscriptionServiceLink()
        {
            StartDate = DateTime.Now;
            EndDate = DateTime.Now.AddMonths(1);
        }
    }
}