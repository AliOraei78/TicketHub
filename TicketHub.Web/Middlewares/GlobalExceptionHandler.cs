using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TicketHub.Core.Common.Exceptions;

namespace TicketHub.Web.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is TicketHubException customException)
        {
            httpContext.Response.StatusCode = customException.StatusCode;
            problemDetails.Title = customException.GetType().Name;
            problemDetails.Status = customException.StatusCode;
            problemDetails.Detail = customException.Message;

            // اضافه کردن ErrorCode به بخش Extensions استاندارد ProblemDetails
            problemDetails.Extensions["errorCode"] = customException.ErrorCode;

            // اگر خطای Validation بود، لیست خطاها را هم اضافه کن
            if (customException is ValidationException validationException && validationException.Errors.Any())
            {
                problemDetails.Extensions["errors"] = validationException.Errors;
            }
        }
        else if (exception is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            problemDetails.Title = "ConcurrencyConflict";
            problemDetails.Status = StatusCodes.Status409Conflict;
            problemDetails.Detail = "اطلاعات این رکورد همزمان توسط کاربر یا فرآیند دیگری تغییر یافته است.";
            problemDetails.Extensions["errorCode"] = "CONCURRENCY_CONFLICT";
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            problemDetails.Title = "Server Error";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Detail = "یک خطای غیرمنتظره در سرور رخ داده است.";
            problemDetails.Extensions["errorCode"] = "INTERNAL_SERVER_ERROR";
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}