using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using WpfSUB.Models;
using WpfSUB.Services;
using System.Linq;

namespace WpfSUB.Pages
{
    public partial class PublicationPage : Page
    {
        private PublicationService _publicationService;
        private ObservableCollection<Publication> _publications;

        public PublicationPage()
        {
            InitializeComponent();
            _publicationService = new PublicationService();
            LoadPublications();
        }

        private void LoadPublications()
        {
            _publications = new ObservableCollection<Publication>(_publicationService.Publications);
            PublicationsListView.ItemsSource = _publications;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = PublicationsListView.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private void PublicationsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            MessageBox.Show("Добавление издания - будет реализовано");
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (PublicationsListView.SelectedItem is Publication selectedPublication)
            {
                MessageBox.Show($"Редактирование издания: {selectedPublication.Title} - будет реализовано");
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (PublicationsListView.SelectedItem is Publication selectedPublication)
            {
                var result = MessageBox.Show($"Удалить издание {selectedPublication.Title}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _publicationService.Remove(selectedPublication);
                    _publications.Remove(selectedPublication);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPublications();
        }
    }
}