using System.Collections.ObjectModel;

namespace WpfSUB.Models
{
    public class AdditionalService : ObservableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // Связь M:M с Subscription через SubscriptionServiceLink
        private ObservableCollection<SubscriptionServiceLink> _serviceLinks;
        public ObservableCollection<SubscriptionServiceLink> ServiceLinks
        {
            get => _serviceLinks;
            set => SetProperty(ref _serviceLinks, value);
        }

        public AdditionalService()
        {
            ServiceLinks = new ObservableCollection<SubscriptionServiceLink>();
            IsActive = true;
        }
    }
}