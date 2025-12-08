using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace ApiClient.Models.Responses
{
    public class UserSession : INotifyPropertyChanged
    {
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static UserSession _instance = new UserSession();
        public static UserSession Instance
        {
            get => _instance;
            set
            {
                if (_instance != value)
                {
                    _instance = value;
                    OnStaticPropertyChanged(nameof(Instance));
                }
            }
        }

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    _fullName = value;
                    OnPropertyChanged();
                    OnStaticPropertyChanged(nameof(FullName));
                }
            }
        }

        private string _roleName;
        public string RoleName
        {
            get => _roleName;
            set
            {
                if (_roleName != value)
                {
                    _roleName = value;
                    OnPropertyChanged();
                    OnStaticPropertyChanged(nameof(RoleName));

                    OnStaticPropertyChanged(nameof(IsAdministrator));
                    OnStaticPropertyChanged(nameof(IsManager));
                    OnStaticPropertyChanged(nameof(IsGuest));
                    OnStaticPropertyChanged(nameof(HasAdminAccess));
                    OnStaticPropertyChanged(nameof(HasManagerAccess));
                    OnStaticPropertyChanged(nameof(HasGuestAccess));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void OnStaticPropertyChanged(string propertyName)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        public static void UpdateFromAuthResponse(AuthResponse authResponse)
        {
            if (authResponse.Success)
            {
                Instance ??= new UserSession();
                Instance.FullName = authResponse.FullName;
                Instance.RoleName = authResponse.RoleName;
            }
        }

        // Базовые свойства ролей
        public static bool IsAdministrator => Instance?.RoleName?.Equals("администратор", StringComparison.OrdinalIgnoreCase) == true;
        public static bool IsManager => Instance?.RoleName?.Equals("менеджер", StringComparison.OrdinalIgnoreCase) == true;
        public static bool IsGuest => Instance?.RoleName?.Equals("гость", StringComparison.OrdinalIgnoreCase) == true ||
                                    string.IsNullOrEmpty(Instance?.RoleName);

        // Свойства доступа с учетом иерархии (для привязок в XAML)
        public static bool HasAdminAccess => IsAdministrator;
        public static bool HasManagerAccess => IsAdministrator || IsManager;
        public static bool HasGuestAccess => IsAdministrator || IsManager || IsGuest;

        // Методы проверки доступа
        public static bool CheckAdminAccess() => IsAdministrator;
        public static bool CheckManagerAccess() => IsAdministrator || IsManager;
        public static bool CheckGuestAccess() => IsAdministrator || IsManager || IsGuest;

        // Универсальный метод проверки
        public static bool CheckAccess(bool requireAdmin = false, bool requireManager = false)
        {
            if (requireAdmin) return IsAdministrator;
            if (requireManager) return IsAdministrator || IsManager;
            return true; // Гостевой доступ
        }

        // Метод для выхода
        public static void Logout()
        {
            Instance.FullName = null;
            Instance.RoleName = null;
        }

        // Свойство для проверки аутентификации
        public static bool IsAuthenticated => !string.IsNullOrEmpty(Instance?.RoleName);
    }
}
