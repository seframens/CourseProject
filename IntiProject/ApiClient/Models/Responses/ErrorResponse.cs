namespace ApiClient.Models.Responses
{
    // Модель для ошибки API (если возвращается в формате {"detail": "..."})
    public class ErrorResponse
    {
        public string? Detail { get; set; }
    }
}
