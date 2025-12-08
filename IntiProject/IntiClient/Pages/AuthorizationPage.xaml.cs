using ApiClient.Models.Responses;
using ApiClient.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using static System.Net.WebRequestMethods;

namespace IntiClient.Pages
{
    /// <summary>
    /// 
    /// </summary>
    public partial class AuthorizationPage : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly ApiService _apiService = new(new("http://192.168.1.72:8000/")); 

        public AuthorizationPage(MainWindow mainWindow)
        {
            InitializeComponent();

            _mainWindow = mainWindow;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password; 

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ErrorTextBlock.Text = "Логин и пароль обязательны.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            ErrorTextBlock.Visibility = Visibility.Collapsed;

            try
            {

                var result = await _apiService.LoginAsync(login, password);

                Debug.WriteLine($"Success: {result.Success}");
                Debug.WriteLine($"FullName: {result.FullName}");
                Debug.WriteLine($"RoleName: {result.RoleName}");
                Debug.WriteLine($"IsAdministrator: {result.IsAdministrator}");

                if (result != null && result.Success)
                {
                    _apiService.SetUserRole(result.RoleName);

                    ProjectPage projectPage = new ProjectPage(_apiService);
                    projectPage.DataContext = result;

                    NavigationService.Navigate(projectPage);

                    LoginTextBox.Clear();
                }
                else
                {
                    ErrorTextBlock.Text = result?.Message ?? "Неизвестная ошибка при входе";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Неверный логин или пароль";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }


        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var guestAuthResponse = new AuthResponse
                {
                    Success = true,
                    FullName = "Гость",
                    RoleName = "Гость" 
                };

                _apiService.SetUserRole("Гость");

                ProjectPage projectPage = new ProjectPage(_apiService);
                projectPage.DataContext = guestAuthResponse;

                NavigationService.Navigate(projectPage);

                LoginTextBox.Clear();
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = "Неизвестная ошибка при входе.";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }          
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы точно хотите выйти из приложения?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
