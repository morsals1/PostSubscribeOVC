using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using WpfSUB.Data;
using WpfSUB.Models;
using WpfSUB.Services;

namespace WpfSUB.Pages
{
    public partial class LoginPage : Page
    {
        private AppDbContext _context;
        private string _enteredPassword = "";

        // ViewModel свойства для привязки
        public string OperatorLogin { get; set; }

        public LoginPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            Loaded += LoginPage_Loaded;
            DataContext = this;
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Установить фокус на поле логина при загрузке
            LoginTextBox.Focus();

            // Проверить, есть ли операторы в базе данных
            CheckOperatorsInDatabase();
        }

        private void CheckOperatorsInDatabase()
        {
            try
            {
                var operatorCount = _context.Operators.Count();
                if (operatorCount == 0)
                {
                    ShowInfoMessage("В системе нет операторов. Обратитесь к администратору.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка подключения к базе данных: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            // Сбросить ошибки
            HideErrorMessage();

            // Проверка логина
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
            {
                ShowErrorMessage("Введите логин оператора");
                LoginTextBox.Focus();
                return false;
            }

            // Проверка пароля
            if (string.IsNullOrWhiteSpace(_enteredPassword))
            {
                ShowErrorMessage("Введите пароль");
                PasswordBox.Focus();
                return false;
            }

            return true;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                // Поиск оператора в базе данных
                var login = LoginTextBox.Text.Trim();
                var password = _enteredPassword;

                var operatorUser = _context.Operators
                    .AsNoTracking() // Не отслеживать изменения
                    .FirstOrDefault(o => o.Login == login && o.Password == password);

                if (operatorUser == null)
                {
                    ShowErrorMessage("Неверный логин или пароль оператора");
                    StartErrorAnimation();
                    ClearPasswordField();
                    return;
                }

                // Успешная авторизация
                OnSuccessfulLogin(operatorUser);
            }
            catch (DbUpdateException ex)
            {
                ShowErrorMessage($"Ошибка базы данных: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка авторизации: {ex.Message}");
            }
        }

        private void OnSuccessfulLogin(Operator operatorUser)
        {
            // Сохраняем информацию о текущем операторе через SessionService
            SessionService.CurrentOperator = operatorUser;

            // Показать сообщение об успешном входе
            ShowSuccessMessage($"Добро пожаловать, {operatorUser.FullName}!");

            // Задержка перед переходом для отображения сообщения
            System.Threading.Thread.Sleep(300);

            // Переход на главную страницу
            NavigationService.Navigate(new MainPage());
        }

        private void ShowErrorMessage(string message)
        {
            ErrorMessageText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;

            // Визуальный эффект для кнопки
            StartErrorAnimation();
        }

        private void ShowSuccessMessage(string message)
        {
            // Можно использовать более красивый Toast, но для простоты используем MessageBox
            MessageBox.Show(message, "Успешный вход",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowInfoMessage(string message)
        {
            // Простое информационное сообщение в интерфейсе
            var infoTextBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.DarkBlue,
                Background = Brushes.LightBlue,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12
            };

            // Добавляем в StackPanel перед кнопкой входа
            var grid = Content as Grid;
            var border = grid?.Children.OfType<Border>().FirstOrDefault(b => b.CornerRadius.TopLeft == 20);
            if (border?.Child is Grid loginGrid)
            {
                var stackPanel = new StackPanel();
                stackPanel.Children.Add(infoTextBlock);

                // Заменяем текущий контент
                var originalContent = loginGrid.Children[3]; // Кнопка входа
                loginGrid.Children.RemoveAt(3);
                loginGrid.Children.Insert(3, stackPanel);

                // Добавляем кнопку обратно
                stackPanel.Children.Add(originalContent);

                // Автоматически скрыть через 5 секунд
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, args) =>
                {
                    loginGrid.Children.Remove(stackPanel);
                    loginGrid.Children.Insert(3, originalContent);
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void HideErrorMessage()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }

        private void StartErrorAnimation()
        {
            // Простая анимация для визуальной обратной связи
            var originalBrush = LoginButton.Background.Clone();
            var animation = new System.Windows.Media.Animation.ColorAnimation
            {
                To = Colors.Red,
                Duration = TimeSpan.FromSeconds(0.1),
                AutoReverse = true,
                RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(2)
            };

            LoginButton.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);

            // Восстановить оригинальный цвет после анимации
            var restoreTimer = new System.Windows.Threading.DispatcherTimer();
            restoreTimer.Interval = TimeSpan.FromSeconds(0.5);
            restoreTimer.Tick += (s, e) =>
            {
                LoginButton.Background = originalBrush;
                restoreTimer.Stop();
            };
            restoreTimer.Start();
        }

        private void ClearPasswordField()
        {
            PasswordBox.Password = "";
            _enteredPassword = "";
            UpdateLoginButtonState();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _enteredPassword = PasswordBox.Password;
            UpdateLoginButtonState();
        }

        private void UpdateLoginButtonState()
        {
            LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(LoginTextBox.Text) &&
                                   !string.IsNullOrWhiteSpace(_enteredPassword) &&
                                   _enteredPassword.Length >= 3;
        }

        // Обработчики для эффектов фокуса
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                textBox.BorderThickness = new Thickness(2);
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                textBox.BorderThickness = new Thickness(1);
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                passwordBox.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                passwordBox.BorderThickness = new Thickness(2);
            }
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                passwordBox.BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                passwordBox.BorderThickness = new Thickness(1);
            }
        }

        // Обработка нажатия Enter для входа
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter && LoginButton.IsEnabled)
            {
                LoginButton_Click(null, null);
                e.Handled = true;
            }
        }
    }
}