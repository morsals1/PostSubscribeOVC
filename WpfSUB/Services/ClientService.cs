using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;
using WpfSUB.Models;

namespace WpfSUB.Services
{
    public class ClientService
    {
        private readonly AppDbContext _db = BaseDbService.Instance.Context;

        public ObservableCollection<Client> Clients { get; set; } = new();

        public ClientService()
        {
            GetAll();
        }

        public int Commit() => _db.SaveChanges();

        public void GetAll()
        {
            var clients = _db.Clients
                .Include(c => c.Profile)
                .Include(c => c.Subscriptions)
                .ThenInclude(s => s.Publication)
                .ToList();

            Clients.Clear();
            foreach (var client in clients)
            {
                Clients.Add(client);
            }
        }

        public Client GetById(int id)
        {
            return _db.Clients
                .Include(c => c.Profile)
                .Include(c => c.Subscriptions)
                .ThenInclude(s => s.Publication)
                .FirstOrDefault(c => c.Id == id);
        }

        public void Add(Client client)
        {
            if (client.Profile == null)
            {
                client.Profile = new ClientProfile();
            }

            client.Profile.AvatarUrl ??= "";
            client.Profile.Phone ??= "";
            client.Profile.Bio ??= "";
            client.Profile.Preferences ??= "";

            _db.Clients.Add(client);
            Commit();
            Clients.Add(client);
        }

        public void Update(Client client)
        {
            _db.Clients.Update(client);
            Commit();
        }

        public void Remove(Client client)
        {
            _db.Clients.Remove(client);
            if (Commit() > 0)
                Clients.Remove(client);
        }
    }
}