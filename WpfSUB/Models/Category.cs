using System;
using System.Collections.ObjectModel;
using WpfSUB.Models;

namespace WpfSUB.Models
{
    public class Category : ObservableObject
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

        private DateTime _createdDate;
        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        private ObservableCollection<Publication> _publications;
        public ObservableCollection<Publication> Publications
        {
            get => _publications;
            set => SetProperty(ref _publications, value);
        }

        public Category()
        {
            Publications = new ObservableCollection<Publication>();
            CreatedDate = DateTime.Now;
        }
    }
}