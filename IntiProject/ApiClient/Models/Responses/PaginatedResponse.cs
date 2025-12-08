using System.Text.Json.Serialization;

namespace ApiClient.Models.Responses
{
   public class PaginatedResponse
    {
        [JsonPropertyName("projects")]
        public List<ProjectDetailsModel> Items { get; set; } = [];

        // Сопоставляем с "total_count" в JSON
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        // Сопоставляем с "page" в JSON
        [JsonPropertyName("page")]
        public int Page { get; set; }

        // Сопоставляем с "total_pages" в JSON
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        // Сопоставляем с "has_next" в JSON
        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        // Сопоставляем с "has_prev" в JSON
        [JsonPropertyName("has_prev")]
        public bool HasPrev { get; set; }
    }
}
