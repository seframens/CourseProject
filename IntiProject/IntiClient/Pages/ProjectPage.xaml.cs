using ApiClient.Models.Lists;
using ApiClient.Models.Requests;
using ApiClient.Models.Responses;
using ApiClient.Services;
using IntiClient.Events;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;

namespace IntiClient.Pages
{
    /// <summary>
    /// Логика взаимодействия для ProjectPage.xaml
    /// </summary>
    public partial class ProjectPage : Page
    {
        private readonly ApiService _apiService;
        private readonly ObservableCollection<ProjectDetailsModel> _projects;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;

        private string? _currentStatusFilter;
        private string? _currentManagerFilter;
        private DateOnly? _currentStartDateFilter;
        private DateOnly? _currentEndDateFilter;
        private string? _currentSearchTermFilter;

        private List<StatusListModel> _allStatuses = new List<StatusListModel>();
        private List<ManagerListModel> _allManagers = new List<ManagerListModel>();

        private string? _currentSearchText;

        public ProjectPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            _projects = new ObservableCollection<ProjectDetailsModel>();
            ProjectsDataGrid.ItemsSource = _projects;

            LoadProjectsAsync(_currentPage);
            LoadFilterListsAsync();

            ProjectEvent.ProjectsUpdated += ProjectEvent_ProjectsUpdated;
        }
        private async Task LoadFilterListsAsync()
        {
            try
            {
                var statuses = await _apiService.GetStatusesListAsync();
                if (statuses != null)
                {
                    _allStatuses = statuses;
                    var allStatuses = new List<string> { "Все" };
                    allStatuses.AddRange(statuses.Select(s => s.Name));
                    StatusFilterComboBox.ItemsSource = allStatuses;
                    StatusFilterComboBox.SelectedIndex = 0;
                }

                var managers = await _apiService.GetManagersListAsync();
                if (managers != null)
                {
                    _allManagers = managers;
                    var allManagers = new List<string> { "Все" };
                    allManagers.AddRange(managers.Select(m => m.FullName));
                    ManagerFilterComboBox.ItemsSource = allManagers;
                    ManagerFilterComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списков для фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ProjectEvent_ProjectsUpdated()
        {
            await LoadProjectsAsync(_currentPage);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ProjectEvent.ProjectsUpdated -= ProjectEvent_ProjectsUpdated;
        }

        private async Task LoadProjectsAsync(int page, string? statusFilter = null, string? managerFilter = null, DateOnly? startDateFilter = null, DateOnly? endDateFilter = null, string? searchTermFilter = null)
        {
            try
            {
                var response = await _apiService.GetProjectsAsync(
                    page: page,
                    projectStatus: statusFilter,
                    projectManager: managerFilter,
                    projectDateFrom: startDateFilter,
                    projectDateTo: endDateFilter,
                    projectSearch_text: searchTermFilter
                );

                if (response != null)
                {
                    _projects.Clear();
                    foreach (var item in response.Items)
                    {
                        _projects.Add(item);
                    }
                    _currentPage = response.Page;
                    _totalPages = response.TotalPages;
                    _totalCount = response.TotalCount;

                    UpdatePaginationControls();
                    UpdateProjectCountLabel();
                }
                else
                {
                    MessageBox.Show("Ошибка получения списка проектов от сервера.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки проектов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProjectCountLabel()
        {
            ProjectCountLabel.Text = $"Всего проектов: {_totalCount} | Показано: {_projects.Count}";
        }

        private void ProjectName_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int projectId)
            {
                NavigationService?.Navigate(new ProjectDetailsPage(_apiService, projectId));
            }
        }

        private void UpdatePaginationControls()
        {
            PageInfoLabel.Content = $"Стр. {_currentPage} из {_totalPages}";
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
        }

        private void AddProjectButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ProjectEditPage(_apiService, null));
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Logout();

            NavigationService.GoBack();
        }

        private void FiltersButton_Click(object sender, RoutedEventArgs e)
        {
            FiltersPanel.Visibility = FiltersPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            FiltersButton.Content = FiltersPanel.Visibility == Visibility.Collapsed ? "Фильтры" : "Скрыть фильтры";
        }

        private async void ApplyFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            string? status = StatusFilterComboBox.Text;
            if (status == "Все")
                status = null;

            string? manager = ManagerFilterComboBox.Text;
            if (manager == "Все")
                manager = null;

            DateOnly? startDate = StartDatePicker.SelectedDate.HasValue ? DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value) : (DateOnly?)null;
            DateOnly? endDate = EndDatePicker.SelectedDate.HasValue ? DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value) : (DateOnly?)null;

            _currentStatusFilter = status;
            _currentManagerFilter = manager;
            _currentStartDateFilter = startDate;
            _currentEndDateFilter = endDate;

            _currentPage = 1;

            await LoadProjectsAsync(_currentPage, _currentStatusFilter, _currentManagerFilter, _currentStartDateFilter, _currentEndDateFilter, _currentSearchTermFilter);
        }


