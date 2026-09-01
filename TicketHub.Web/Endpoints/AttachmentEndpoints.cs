using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Web.Endpoints;

public static class AttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/attachments");

        group.MapGet("/{id:int}/download", async (
            int id,
            IRepository<Attachment> attachmentRepo,
            IFileStorageService fileStorageService,
            HttpContext httpContext) =>
        {
            var attachment = await attachmentRepo.GetByIdAsync(id);
            if (attachment == null || string.IsNullOrWhiteSpace(attachment.FilePath))
            {
                return Results.NotFound(new { message = "فایل ضمیمه یافت نشد." });
            }

            var fileUrl = await fileStorageService.GetFileUrlAsync(attachment.FilePath, TimeSpan.FromHours(1));

            if (fileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                fileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Redirect(fileUrl);
            }

            var stream = await fileStorageService.GetFileStreamAsync(attachment.FilePath);
            if (stream == null)
            {
                return Results.NotFound(new { message = "محتوای فایل در فضای ذخیره‌سازی یافت نشد." });
            }

            var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                ? "application/octet-stream"
                : attachment.ContentType;

            return Results.File(
                stream,
                contentType: contentType,
                fileDownloadName: attachment.FileName,
                enableRangeProcessing: true);
        });

        group.MapGet("/{id:int}/url", async (
            int id,
            IRepository<Attachment> attachmentRepo,
            IFileStorageService fileStorageService) =>
        {
            var attachment = await attachmentRepo.GetByIdAsync(id);
            if (attachment == null || string.IsNullOrWhiteSpace(attachment.FilePath))
            {
                return Results.NotFound(new { message = "فایل ضمیمه یافت نشد." });
            }

            var url = await fileStorageService.GetFileUrlAsync(attachment.FilePath, TimeSpan.FromHours(2));
            return Results.Ok(new { id = attachment.Id, fileName = attachment.FileName, url = url });
        });

        return endpoints;
    }
}
