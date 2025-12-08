using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;
using WpfSUB.Models;

namespace WpfSUB.Services
{
    public class SubscriptionService
    {
        private readonly AppDbContext _db = BaseDbService.Instance.Context;

        public ObservableCollection<Subscription> Subscriptions { get; set; } = new();

        public SubscriptionService()
        {
            GetAll();
        }

        public int Commit() => _db.SaveChanges();

        public void GetAll()
        {
            var subscriptions = _db.Subscriptions
                .Include(s => s.Client)
                .Include(s => s.Publication)
                .ThenInclude(p => p.Category)
                .Include(s => s.Payments)
                .Include(s => s.Deliveries)
                .Include(s => s.ServiceLinks)
                .ThenInclude(ssl => ssl.Service)
                .ToList();

            Subscriptions.Clear();
            foreach (var subscription in subscriptions)
            {
                Subscriptions.Add(subscription);
            }
        }

        public Subscription CreateSubscription(int clientId, int publicationId, int months,
                                             DateTime plannedStartDate, decimal totalPrice)
        {
            var publication = _db.Publications.Find(publicationId);
            if (publication == null)
                throw new Exception("Издание не найдено");

            if (!publication.IsAvailable)
                throw new Exception("Издание временно недоступно для подписки");

            var subscription = new Subscription
            {
                ClientId = clientId,
                PublicationId = publicationId,
                PeriodMonths = months,
                MonthlyPrice = publication.MonthlyPrice,
                TotalPrice = totalPrice, // ← используем переданную сумму
                Status = "оформлена",
                PlannedStartDate = plannedStartDate,
                IsFullyPaid = false,
                PaymentDeadline = DateTime.Today.AddDays(10),
                CreatedDate = DateTime.Now
            };

            subscription.CalculateDates();
            subscription.Status = "ожидает_оплаты";

            _db.Subscriptions.Add(subscription);

            try
            {
                Commit();

                // Загружаем полные данные для коллекции
                var subscriptionWithDetails = _db.Subscriptions
                    .Include(s => s.Client)
                    .Include(s => s.Publication)
                    .FirstOrDefault(s => s.Id == subscription.Id);

                if (subscriptionWithDetails != null)
                {
                    Subscriptions.Add(subscriptionWithDetails);
                }

                return subscription;
            }
            catch (Exception ex)
            {
                _db.Entry(subscription).State = EntityState.Detached;
                throw new Exception($"Ошибка создания подписки: {ex.Message}", ex);
            }
        }

        public Payment AddPayment(int subscriptionId, decimal amount, string paymentMethod, string receiptNumber)
        {
            var subscription = _db.Subscriptions.Find(subscriptionId);
            if (subscription == null)
                throw new Exception("Подписка не найдена");

            if (subscription.Status != "ожидает_оплаты")
                throw new Exception("Подписка не ожидает оплаты");

            if (amount != subscription.TotalPrice)
                throw new Exception($"Требуется полная оплата: {subscription.TotalPrice} руб.");

            if (subscription.PaymentDeadline.HasValue && subscription.PaymentDeadline < DateTime.Today)
                throw new Exception("Срок оплаты истек");

            var payment = new Payment
            {
                SubscriptionId = subscriptionId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                ReceiptNumber = receiptNumber,
                PaymentStatus = "ожидает_подтверждения"
            };

            _db.Payments.Add(payment);
            Commit();

            return payment;
        }

        public void ConfirmPayment(int paymentId, int operatorId)
        {
            var payment = _db.Payments
                .Include(p => p.Subscription)
                .ThenInclude(s => s.Publication)
                .FirstOrDefault(p => p.Id == paymentId);

            if (payment == null)
                throw new Exception("Платеж не найден");

            var subscription = payment.Subscription;

            // Подтверждаем платеж
            payment.PaymentStatus = "подтвержден";
            payment.ProcessedDate = DateTime.Now;
            payment.OperatorId = operatorId;

            // Обновляем подписку
            subscription.IsFullyPaid = true;
            subscription.PaidDate = DateTime.Now;
            subscription.Status = "оплачена";

            // Устанавливаем фактические даты
            subscription.ActualStartDate = GetNextFirstOfMonth();
            subscription.ActualEndDate = subscription.ActualStartDate.Value
                .AddMonths(subscription.PeriodMonths)
                .AddDays(-1);

            Commit();
        }

        public void ActivateSubscription(int subscriptionId)
        {
            var subscription = _db.Subscriptions.Find(subscriptionId);
            if (subscription == null)
                throw new Exception("Подписка не найдена");

            if (!subscription.CanBeActivated())
                throw new Exception("Подписка не может быть активирована");

            subscription.Status = "активна";

            // Создаем расписание доставок
            ScheduleDeliveries(subscription);

            Commit();
        }

