using WpfSUB.Data;

namespace WpfSUB.Services
{
    public class BaseDbService
    {
        private BaseDbService()
        {
            context = new AppDbContext();
        }

        private static BaseDbService? instance;

        public static BaseDbService Instance
        {
            get
            {
                if (instance == null)
                    instance = new BaseDbService();
                return instance;
            }
        }

        private AppDbContext context;
        public AppDbContext Context => context;
    }
}