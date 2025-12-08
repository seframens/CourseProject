using ApiClient.Models.Lists;
using ApiClient.Models.Requests;
using ApiClient.Models.Responses;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ApiClient.Services
{
    public class ApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private string? _userRole;

        public string? UserRole => _userRole;

        public ApiService(Uri baseAddress)
        {
            _httpClient = new HttpClient { BaseAddress = baseAddress };
        }

        public void SetUserRole(string role)
        {
            _userRole = role;
        }

        public async Task<AuthResponse?> LoginAsync(string login, string password)
        {
            var loginData = new { Login = login, Password = password };
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (authResponse != null && authResponse.Success)
                {
                    UserSession.UpdateFromAuthResponse(authResponse);
                    SetUserRole(authResponse.RoleName);
                }
                return authResponse;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new AuthResponse { Success = false, Message = errorResponse?.Detail ?? $"Ошибка: {response.StatusCode}" };
                }
                catch
                {
                    return new AuthResponse { Success = false, Message = $"Ошибка: {response.StatusCode}" };
                }
            }
        }

        public async Task<PaginatedResponse?> GetProjectsAsync(int page = 1,
            string? projectStatus = null,
            string? projectManager = null, 
            DateOnly? projectDateFrom = null,
            DateOnly? projectDateTo = null, 
            string? projectSearch_text = null)
        {
            var requestData = new
            {
                status = projectStatus,
                project_manager = projectManager,
                project_date_from = projectDateFrom?.ToString("yyyy-MM-dd"),
                project_date_to = projectDateTo?.ToString("yyyy-MM-dd"),
                search_text = projectSearch_text
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"projects/details?page={page}", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[DEBUG ApiService] GetProjectsAsync response: {responseContent}");

                var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return paginatedResponse;
            }
            else
            {
                Console.WriteLine($"GetProjectsAsync failed: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error content: {errorContent}"); 
                return null;
            }
        }

        public async Task<ProjectDetailsModel?> GetProjectByIdAsync(int projectId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"projects/{projectId}");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var projectDetails = JsonSerializer.Deserialize<ProjectDetailsModel>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return projectDetails;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG ApiService] Project with ID {projectId} not found (404).");
                    return null;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR ApiService] GetProjectById failed with status: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR ApiService] Exception in GetProjectById: {ex.Message}");
                return null;
            }
        }

        public async Task<List<ManagerListModel>?> GetManagersListAsync()
        {
            var response = await _httpClient.GetAsync("managers/list");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[DEBUG ApiService] Raw Managers Response: {responseContent}");

                var list = JsonSerializer.Deserialize<List<ManagerListModel>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Debug.WriteLine($"[DEBUG ApiService] Deserialized Managers Count: {list?.Count ?? 0}");
                if (list != null)
                {
                    foreach (var manager in list)
                    {
                        Debug.WriteLine($"[DEBUG ApiService] Manager FullName: '{manager.FullName}', Qualification: '{manager.Qualification}'"); // Данные объекта
                    }
                }

                return list;
            }
            else
            {
                Console.WriteLine($"GetManagersList failed: {response.StatusCode}");
                return null;
            }
        }

        public async Task<List<WorkTypeListModel>?> GetWorkTypesListAsync()
        {
            var response = await _httpClient.GetAsync("work-types/list");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<WorkTypeListModel>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                Console.WriteLine($"GetWorkTypesList failed: {response.StatusCode}");
                return null;
            }
        }

        public async Task<List<StatusListModel>?> GetStatusesListAsync()
        {
            var response = await _httpClient.GetAsync("statuses/list");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<StatusListModel>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                Console.WriteLine($"GetStatusesList failed: {response.StatusCode}");
                return null;
            }
        }

        public async Task<OperationResult> CreateProjectAsync(CreateProjectRequestModel projectData)
        {
            try
            {
                var json = JsonSerializer.Serialize(projectData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("projects/create", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    return OperationResult.SuccessResult();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        using var doc = JsonDocument.Parse(errorContent);
                        if (doc.RootElement.TryGetProperty("detail", out var detailElement))
                        {
                            if (detailElement.ValueKind == JsonValueKind.Array && detailElement.GetArrayLength() > 0)
                            {
                                var firstError = detailElement[0];
                                if (firstError.TryGetProperty("msg", out var msgElement))
                                {
                                    return OperationResult.FailureResult(msgElement.GetString() ?? "Неизвестная ошибка валидации.");
                                }
                            }
                            else if (detailElement.ValueKind == JsonValueKind.String)
                            {
                                return OperationResult.FailureResult(detailElement.GetString() ?? "Ошибка сервера.");
                            }
                        }
                        return OperationResult.FailureResult($"Ошибка сервера: {response.StatusCode}. {errorContent}");
                    }
                    catch
                    {
                        return OperationResult.FailureResult($"Ошибка сервера: {response.StatusCode}. {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                return OperationResult.FailureResult($"Ошибка подключения: {ex.Message}");
            }
        }

        public async Task<OperationResult> UpdateProjectAsync(int projectId, UpdateProjectRequestModel projectData)
        {
            try
            {
                var json = JsonSerializer.Serialize(projectData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"projects/{projectId}", content);

                if (response.IsSuccessStatusCode)
                {
                    return OperationResult.SuccessResult();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(errorContent);
                        if (doc.RootElement.TryGetProperty("detail", out var detailElement))
                        {
                            if (detailElement.ValueKind == JsonValueKind.Array && detailElement.GetArrayLength() > 0)
                            {
                                var firstError = detailElement[0];
                                if (firstError.TryGetProperty("msg", out var msgElement))
                                {
                                    return OperationResult.FailureResult(msgElement.GetString() ?? "Неизвестная ошибка валидации.");
                                }
                            }
                            else if (detailElement.ValueKind == JsonValueKind.String)
                            {
                                return OperationResult.FailureResult(detailElement.GetString() ?? "Ошибка сервера.");
                            }
                        }
                        return OperationResult.FailureResult($"Ошибка сервера: {response.StatusCode}. {errorContent}");
                    }
                    catch
                    {
                        return OperationResult.FailureResult($"Ошибка сервера: {response.StatusCode}. {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                return OperationResult.FailureResult($"Ошибка подключения: {ex.Message}");
            }
        }

        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"projects/{projectId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR ApiService] DeleteProjectAsync failed for ID {projectId}: {ex.Message}");
                return false;
            }
        }

        public void Dispose() =>
            _httpClient?.Dispose();
    }
}