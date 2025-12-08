using System.Text.Json.Serialization;

namespace ApiClient.Models.Lists
{
    public class ManagerListModel
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("qualification")]
        public string Qualification { get; set; } = string.Empty;

        public int ProjectManagerId { get; set; } 
    }
}
