using System;
using System.Collections.ObjectModel;
using WpfSUB.Models;

namespace WpfSUB.Models
{
    public class Publication : ObservableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _issn;
        public string ISSN
        {
            get => _issn;
            set => SetProperty(ref _issn, value);
        }

        private string _periodicity;
        public string Periodicity
        {
            get => _periodicity;
            set => SetProperty(ref _periodicity, value);
        }

        private int _categoryId;
        public int CategoryId
        {
            get => _categoryId;
            set => SetProperty(ref _categoryId, value);
        }

        private Category _category;
        public Category Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private string _publisher;
        public string Publisher
        {
            get => _publisher;
            set => SetProperty(ref _publisher, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private bool _isAvailable;
        public bool IsAvailable
        {
            get => _isAvailable;
            set => SetProperty(ref _isAvailable, value);
        }

        private DateTime _createdDate;
        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        // Цены
        private decimal _monthlyPrice;
        public decimal MonthlyPrice
        {
            get => _monthlyPrice;
            set => SetProperty(ref _monthlyPrice, value);
        }

        private decimal? _quarterlyPrice;
        public decimal? QuarterlyPrice
        {
            get => _quarterlyPrice;
            set => SetProperty(ref _quarterlyPrice, value);
        }

        private decimal? _yearlyPrice;
        public decimal? YearlyPrice
        {
            get => _yearlyPrice;
            set => SetProperty(ref _yearlyPrice, value);
        }

        private DateTime? _priceValidFrom;
        public DateTime? PriceValidFrom
        {
            get => _priceValidFrom;
            set => SetProperty(ref _priceValidFrom, value);
        }

        private DateTime? _priceValidTo;
        public DateTime? PriceValidTo
        {
            get => _priceValidTo;
            set => SetProperty(ref _priceValidTo, value);
        }

        // Связи
        private ObservableCollection<Subscription> _subscriptions;
        public ObservableCollection<Subscription> Subscriptions
        {
            get => _subscriptions;
            set => SetProperty(ref _subscriptions, value);
        }

        public Publication()
        {
            Subscriptions = new ObservableCollection<Subscription>();
            IsAvailable = true;
            CreatedDate = DateTime.Now;
        }

        // Метод расчета цены для периода
        public decimal CalculatePriceForPeriod(int months)
        {
            // Применяем скидки за длительный период
            var basePrice = MonthlyPrice * months;

            return months switch
            {
                >= 24 => basePrice * 0.80m,  // 20% скидка за 2+ года
                >= 12 => basePrice * 0.90m,  // 10% скидка за год
                >= 6 => basePrice * 0.95m,   // 5% скидка за полгода
                _ => basePrice               // Без скидки
            };
        }
    }
}