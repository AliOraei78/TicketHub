using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace TicketHub.Web.Security;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth-policy";
    public const string AntiSpamPolicy = "anti-spam-policy";
    public const string CaptchaPolicy = "captcha-policy";

    public static IServiceCollection AddTicketHubRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = 429,
                    error = "Too Many Requests",
                    message = "تعداد درخواست‌های ارسالی بیش از حد مجاز است. لطفاً چند لحظه بعد مجدداً تلاش کنید."
                }, cancellationToken: cancellationToken);
            };

            // 1. Auth policy: Max 5 attempts per 1 minute window (Anti-bruteforce for login/auth)
            options.AddFixedWindowLimiter(AuthPolicy, opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // 2. Anti-spam policy: Max 10 requests per 1 minute sliding window (For high-frequency actions)
            options.AddSlidingWindowLimiter(AntiSpamPolicy, opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.QueueLimit = 0;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // 3. Captcha policy: Max 60 requests per 1 minute window (For dynamic captcha image generation & reloads)
            options.AddFixedWindowLimiter(CaptchaPolicy, opt =>
            {
                opt.PermitLimit = 60;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            // 3. Global IP-based partition rate limiter for general requests
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

        return services;
    }
}