        private DateTime GetNextFirstOfMonth()
        {
            var today = DateTime.Today;
            return new DateTime(today.Year, today.Month, 1).AddMonths(1);
        }

        private void ScheduleDeliveries(Subscription subscription)
        {
            var publication = subscription.Publication;
            DateTime deliveryDate = subscription.ActualStartDate.Value;
            int issueNumber = 1;

            while (deliveryDate <= subscription.ActualEndDate)
            {
                var delivery = new Delivery
                {
                    SubscriptionId = subscription.Id,
                    IssueNumber = issueNumber++,
                    IssueDate = deliveryDate,
                    ExpectedDeliveryDate = deliveryDate.AddDays(2), // +2 дня на доставку
                    DeliveryStatus = "запланирована"
                };

                _db.Deliveries.Add(delivery);

                // Следующая доставка в зависимости от периодичности
                deliveryDate = publication.Periodicity switch
                {
                    "ежедневно" => deliveryDate.AddDays(1),
                    "еженедельно" => deliveryDate.AddDays(7),
                    "ежемесячно" => deliveryDate.AddMonths(1),
                    "ежеквартально" => deliveryDate.AddMonths(3),
                    _ => deliveryDate.AddMonths(1)
                };
            }
        }

        public List<Subscription> GetClientSubscriptions(int clientId)
        {
            return _db.Subscriptions
                .Where(s => s.ClientId == clientId)
                .Include(s => s.Publication)
                .ThenInclude(p => p.Category)
                .Include(s => s.Payments)
                .Include(s => s.Deliveries)
                .ToList();
        }

        public List<Subscription> GetActiveSubscriptions()
        {
            return _db.Subscriptions
                .Where(s => s.Status == "активна" &&
                           s.ActualEndDate >= DateTime.Today)
                .Include(s => s.Client)
                .Include(s => s.Publication)
                .ToList();
        }

        public List<Subscription> GetSubscriptionsAwaitingPayment()
        {
            return _db.Subscriptions
                .Where(s => s.Status == "ожидает_оплаты" &&
                           s.PaymentDeadline >= DateTime.Today)
                .Include(s => s.Client)
                .Include(s => s.Publication)
                .ToList();
        }

        public void CancelOverdueSubscriptions()
        {
            var overdueSubscriptions = _db.Subscriptions
                .Where(s => s.Status == "ожидает_оплаты" &&
                           s.PaymentDeadline < DateTime.Today.AddDays(-3) &&
                           !s.IsFullyPaid)
                .ToList();

            foreach (var subscription in overdueSubscriptions)
            {
                subscription.Status = "отменена";
            }

            if (overdueSubscriptions.Any())
                Commit();
        }
        public void UpdateSubscription(Subscription subscription)
        {
            var existingSubscription = _db.Subscriptions.Find(subscription.Id);
            if (existingSubscription == null)
                throw new Exception("Подписка не найдена");

            // Обновляем ВСЕ поля
            existingSubscription.ClientId = subscription.ClientId;
            existingSubscription.PublicationId = subscription.PublicationId;
            existingSubscription.PeriodMonths = subscription.PeriodMonths;
            existingSubscription.MonthlyPrice = subscription.MonthlyPrice;
            existingSubscription.TotalPrice = subscription.TotalPrice;
            existingSubscription.Status = subscription.Status;
            existingSubscription.IsFullyPaid = subscription.IsFullyPaid;
            existingSubscription.PaidDate = subscription.PaidDate;
            existingSubscription.PlannedStartDate = subscription.PlannedStartDate;
            existingSubscription.PlannedEndDate = subscription.PlannedEndDate;
            existingSubscription.ActualStartDate = subscription.ActualStartDate;
            existingSubscription.ActualEndDate = subscription.ActualEndDate;
            existingSubscription.PaymentDeadline = subscription.PaymentDeadline;

            // Пересчитываем даты если изменился период или дата начала
            existingSubscription.CalculateDates();

            try
            {
                Commit();

                // Обновляем в ObservableCollection
                var index = Subscriptions.IndexOf(Subscriptions.FirstOrDefault(s => s.Id == subscription.Id));
                if (index >= 0)
                {
                    Subscriptions[index] = existingSubscription;
                }
            }
            catch (DbUpdateException ex)
            {
                string errorMessage = $"Ошибка при обновлении подписки: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nДетали: {ex.InnerException.Message}";
                }
                throw new Exception(errorMessage, ex);
            }
        }

        public void CompleteExpiredSubscriptions()
        {
            var expiredSubscriptions = _db.Subscriptions
                .Where(s => s.Status == "активна" &&
                           s.ActualEndDate < DateTime.Today)
                .ToList();

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = "завершена";
            }

            if (expiredSubscriptions.Any())
                Commit();
        }
    }
}