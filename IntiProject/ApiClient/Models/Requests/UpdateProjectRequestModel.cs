namespace ApiClient.Models.Requests
{
    public class UpdateProjectRequestModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Employer { get; set; } = string.Empty;
        public string ProjectManagerFullName { get; set; } = string.Empty;
        public string WorkTypeName { get; set; } = string.Empty;  
        public DateOnly ProjectDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
