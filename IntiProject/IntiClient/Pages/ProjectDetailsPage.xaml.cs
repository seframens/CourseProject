using ApiClient.Services;
using IntiClient.Events;
using System.Windows;
using System.Windows.Controls;

namespace IntiClient.Pages
{
    /// <summary>
    /// Логика взаимодействия для ProjectDetailsPage.xaml
    /// </summary>
    public partial class ProjectDetailsPage : Page
    {
        private readonly ApiService _apiService;
        private readonly int _projectId;

        public ProjectDetailsPage(ApiService apiService, int projectId)
        {
            InitializeComponent();
            _apiService = apiService;
            _projectId = projectId;

            LoadProjectDetailsAsync(_projectId);

            ProjectEvent.ProjectUpdated += ProjectEvent_ProjectUpdated;
        }

        private async void ProjectEvent_ProjectUpdated(int updatedProjectId)
        {
            if (updatedProjectId == _projectId)
            {
                await Dispatcher.Invoke(async () =>
                {
                    await LoadProjectDetailsAsync(_projectId);
                });
            }
        }

        private async Task LoadProjectDetailsAsync(int projectId)
        {
            try
            {
                var project = await _apiService.GetProjectByIdAsync(projectId);

                if (project != null)
                {
                    ProjectNameTextBlock.Text = project.ProjectName;
                    DescriptionTextBlock.Text = project.Description ?? "Нет описания";
                    EmployerTextBlock.Text = project.Employer;
                    ManagerFullNameTextBlock.Text = project.ProjectManagerFullName;
                    ManagerQualificationTextBlock.Text = project.ProjectManagerQualification;
                    WorkTypeNameTextBlock.Text = project.WorkTypeName;
                    ProjectDateTextBlock.Text = project.ProjectDate.ToString("dd.MM.yyyy");
                    StatusTextBlock.Text = project.Status;
                    ProjectIdTextBlock.Text = project.ProjectNumber.ToString();
                }
                else
                {
                    ErrorTextBlock.Text = "Проект не найден";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Ошибка загрузки проекта: {ex.Message}";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void EditProjectButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ProjectEditPage(_apiService, _projectId));
        }

        private async void DeleteProjectButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить проект с ID {_projectId}?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _apiService.DeleteProjectAsync(_projectId);
                    if (success)
                    {
                        ProjectEvent.RaiseProjectsUpdated();
                        NavigationService?.GoBack();
                    }
                    else
                    {
                        ErrorTextBlock.Text = "Ошибка удаления проекта (сервер вернул неуспех).";
                        ErrorTextBlock.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    ErrorTextBlock.Text = $"Ошибка при удалении проекта: {ex.Message}";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
