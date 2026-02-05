using System;
using System.Linq;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;
using WpfSUB.Models;
using System.Collections.Generic;
using Timer = System.Timers.Timer;

namespace WpfSUB.Services
{
    public class SubscriptionAutoProcessor : IDisposable
    {
        private Timer _timer;
        private AppDbContext _context;

        public SubscriptionAutoProcessor()
        {
            _context = new AppDbContext();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _timer = new Timer(86400000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
            CheckAndProcessSubscriptions();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CheckAndProcessSubscriptions();
        }

        private void CheckAndProcessSubscriptions()
        {
            var today = DateTime.Today;
            if (today.Day > 15)
            {
                ProcessSubscriptionsForNextMonth(today);
            }
            CheckExpiredSubscriptions(today);
            CheckPaymentDeadlines(today);
        }

        private void ProcessSubscriptionsForNextMonth(DateTime today)
        {
            try
            {
                var nextMonth = today.AddMonths(1);
                var subscriptions = _context.Subscriptions
                    .Include(s => s.Publication)
                    .Include(s => s.Client)
                    .Where(s => s.Status == "оплачена" &&
                               s.PlannedStartDate.HasValue &&
                               s.PlannedStartDate.Value.Month == nextMonth.Month &&
                               s.PlannedStartDate.Value.Year == nextMonth.Year &&
                               !s.ActualStartDate.HasValue)
                    .ToList();

                foreach (var subscription in subscriptions)
                {
                    subscription.ActualStartDate = subscription.PlannedStartDate;
                    subscription.ActualEndDate = subscription.ActualStartDate.Value
                        .AddMonths(subscription.PeriodMonths)
                        .AddDays(-1);
                    subscription.Status = "активна";
                    CreateDeliverySchedule(subscription);
                }

                if (subscriptions.Any())
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception)
            {
            }
        }

        private void CreateDeliverySchedule(Subscription subscription)
        {
            if (subscription.ActualStartDate == null || subscription.ActualEndDate == null)
                return;

            var deliveryDate = subscription.ActualStartDate.Value;
            int issueNumber = 1;
            var deliveries = new List<Delivery>();

            string periodicity = subscription.Publication != null ? subscription.Publication.Periodicity : "ежемесячно";

            while (deliveryDate <= subscription.ActualEndDate.Value)
            {
                var delivery = new Delivery
                {
                    SubscriptionId = subscription.Id,
                    IssueNumber = issueNumber++,
                    IssueDate = deliveryDate,
                    ExpectedDeliveryDate = deliveryDate.AddDays(2),
                    DeliveryStatus = "запланирована"
                };
                deliveries.Add(delivery);

                if (periodicity == "ежедневно")
                {
                    deliveryDate = deliveryDate.AddDays(1);
                }
                else if (periodicity == "еженедельно")
                {
                    deliveryDate = deliveryDate.AddDays(7);
                }
                else if (periodicity == "ежемесячно")
                {
                    deliveryDate = deliveryDate.AddMonths(1);
                }
                else if (periodicity == "ежеквартально")
                {
                    deliveryDate = deliveryDate.AddMonths(3);
                }
                else
                {
                    deliveryDate = deliveryDate.AddMonths(1);
                }
            }

            _context.Deliveries.AddRange(deliveries);
        }

        private void CheckExpiredSubscriptions(DateTime today)
        {
            var expiredSubscriptions = _context.Subscriptions
                .Where(s => s.Status == "активна" &&
                           s.ActualEndDate.HasValue &&
                           s.ActualEndDate < today)
                .ToList();

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = "завершена";
            }

            if (expiredSubscriptions.Any())
            {
                _context.SaveChanges();
            }
        }

        private void CheckPaymentDeadlines(DateTime today)
        {
            var overdueSubscriptions = _context.Subscriptions
                .Where(s => s.Status == "ожидает_оплаты" &&
                           s.PaymentDeadline.HasValue &&
                           s.PaymentDeadline < today)
                .ToList();

            foreach (var subscription in overdueSubscriptions)
            {
                subscription.Status = "отменена";
            }

            if (overdueSubscriptions.Any())
            {
                _context.SaveChanges();
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _context?.Dispose();
        }
    }
}