        private async void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            StatusFilterComboBox.SelectedIndex = 0;
            ManagerFilterComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            SearchTextBox.Text = string.Empty;

            _currentStatusFilter = null;
            _currentManagerFilter = null;
            _currentStartDateFilter = null;
            _currentEndDateFilter = null;
            _currentSearchTermFilter = null;

            _currentPage = 1;

            await LoadProjectsAsync(_currentPage, _currentStatusFilter, _currentManagerFilter, _currentStartDateFilter, _currentEndDateFilter, _currentSearchTermFilter);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int projectId)
            {
                NavigationService?.Navigate(new ProjectEditPage(_apiService, projectId));
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int projectId)
            {
                if (MessageBox.Show($"Удалить проект с ID {projectId}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    bool success = await _apiService.DeleteProjectAsync(projectId);
                    if (success)
                    {
                        ProjectEvent_ProjectsUpdated();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка удаления проекта.");
                    }
                }
            }
        }

        private async void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                await LoadProjectsAsync(_currentPage - 1);
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                await LoadProjectsAsync(_currentPage + 1);
            }
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearchTermFilter = string.IsNullOrWhiteSpace(SearchTextBox.Text) ? null : SearchTextBox.Text;

            _currentPage = 1;

            await LoadProjectsAsync(_currentPage, _currentStatusFilter, _currentManagerFilter, _currentStartDateFilter, _currentEndDateFilter, _currentSearchTermFilter);
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Выберите CSV-файл для импорта"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    await ProcessCsvImportAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}");
            }
        }

        private async Task ProcessCsvImportAsync(string filePath)
        {
            try
            {
                using (TextFieldParser parser = new TextFieldParser(filePath, System.Text.Encoding.UTF8))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(";");
                    parser.HasFieldsEnclosedInQuotes = false;
                    parser.TrimWhiteSpace = true;

                    string[] headers = parser.ReadFields();
                    if (headers == null || headers.Length < 7)
                    {
                        MessageBox.Show("CSV-файл должен содержать заголовки для Name, Description, Employer, ProjectManagerFullName, WorkTypeName, ProjectDate, Status.", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int nameIndex = Array.IndexOf(headers, "Name");
                    int descIndex = Array.IndexOf(headers, "Description");
                    int employerIndex = Array.IndexOf(headers, "Employer");
                    int managerIndex = Array.IndexOf(headers, "ProjectManagerFullName");
                    int workTypeIndex = Array.IndexOf(headers, "WorkTypeName");
                    int dateIndex = Array.IndexOf(headers, "ProjectDate");
                    int statusIndex = Array.IndexOf(headers, "Status");

                    if (nameIndex == -1 || managerIndex == -1 || workTypeIndex == -1 || dateIndex == -1 || statusIndex == -1)
                    {
                        MessageBox.Show("CSV-файл должен содержать столбцы: Name, ProjectManagerFullName, WorkTypeName, ProjectDate, Status.", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int importedCount = 0;
                    int errorCount = 0;

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        if (fields == null || fields.Length < headers.Length)
                        {
                            continue;
                        }

                        string name = fields[nameIndex]?.Trim() ?? string.Empty;
                        string? description = descIndex >= 0 && descIndex < fields.Length ? fields[descIndex]?.Trim() : null;
                        string employer = employerIndex >= 0 && employerIndex < fields.Length ? fields[employerIndex]?.Trim() ?? string.Empty : string.Empty;
                        string projectManagerFullName = fields[managerIndex]?.Trim() ?? string.Empty;
                        string workTypeName = fields[workTypeIndex]?.Trim() ?? string.Empty;

                        string dateString = fields[dateIndex]?.Trim() ?? string.Empty;
                        DateOnly projectDate;
                        try
                        {
                            projectDate = DateOnly.ParseExact(dateString, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (FormatException)
                        {
                            MessageBox.Show($"Неверный формат даты '{dateString}' в строке файла. Ожидается dd.MM.yyyy.", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Warning);
                            errorCount++;
                            continue; 
                        }

                        string status = fields[statusIndex]?.Trim() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(projectManagerFullName) || string.IsNullOrWhiteSpace(workTypeName) || string.IsNullOrWhiteSpace(status))
                        {
                            MessageBox.Show($"Пустые обязательные поля в строке файла: Name='{name}', ProjectManagerFullName='{projectManagerFullName}', WorkTypeName='{workTypeName}', Status='{status}'", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Warning);
                            errorCount++;
                            continue; 
                        }

                        var createData = new CreateProjectRequestModel
                        {
                            Name = name,
                            Description = description,
                            Employer = employer,
                            ProjectManagerFullName = projectManagerFullName,
                            WorkTypeName = workTypeName,
                            ProjectDate = projectDate,
                            Status = status
                        };

                        var result = await _apiService.CreateProjectAsync(createData);

                        if (result.Success) 
                        {
                            importedCount++;
                        }
                        else
                        {
                            errorCount++;
                        }
                    }

                    MessageBox.Show($"Импорт завершён. Успешно: {importedCount}, Ошибок: {errorCount}.", "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadProjectsAsync(_currentPage, _currentStatusFilter, _currentManagerFilter, _currentStartDateFilter, _currentEndDateFilter, _currentSearchTermFilter);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении CSV-файла: {ex.Message}", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                var saveFileDialog = new SaveFileDialog()
                {
                    Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    Title = "Сохранить список проектов как PDF",

                    FileName = $"Projects_Page{_currentPage}_",
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    try
                    {
                        GeneratePdfReport(_projects.ToList(), filePath, _currentPage, _totalPages, _totalCount, _currentStatusFilter, _currentManagerFilter, _currentStartDateFilter, _currentEndDateFilter, _currentSearchTermFilter);
                        MessageBox.Show($"PDF-отчёт сохранён в {filePath}", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при создании PDF-файла: {ex.Message}", "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка: {ex.Message}");
            }
        }

        private void GeneratePdfReport(List<ProjectDetailsModel> projects, string filePath, int currentPage, int totalPages, int totalCount, string? statusFilter, string? managerFilter, DateOnly? startDateFilter, DateOnly? endDateFilter, string? searchTermFilter)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                    page.Header()
                        .Text($"Список проектов - Страница {currentPage} из {totalPages}")
                        .SemiBold().FontSize(14);

                    page.Content()
                        .PaddingVertical(0.5f, Unit.Centimetre)
                        .Column(column =>
                        {
                            if (!string.IsNullOrEmpty(statusFilter) || !string.IsNullOrEmpty(managerFilter) || startDateFilter.HasValue || endDateFilter.HasValue || !string.IsNullOrEmpty(searchTermFilter))
                            {
                                column.Item()
                                    .PaddingBottom(0.2f, Unit.Centimetre)
                                    .Text(text =>
                                    {
                                        text.Span("Применённые фильтры: ").SemiBold();
                                        if (!string.IsNullOrEmpty(statusFilter)) text.Span($"Статус: {statusFilter}; ");
                                        if (!string.IsNullOrEmpty(managerFilter)) text.Span($"Менеджер: {managerFilter}; ");
                                        if (startDateFilter.HasValue) text.Span($"Дата от: {startDateFilter.Value:dd.MM.yyyy}; ");
                                        if (endDateFilter.HasValue) text.Span($"Дата до: {endDateFilter.Value:dd.MM.yyyy}; ");
                                        if (!string.IsNullOrEmpty(searchTermFilter)) text.Span($"Поиск: {searchTermFilter}; ");
                                    });
                            }

                            column.Item()
                                .Element(SetTableStyle)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1); // ID
                                        columns.RelativeColumn(2); // Название
                                        columns.RelativeColumn(2); // Описание
                                        columns.RelativeColumn(1.5f); // Заказчик
                                        columns.RelativeColumn(1.5f); // Руководитель проекта
                                        columns.RelativeColumn(1.5f); // Тип работ
                                        columns.RelativeColumn(2); // Дата
                                        columns.RelativeColumn(1); // Статус
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("ID"); 
                                        header.Cell().Element(CellStyle).Text("Название"); 
                                        header.Cell().Element(CellStyle).Text("Описание"); 
                                        header.Cell().Element(CellStyle).Text("Заказчик");
                                        header.Cell().Element(CellStyle).Text("Руководитель проекта"); 
                                        header.Cell().Element(CellStyle).Text("Тип работ"); 
                                        header.Cell().Element(CellStyle).Text("Дата"); 
                                        header.Cell().Element(CellStyle).Text("Статус"); 

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black).EnsureSpace(5);
                                        }
                                    });

                                    foreach (var project in projects)
                                    {
                                        table.Cell().Element(CellStyle).Text(project.ProjectNumber.ToString()); 
                                        table.Cell().Element(CellStyle).Text(project.ProjectName);
                                        table.Cell().Element(CellStyle).Text(project.Description);
                                        table.Cell().Element(CellStyle).Text(project.Employer);
                                        table.Cell().Element(CellStyle).Text(project.ProjectManagerFullName); 
                                        table.Cell().Element(CellStyle).Text(project.WorkTypeName); 
                                        table.Cell().Element(CellStyle).Text(project.ProjectDate.ToString("dd.MM.yyyy")); 
                                        table.Cell().Element(CellStyle).Text(project.Status); 

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).EnsureSpace(5);
                                        }
                                    }
                                });

                            static IContainer SetTableStyle(IContainer container)
                            {
                                return container.Border(1).BorderColor(Colors.Grey.Lighten3).Padding(5).EnsureSpace(5);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Всего проектов на этой странице: {projects.Count} | Всего проектов: {totalCount} | Страница {currentPage} из {totalPages}");
                });
            })
            .GeneratePdf(filePath);
        }

        private void SortByDateAscButton_Click(object sender, RoutedEventArgs e)
        {
            var sortedList = _projects.OrderBy(p => p.ProjectDate).ToList();
            _projects.Clear();
            foreach (var project in sortedList)
            {
                _projects.Add(project);
            }
        }

        private void SortByDateDescButton_Click(object sender, RoutedEventArgs e)
        {
            var sortedList = _projects.OrderByDescending(p => p.ProjectDate).ToList();
            _projects.Clear();
            foreach (var project in sortedList)
            {
                _projects.Add(project);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var sortedList = _projects.OrderByDescending(p => p.ProjectNumber).ToList();
            _projects.Clear();
            foreach (var project in sortedList)
            {
                _projects.Add(project);
            }
        }
    }
}
