using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProductApi.DTOs;

namespace ProductApi.Middleware;

/// <summary>
/// Replaces ASP.NET Core's default validation problem-details response
/// with the application's consistent ApiErrorResponse envelope. This keeps
/// every error in the API using the same JSON shape.
/// </summary>
public class CustomValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = new Dictionary<string, List<string>>();
            foreach (var kvp in context.ModelState)
            {
                var key = kvp.Key;
                // Convert PascalCase model keys to snake_case-ish JSON names.
                if (string.IsNullOrEmpty(key)) key = "model";
                var messages = kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new();
                if (messages.Count > 0)
                {
                    errors[key] = messages;
                }
            }

            context.Result = new BadRequestObjectResult(new ApiErrorResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
            return;
        }

        await next();
    }
}
