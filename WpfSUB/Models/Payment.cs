using System;
using WpfSUB.Models;

namespace WpfSUB.Models
{
    public class Payment : ObservableObject
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

        // Сумма и детали
        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        // Даты
        private DateTime _paymentDate;
        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => SetProperty(ref _paymentDate, value);
        }

        private DateTime? _processedDate;
        public DateTime? ProcessedDate
        {
            get => _processedDate;
            set => SetProperty(ref _processedDate, value);
        }

        // Способ оплаты
        private string _paymentMethod;
        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        // Статус
        private string _paymentStatus;
        public string PaymentStatus
        {
            get => _paymentStatus;
            set => SetProperty(ref _paymentStatus, value);
        }

        // Номер документа
        private string _receiptNumber;
        public string ReceiptNumber
        {
            get => _receiptNumber;
            set => SetProperty(ref _receiptNumber, value);
        }

        private string _bankTransactionId;
        public string BankTransactionId
        {
            get => _bankTransactionId;
            set => SetProperty(ref _bankTransactionId, value);
        }

        // Оператор
        private int? _operatorId;
        public int? OperatorId
        {
            get => _operatorId;
            set => SetProperty(ref _operatorId, value);
        }

        private Operator _operator;
        public Operator Operator
        {
            get => _operator;
            set => SetProperty(ref _operator, value);
        }

        public Payment()
        {
            PaymentDate = DateTime.Now;
            PaymentStatus = "ожидает_подтверждения";
            BankTransactionId = string.Empty;
        }
    }
}