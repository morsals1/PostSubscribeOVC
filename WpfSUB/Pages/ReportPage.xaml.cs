using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;
using WpfSUB.Models;
using System.Collections.Generic;

namespace WpfSUB.Pages
{
    public partial class ReportPage : Page
    {
        private string _currentReportType = "";

        public ReportPage(string reportType = "")
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(reportType))
            {
                GenerateReport(reportType);
            }
        }

        private void ClearReport()
        {
            // Удаляем все элементы кроме первых двух (ReportTitle и ReportText)
            while (ReportContent.Children.Count > 2)
            {
                ReportContent.Children.RemoveAt(2);
            }
        }

        private void GenerateReport(string reportType)
        {
            ClearReport();
            _currentReportType = reportType;

            try
            {
                switch (reportType)
                {
                    case "pricelist":
                        GeneratePriceList();
                        break;
                    case "receipts":
                        GenerateReceiptsInfo();
                        break;
                    case "activesubscriptions":
                        GenerateActiveSubscriptions();
                        break;
                    case "pendingsubscriptions":
                        GeneratePendingSubscriptions();
                        break;
                    default:
                        ReportTitle.Text = "Отчеты системы";
                        ReportText.Text = "Выберите тип отчета для генерации";
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации отчета: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GeneratePriceList()
        {
            using (var context = new AppDbContext())
            {
                var publications = context.Publications
                    .Include(p => p.Category)
                    .Where(p => p.IsAvailable)
                    .OrderBy(p => p.Category.Name)
                    .ThenBy(p => p.Title)
                    .ToList();

                ReportTitle.Text = "Прайс-лист изданий";
                ReportText.Text = $"Всего изданий: {publications.Count}";

                if (!publications.Any())
                {
                    AddInfoText("Нет доступных изданий для отображения");
                    return;
                }

                // Группируем по категориям
                var categories = publications.GroupBy(p => p.Category?.Name ?? "Без категории");

                foreach (var category in categories)
                {
                    AddSectionHeader(category.Key);

                    foreach (var publication in category)
                    {
                        string priceInfo = $"{publication.MonthlyPrice:C}/мес.";
                        if (publication.QuarterlyPrice.HasValue)
                            priceInfo += $", {publication.QuarterlyPrice:C}/квартал";
                        if (publication.YearlyPrice.HasValue)
                            priceInfo += $", {publication.YearlyPrice:C}/год";

                        AddReportItem($"• {publication.Title} ({publication.ISSN}) - {priceInfo}");
                    }
                }
            }
        }

        private void GenerateReceiptsInfo()
        {
            ReportTitle.Text = "Квитанции подписок";
            ReportText.Text = "Для генерации квитанции перейдите в раздел 'Подписки'";

            using (var context = new AppDbContext())
            {
                var recentPayments = context.Payments
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s.Client)
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s.Publication)
                    .Where(p => p.PaymentStatus == "подтвержден")
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .ToList();

                if (recentPayments.Any())
                {
                    AddSectionHeader("Последние оплаченные подписки:");

                    foreach (var payment in recentPayments)
                    {
                        AddReportItem($"Квитанция №{payment.ReceiptNumber}: " +
                                     $"{payment.Subscription.Client.FullName} - " +
                                     $"{payment.Subscription.Publication.Title} - " +
                                     $"{payment.Amount:C} ({payment.PaymentDate:dd.MM.yyyy})");
                    }
                }
            }
        }

        private void GenerateActiveSubscriptions()
        {
            using (var context = new AppDbContext())
            {
                var activeSubscriptions = context.Subscriptions
                    .Include(s => s.Client)
                    .Include(s => s.Publication)
                        .ThenInclude(p => p.Category)
                    .Where(s => s.Status == "активна" && s.ActualEndDate >= DateTime.Today)
                    .OrderBy(s => s.ActualEndDate)
                    .ToList();

                ReportTitle.Text = "Активные подписки";
                ReportText.Text = $"Всего активных подписок: {activeSubscriptions.Count}";

                if (!activeSubscriptions.Any())
                {
                    AddInfoText("Нет активных подписок");
                    return;
                }

                // Статистика
                var totalRevenue = activeSubscriptions.Sum(s => s.TotalPrice);
                var averageDuration = activeSubscriptions.Average(s => s.PeriodMonths);

                AddSectionHeader("Статистика:");
                AddReportItem($"Общая выручка: {totalRevenue:C}");
                AddReportItem($"Средний период подписки: {averageDuration:F1} месяцев");
                AddReportItem($"Подписок истекает в этом месяце: " +
                            $"{activeSubscriptions.Count(s => s.ActualEndDate?.Month == DateTime.Today.Month)}");

                AddSectionHeader("Список активных подписок:");

                foreach (var subscription in activeSubscriptions)
                {
                    string statusInfo = "";
                    if (subscription.ActualEndDate.HasValue)
                    {
                        var daysRemaining = (subscription.ActualEndDate.Value - DateTime.Today).Days;
                        statusInfo = daysRemaining <= 30 ? $" (осталось {daysRemaining} дней)" : "";
                    }

                    AddReportItem($"• {subscription.Client.FullName}: " +
                                 $"{subscription.Publication.Title} - " +
                                 $"{subscription.PeriodMonths} мес. - " +
                                 $"{subscription.TotalPrice:C} " +
                                 $"(до {subscription.ActualEndDate:dd.MM.yyyy}){statusInfo}");
                }

                // Группировка по изданиям
                AddSectionHeader("Популярные издания:");
                var popularPublications = activeSubscriptions
                    .GroupBy(s => s.Publication.Title)
                    .OrderByDescending(g => g.Count())
                    .Take(5);

                foreach (var publication in popularPublications)
                {
                    AddReportItem($"• {publication.Key}: {publication.Count()} подписок");
                }
            }
        }

        private void GeneratePendingSubscriptions()
        {
            using (var context = new AppDbContext())
            {
                var pendingSubscriptions = context.Subscriptions
                    .Include(s => s.Client)
                    .Include(s => s.Publication)
                    .Where(s => s.Status == "ожидает_оплаты" &&
                              s.PaymentDeadline >= DateTime.Today)
                    .OrderBy(s => s.PaymentDeadline)
                    .ToList();

                ReportTitle.Text = "Подписки, ожидающие оплаты";
                ReportText.Text = $"Подписок к оплате: {pendingSubscriptions.Count}";

                if (!pendingSubscriptions.Any())
                {
                    AddInfoText("Нет подписок, ожидающих оплаты");
                    return;
                }

                var totalAmount = pendingSubscriptions.Sum(s => s.TotalPrice);
                AddSectionHeader($"Общая сумма к оплате: {totalAmount:C}");

                foreach (var subscription in pendingSubscriptions)
                {
                    var daysRemaining = subscription.PaymentDeadline.HasValue
                        ? (subscription.PaymentDeadline.Value - DateTime.Today).Days
                        : 0;

                    string urgency = daysRemaining <= 3 ? " (СРОЧНО!)" :
                                    daysRemaining <= 7 ? " (скоро истекает)" : "";

                    AddReportItem($"• {subscription.Client.FullName}: " +
                                 $"{subscription.Publication.Title} - " +
                                 $"{subscription.TotalPrice:C} " +
                                 $"(до {subscription.PaymentDeadline:dd.MM.yyyy}){urgency}");
                }
            }
        }

        private void AddSectionHeader(string text)
        {
            var header = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 15, 0, 5),
                Foreground = System.Windows.Media.Brushes.DarkBlue
            };
            ReportContent.Children.Add(header);
        }

        private void AddReportItem(string text)
        {
            var item = new TextBlock
            {
                Text = text,
                Margin = new Thickness(10, 2, 0, 2),
                TextWrapping = System.Windows.TextWrapping.Wrap
            };
            ReportContent.Children.Add(item);
        }

        private void AddInfoText(string text)
        {
            var info = new TextBlock
            {
                Text = text,
                FontStyle = System.Windows.FontStyles.Italic,
                Margin = new Thickness(0, 10, 0, 5),
                Foreground = System.Windows.Media.Brushes.Gray
            };
            ReportContent.Children.Add(info);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new MainPage());
        }

        private void PriceList_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport("pricelist");
        }

        private void Receipt_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport("receipts");
        }

        private void ActiveSubscriptions_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport("activesubscriptions");
        }

        private void PendingSubscriptions_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport("pendingsubscriptions");
        }

        private void ClearReport_Click(object sender, RoutedEventArgs e)
        {
            ClearReport();
            ReportTitle.Text = "Отчеты системы";
            ReportText.Text = "Выберите тип отчета для генерации";
            _currentReportType = "";
        }
    }
}