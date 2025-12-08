using Microsoft.EntityFrameworkCore;
using WpfSUB.Models;

namespace WpfSUB.Data
{
    public class AppDbContext : DbContext
    {
        // Таблицы
        public DbSet<Category> Categories { get; set; }
        public DbSet<Publication> Publications { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<ClientProfile> ClientProfiles { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<AdditionalService> AdditionalServices { get; set; }
        public DbSet<SubscriptionServiceLink> SubscriptionServiceLinks { get; set; }
        public DbSet<Operator> Operators { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PostSubscriptionDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // 2. Publication - ИСПРАВЛЕНО!
            modelBuilder.Entity<Publication>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(e => e.ISSN)
                    .HasMaxLength(9);
                entity.Property(e => e.Periodicity)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasConversion<string>(); // Добавляем конвертацию для безопасности
                entity.Property(e => e.Publisher)
                    .IsRequired()
                    .HasMaxLength(300);
                entity.Property(e => e.Description)
                    .HasMaxLength(1000);
                entity.Property(e => e.IsAvailable)
                    .HasDefaultValue(true);
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.MonthlyPrice)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");
                entity.Property(e => e.QuarterlyPrice)
                    .HasColumnType("decimal(10,2)");
                entity.Property(e => e.YearlyPrice)
                    .HasColumnType("decimal(10,2)");
                entity.Property(e => e.PriceValidFrom)
                    .IsRequired();
                entity.Property(e => e.PriceValidTo)
                    .IsRequired();


                // Связь с Category
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Publications)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                //// Проверки - ИСПРАВЛЕНО!
                //entity.HasCheckConstraint("CK_Publication_Periodicity",
                //    "[Periodicity] IN ('ежедневно', 'еженедельно', 'ежемесячно', 'ежеквартально')");

                //entity.HasCheckConstraint("CK_Publication_ISSN",
                //    "[ISSN] IS NULL OR [ISSN] LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]'");
                //entity.HasCheckConstraint("CK_Publication_MonthlyPrice",
                //    "[MonthlyPrice] > 0");
            });

