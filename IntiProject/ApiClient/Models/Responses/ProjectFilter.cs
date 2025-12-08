using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ApiClient.Models.Responses
{
    public class ProjectFilter
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("project_manager")] 
        public string? ProjectManager { get; set; }

        [JsonPropertyName("project_date_from")]
        public DateOnly? ProjectDateFrom { get; set; }

        [JsonPropertyName("project_date_to")]
        public DateOnly? ProjectDateTo { get; set; }

        [JsonPropertyName("search_text")] 
        public string? SearchText { get; set; }
    }
}
