using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;
using WpfSUB.Models;

namespace WpfSUB.Services
{
    public class PublicationService
    {
        private readonly AppDbContext _db = BaseDbService.Instance.Context;

        public ObservableCollection<Publication> Publications { get; set; } = new();

        public PublicationService()
        {
            GetAll();
        }

        public int Commit() => _db.SaveChanges();

        public void GetAll()
        {
            var publications = _db.Publications
                .Include(p => p.Category)
                .ToList();

            Publications.Clear();
            foreach (var publication in publications)
            {
                Publications.Add(publication);
            }
        }

        public void GetAllWithRelations()
        {
            var publications = _db.Publications
                .Include(p => p.Category)
                .Include(p => p.Subscriptions)
                .ThenInclude(s => s.Client)
                .ToList();

            Publications.Clear();
            foreach (var publication in publications)
            {
                Publications.Add(publication);
            }
        }

        public void Add(Publication publication)
        {
            _db.Publications.Add(publication);
            Commit();
            Publications.Add(publication);
        }

        public void Update(Publication publication)
        {
            _db.Publications.Update(publication);
            Commit();
        }

        public void Remove(Publication publication)
        {
            _db.Publications.Remove(publication);
            if (Commit() > 0)
                Publications.Remove(publication);
        }

        public List<Publication> GetAvailablePublications()
        {
            return _db.Publications
                .Where(p => p.IsAvailable)
                .Include(p => p.Category)
                .ToList();
        }

        public decimal CalculatePrice(int publicationId, int months)
        {
            var publication = _db.Publications.Find(publicationId);
            if (publication == null) return 0;

            return publication.CalculatePriceForPeriod(months);
        }
    }
}