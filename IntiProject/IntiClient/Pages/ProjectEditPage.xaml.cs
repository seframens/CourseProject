using ApiClient.Models.Lists;
using ApiClient.Models.Requests;
using ApiClient.Services;
using IntiClient.Events;
using System.Windows;
using System.Windows.Controls;

namespace IntiClient.Pages
{
    public partial class ProjectEditPage : Page
    {
        private readonly ApiService _apiService;
        private readonly int? _projectIdToEdit;

        private List<ManagerListModel> _allManagers = new List<ManagerListModel>();
        private List<WorkTypeListModel> _allWorkTypes = new List<WorkTypeListModel>();
        private Dictionary<string, int> _managerNameToId = new Dictionary<string, int>();
        private Dictionary<string, int> _workTypeNameToId = new Dictionary<string, int>();

        public ProjectEditPage(ApiService apiService, int? projectId)
        {
            InitializeComponent();
            _apiService = apiService;
            _projectIdToEdit = projectId;

            if (_projectIdToEdit.HasValue)
            {
                TitleTextBlock.Text = "Редактирование проекта";
            }
            else
            {
                TitleTextBlock.Text = "Создание проекта";
            }

            Loaded += ProjectEditPage_Loaded;
        }

        private async void ProjectEditPage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializePageAsync();
        }

        private async Task InitializePageAsync()
        {
            await LoadManagersAndWorkTypesAsync();
            if (_projectIdToEdit.HasValue)
            {
                await LoadProjectForEditing(_projectIdToEdit.Value);
            }
        }

        private async Task LoadManagersAndWorkTypesAsync()
        {
            try
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;

                var managersTask = _apiService.GetManagersListAsync();
                var workTypesTask = _apiService.GetWorkTypesListAsync();

                await Task.WhenAll(managersTask, workTypesTask);

                _allManagers = managersTask.Result ?? new List<ManagerListModel>();
                _allWorkTypes = workTypesTask.Result ?? new List<WorkTypeListModel>();

                _managerNameToId = _allManagers.ToDictionary(m => m.FullName, m => m.ProjectManagerId);
                _workTypeNameToId = _allWorkTypes.ToDictionary(wt => wt.Name, wt => wt.WorkTypeId);

                ManagerComboBox.ItemsSource = _allManagers.Select(m => m.FullName).ToList();
                WorkTypeComboBox.ItemsSource = _allWorkTypes.Select(wt => wt.Name).ToList();
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadProjectForEditing(int projectId)
        {
            try
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;

                var response = await _apiService.GetProjectByIdAsync(projectId);
                if (response != null)
                {
                    NameTextBox.Text = response.ProjectName;
                    DescriptionTextBox.Text = response.Description ?? string.Empty;
                    EmployerTextBox.Text = response.Employer;

                    if (!string.IsNullOrEmpty(response.ProjectManagerFullName))
                    {
                        ManagerComboBox.SelectedItem = response.ProjectManagerFullName;
                    }

                    if (!string.IsNullOrEmpty(response.WorkTypeName))
                    {
                        WorkTypeComboBox.SelectedItem = response.WorkTypeName;
                    }

                    ProjectDateDatePicker.SelectedDate = response.ProjectDate.ToDateTime(TimeOnly.MinValue);

                    foreach (ComboBoxItem item in StatusComboBox.Items)
                    {
                        if (item.Content?.ToString() == response.Status)
                        {
                            StatusComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                else
                {
                    ErrorTextBlock.Text = "Ошибка получения данных проекта.";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveProjectAsync();
        }

        private async Task SaveProjectAsync()
        {
            try
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = false;

                string name = NameTextBox.Text;
                string description = DescriptionTextBox.Text;
                string employer = EmployerTextBox.Text;
                string managerName = ManagerComboBox.SelectedItem?.ToString();
                string workTypeName = WorkTypeComboBox.SelectedItem?.ToString();
                DateTime? projectDate = ProjectDateDatePicker.SelectedDate;
                string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(employer) ||
                    string.IsNullOrWhiteSpace(managerName) ||
                    string.IsNullOrWhiteSpace(workTypeName) ||
                    !projectDate.HasValue || string.IsNullOrWhiteSpace(status))
                {
                    ShowError("Заполните все обязательные поля *");
                    return;
                }

                DateOnly projectDateOnly = DateOnly.FromDateTime(projectDate.Value);

                if (_projectIdToEdit.HasValue)
                {
                    await UpdateProjectAsync(name, description, employer, managerName, workTypeName, projectDateOnly, status);
                }
                else
                {
                    await CreateProjectAsync(name, description, employer, managerName, workTypeName, projectDateOnly, status);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private async Task UpdateProjectAsync(string name, string description, string employer,
            string managerName, string workTypeName, DateOnly projectDate, string status)
        {
            try
            {
                var updateData = new UpdateProjectRequestModel
                {
                    Name = name,
                    Description = description,
                    Employer = employer,
                    ProjectManagerFullName = managerName,
                    WorkTypeName = workTypeName,
                    ProjectDate = projectDate,
                    Status = status
                };

                var result = await _apiService.UpdateProjectAsync(_projectIdToEdit.Value, updateData);

                if (result.Success)
                {
                    MessageBox.Show("Проект успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    ProjectEvent.RaiseProjectUpdated(_projectIdToEdit.Value); 
                    ProjectEvent.RaiseProjectsUpdated();

                    NavigationService?.GoBack();
                }
                else
                {
                    ShowError(result.ErrorMessage ?? "Неизвестная ошибка при обновлении");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при обновлении: {ex.Message}");
            }
        }

        private async Task CreateProjectAsync(string name, string description, string employer,
            string managerName, string workTypeName, DateOnly projectDate, string status)
        {
            try
            {
                var createData = new CreateProjectRequestModel
                {
                    Name = name,
                    Description = description,
                    Employer = employer,
                    ProjectManagerFullName = managerName,
                    WorkTypeName = workTypeName,
                    ProjectDate = projectDate,
                    Status = status
                };

                var result = await _apiService.CreateProjectAsync(createData);

                if (result.Success)
                {
                    MessageBox.Show("Проект успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    ProjectEvent.RaiseProjectsUpdated();
                    NavigationService?.GoBack();
                }
                else
                {
                    ShowError(result.ErrorMessage ?? "Неизвестная ошибка при создании");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при создании: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
