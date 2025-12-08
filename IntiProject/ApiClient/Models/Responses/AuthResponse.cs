using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ApiClient.Models.Responses
{
    public class AuthResponse : INotifyPropertyChanged
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("role_name")]
        public string RoleName { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        public bool IsAdministrator => RoleName?.ToLower() == "администратор";
        public bool IsManager => RoleName?.ToLower() == "менеджер";
        public bool IsGuest => RoleName?.ToLower() == "гость" || string.IsNullOrEmpty(RoleName);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
