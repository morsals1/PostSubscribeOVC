using System;
using System.Collections.ObjectModel;
using WpfSUB.Models;

namespace WpfSUB.Models
{
    public class Client : ObservableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _address;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _phone;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        private string _email;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _passportSeries;
        public string PassportSeries
        {
            get => _passportSeries;
            set => SetProperty(ref _passportSeries, value);
        }

        private string _passportNumber;
        public string PassportNumber
        {
            get => _passportNumber;
            set => SetProperty(ref _passportNumber, value);
        }

        private string _issuedBy;
        public string IssuedBy
        {
            get => _issuedBy;
            set => SetProperty(ref _issuedBy, value);
        }

        private DateTime _registrationDate;
        public DateTime RegistrationDate
        {
            get => _registrationDate;
            set => SetProperty(ref _registrationDate, value);
        }

        // Связь 1:1 с профилем
        private ClientProfile _profile;
        public ClientProfile Profile
        {
            get => _profile;
            set => SetProperty(ref _profile, value);
        }

        // Связь 1:M с подписками
        private ObservableCollection<Subscription> _subscriptions;
        public ObservableCollection<Subscription> Subscriptions
        {
            get => _subscriptions;
            set => SetProperty(ref _subscriptions, value);
        }

        public Client()
        {
            Subscriptions = new ObservableCollection<Subscription>();
            RegistrationDate = DateTime.Now;
            Profile = new ClientProfile
            {
                AvatarUrl = "",
                Phone = "",
                Bio = "",
                Preferences = ""
            };
        }
    }
}