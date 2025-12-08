using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiClient.Models.Responses
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Data { get; set; }
        public int? CreatedId { get; set; } 

        public static OperationResult SuccessResult(object? data = null) => new() { Success = true, Data = data };
        public static OperationResult FailureResult(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
    }
}
