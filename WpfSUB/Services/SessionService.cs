using System;
using WpfSUB.Models;

namespace WpfSUB.Services
{
    public static class SessionService
    {
        private static Operator _currentOperator;
        private static DateTime _loginTime;
        private static bool _isLoggedIn = false;

        public static Operator CurrentOperator
        {
            get => _currentOperator;
            set // Сделать set публичным
            {
                _currentOperator = value;
                _isLoggedIn = value != null;
                if (value != null)
                {
                    _loginTime = DateTime.Now;
                }
            }
        }

        public static bool IsLoggedIn => _isLoggedIn;

        public static TimeSpan SessionDuration => _isLoggedIn ? DateTime.Now - _loginTime : TimeSpan.Zero;

        public static string OperatorFullName => _currentOperator?.FullName ?? "Не авторизован";

        public static int? OperatorId => _currentOperator?.Id;

        public static void Login(Operator operatorUser)
        {
            if (operatorUser == null)
                throw new ArgumentNullException(nameof(operatorUser));

            CurrentOperator = operatorUser;
        }

        public static void Logout()
        {
            _currentOperator = null;
            _isLoggedIn = false;
        }

        public static bool CanPerformPayment()
        {
            // Все операторы могут выполнять платежи
            return IsLoggedIn;
        }

        public static bool CanManageOperators()
        {
            // Только администраторы могут управлять другими операторами
            return IsLoggedIn && _currentOperator?.Login == "admin";
        }

        public static void ValidateSession()
        {
            if (!IsLoggedIn)
            {
                throw new UnauthorizedAccessException("Требуется авторизация");
            }
        }
    }
}