            // 3. Client
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.Address)
                    .HasMaxLength(500);
                entity.Property(e => e.Phone)
                    .HasMaxLength(20);
                entity.Property(e => e.Email)
                    .HasMaxLength(254);
                entity.Property(e => e.PassportSeries)
                    .HasMaxLength(4);
                entity.Property(e => e.PassportNumber)
                    .HasMaxLength(6);
                entity.Property(e => e.IssuedBy)
                    .HasMaxLength(500);
                entity.Property(e => e.RegistrationDate)
                    .HasDefaultValueSql("GETDATE()");

                //// Проверки
                //entity.HasCheckConstraint("CK_Client_FullName",
                //    "LEN([FullName]) >= 3");
                //entity.HasCheckConstraint("CK_Client_PassportSeries",
                //    "[PassportSeries] IS NULL OR [PassportSeries] LIKE '[0-9][0-9][0-9][0-9]'");
                //entity.HasCheckConstraint("CK_Client_PassportNumber",
                //    "[PassportNumber] IS NULL OR [PassportNumber] LIKE '[0-9][0-9][0-9][0-9][0-9][0-9]'");

                // Уникальный паспорт
                entity.HasIndex(e => new { e.PassportSeries, e.PassportNumber })
                    .IsUnique()
                    .HasFilter("[PassportSeries] IS NOT NULL AND [PassportNumber] IS NOT NULL");
            });

            // 4. ClientProfile (1:1 с Client)
            // 4. ClientProfile (1:1 с Client)
            modelBuilder.Entity<ClientProfile>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Установите значения по умолчанию для всех NOT NULL полей
                entity.Property(e => e.AvatarUrl)
                    .HasMaxLength(500)
                    .HasDefaultValue(""); // Значение по умолчанию

                entity.Property(e => e.Phone)
                    .HasMaxLength(20)
                    .HasDefaultValue(""); // Значение по умолчанию

                entity.Property(e => e.Bio)
                    .HasMaxLength(2000)
                    .HasDefaultValue(""); // Значение по умолчанию

                entity.Property(e => e.Preferences)
                    .HasMaxLength(1000)
                    .HasDefaultValue(""); // Значение по умолчанию

                // Birthday - nullable, не требует значения по умолчанию

                // Связь 1:1
                entity.HasOne(e => e.Client)
                    .WithOne(c => c.Profile)
                    .HasForeignKey<ClientProfile>(e => e.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 5. Subscription
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PeriodMonths)
                    .IsRequired();
                entity.Property(e => e.MonthlyPrice)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");
                entity.Property(e => e.TotalPrice)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("оформлена");
                entity.Property(e => e.IsFullyPaid)
                    .HasDefaultValue(false);

                // Связи
                entity.HasOne(e => e.Client)
                    .WithMany(c => c.Subscriptions)
                    .HasForeignKey(e => e.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Publication)
                    .WithMany(p => p.Subscriptions)
                    .HasForeignKey(e => e.PublicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                //// Проверки - ИСПРАВЛЕНО!
                //entity.HasCheckConstraint("CK_Subscription_Period",
                //    "[PeriodMonths] IN (1, 3, 6, 12, 24)"); // Добавил 1 месяц для гибкости

                //entity.HasCheckConstraint("CK_Subscription_TotalPrice",
                //    "[TotalPrice] > 0");
                //entity.HasCheckConstraint("CK_Subscription_Status",
                //    "[Status] IN ('оформлена', 'ожидает_оплаты', 'оплачена', 'активна', 'завершена', 'отменена')");
                //entity.HasCheckConstraint("CK_Subscription_Payment",
                //    "([IsFullyPaid] = 1 AND [PaidDate] IS NOT NULL) OR ([IsFullyPaid] = 0 AND [PaidDate] IS NULL)");
            });

            // 6. Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");
                entity.Property(e => e.PaymentDate)
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.PaymentMethod)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(e => e.PaymentStatus)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasDefaultValue("ожидает_подтверждения");
                entity.Property(e => e.ReceiptNumber)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.BankTransactionId)
                    .HasMaxLength(100);

                // Связи
                entity.HasOne(e => e.Subscription)
                    .WithMany(s => s.Payments)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Operator)
                    .WithMany(o => o.Payments)
                    .HasForeignKey(e => e.OperatorId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Проверки
                //entity.HasCheckConstraint("CK_Payment_Amount",
                //    "[Amount] > 0");
                //entity.HasCheckConstraint("CK_Payment_Method",
                //    "[PaymentMethod] IN ('наличные', 'карта_отделение', 'карта_онлайн', 'банковский_перевод')");
                //entity.HasCheckConstraint("CK_Payment_Status",
                //    "[PaymentStatus] IN ('ожидает_подтверждения', 'подтвержден', 'отклонен')");

                entity.HasIndex(e => e.ReceiptNumber).IsUnique();
            });

            // 7. Delivery
            modelBuilder.Entity<Delivery>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DeliveryStatus)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("запланирована");
                entity.Property(e => e.ExpectedDeliveryDate)
                    .IsRequired();
                entity.Property(e => e.IssueDate)
                    .IsRequired();
                entity.Property(e => e.ProblemReason)
                    .HasMaxLength(1000);

                // Связи
                entity.HasOne(e => e.Subscription)
                    .WithMany(s => s.Deliveries)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                //// Проверки
                //entity.HasCheckConstraint("CK_Delivery_Status",
                //    "[DeliveryStatus] IN ('запланирована', 'отправлено', 'в_пути', 'доставлено', 'возврат', 'отменена')");
            });

            // 8. AdditionalService
            modelBuilder.Entity<AdditionalService>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Description)
                    .HasMaxLength(1000);
                entity.Property(e => e.Price)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(e => e.Name).IsUnique();
            });

            // 9. SubscriptionServiceLink (M:M связь)
            modelBuilder.Entity<SubscriptionServiceLink>(entity =>
            {
                // Составной ключ
                entity.HasKey(e => new { e.SubscriptionId, e.ServiceId });

                entity.Property(e => e.Price)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");
                entity.Property(e => e.StartDate)
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.EndDate)
                    .IsRequired();

                // Связи
                entity.HasOne(e => e.Subscription)
                    .WithMany(s => s.ServiceLinks)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Service)
                    .WithMany(s => s.ServiceLinks)
                    .HasForeignKey(e => e.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 10. Operator
            modelBuilder.Entity<Operator>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Login)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.Login).IsUnique();
            });

            // Начальные данные (Seed data)
            var staticDate = new DateTime(2024, 1, 1, 10, 0, 0);

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Газеты",
                    CreatedDate = staticDate
                },
                new Category
                {
                    Id = 2,
                    Name = "Журналы",
                    CreatedDate = staticDate
                },
                new Category
                {
                    Id = 3,
                    Name = "Научные издания",
                    CreatedDate = staticDate
                }
            );

            // Убедитесь, что значения Periodicity точно соответствуют CHECK constraint
            modelBuilder.Entity<Publication>().HasData(
                new Publication
                {
                    Id = 1,
                    Title = "Известия",
                    ISSN = "1234-5678",
                    Periodicity = "ежедневно",  // Должно быть в CHECK constraint
                    CategoryId = 1,
                    Publisher = "Издательский дом",
                    Description = "Ежедневная общественно-политическая газета",
                    MonthlyPrice = 100.00m,
                    QuarterlyPrice = 280.00m,
                    YearlyPrice = 1080.00m,
                    IsAvailable = true,
                    CreatedDate = staticDate,
                    PriceValidFrom = staticDate,
                    PriceValidTo = new DateTime(2024, 12, 31, 23, 59, 59)
                },
                new Publication
                {
                    Id = 2,
                    Title = "Наука и жизнь",
                    ISSN = "2345-6789",
                    Periodicity = "ежемесячно",  // Должно быть в CHECK constraint
                    CategoryId = 3,
                    Publisher = "Научное издательство",
                    Description = "Научно-популярный журнал для широкого круга читателей",
                    MonthlyPrice = 200.00m,
                    QuarterlyPrice = 570.00m,
                    YearlyPrice = 2280.00m,
                    IsAvailable = true,
                    CreatedDate = staticDate,
                    PriceValidFrom = staticDate,
                    PriceValidTo = new DateTime(2024, 12, 31, 23, 59, 59)
                }
            );

            modelBuilder.Entity<Operator>().HasData(
                new Operator
                {
                    Id = 1,
                    Login = "operator1",
                    FullName = "Иванов Иван Иванович",
                    Password = "password123",
                    CreatedAt = staticDate
                }
            );

            modelBuilder.Entity<AdditionalService>().HasData(
                new AdditionalService
                {
                    Id = 1,
                    Name = "Доставка на дом",
                    Description = "Доставка прямо к вам домой в удобное время",
                    Price = 100.00m,
                    IsActive = true
                },
                new AdditionalService
                {
                    Id = 2,
                    Name = "Упаковка в пленку",
                    Description = "Защитная упаковка от влаги и повреждений при транспортировке",
                    Price = 50.00m,
                    IsActive = true
                }
            );
        }
    }
}