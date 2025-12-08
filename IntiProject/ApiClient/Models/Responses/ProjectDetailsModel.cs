using System.Text.Json.Serialization;

namespace ApiClient.Models.Responses
{
    public class ProjectDetailsModel
    {
        public int ProjectNumber { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Employer { get; set; } = string.Empty;

        public string ProjectManagerFullName { get; set; } = string.Empty;

        public string ProjectManagerQualification { get; set; } = string.Empty;

        public string WorkTypeName { get; set; } = string.Empty;

        public DateOnly ProjectDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
