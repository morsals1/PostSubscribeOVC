using System;

namespace WpfSUB.Models
{
    public class Delivery : ObservableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        // Связь с подпиской
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

        // Информация о выпуске
        private int _issueNumber;
        public int IssueNumber
        {
            get => _issueNumber;
            set => SetProperty(ref _issueNumber, value);
        }

        private DateTime _issueDate;
        public DateTime IssueDate
        {
            get => _issueDate;
            set => SetProperty(ref _issueDate, value);
        }

        private DateTime _expectedDeliveryDate;
        public DateTime ExpectedDeliveryDate
        {
            get => _expectedDeliveryDate;
            set => SetProperty(ref _expectedDeliveryDate, value);
        }

        // Статус
        private string _deliveryStatus;
        public string DeliveryStatus
        {
            get => _deliveryStatus;
            set => SetProperty(ref _deliveryStatus, value);
        }

        // Фактические даты
        private DateTime? _sentAt;
        public DateTime? SentAt
        {
            get => _sentAt;
            set => SetProperty(ref _sentAt, value);
        }

        private DateTime? _deliveredAt;
        public DateTime? DeliveredAt
        {
            get => _deliveredAt;
            set => SetProperty(ref _deliveredAt, value);
        }

        // Причина проблем
        private string _problemReason;
        public string ProblemReason
        {
            get => _problemReason;
            set => SetProperty(ref _problemReason, value);
        }

        public Delivery()
        {
            DeliveryStatus = "запланирована";
        }
    }
}