using System;
using System.Collections.ObjectModel;

namespace WpfSUB.Models
{
    public class Operator : ObservableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _login;
        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private DateTime _createdAt;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        // Связи
        private ObservableCollection<Payment> _payments;
        public ObservableCollection<Payment> Payments
        {
            get => _payments;
            set => SetProperty(ref _payments, value);
        }

        public Operator()
        {
            Payments = new ObservableCollection<Payment>();
            CreatedAt = DateTime.Now;
        }
    }
}