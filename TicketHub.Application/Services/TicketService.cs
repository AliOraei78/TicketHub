namespace TicketHub.Application.Services;

using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository ticketRepository,
        ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<TicketDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("جستجوی تیکت با شناسه {Id}.", id);

            var ticket = await _ticketRepository.GetByIdAsync(id);

            if (ticket == null)
                _logger.LogWarning("تیکت با شناسه {Id} یافت نشد.", id);

            return ticket?.Adapt<TicketDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت تیکت با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task<(List<TicketDto> Tickets, int TotalCount)> GetFilteredTicketsAsync(
        string searchTerm, int? projectId, int? statusId, int? userId, int page, int pageSize)
    {
        try
        {
            _logger.LogInformation("دریافت لیست تیکت‌ها با فیلتر. صفحه: {Page}، تعداد در صفحه: {PageSize}.", page, pageSize);

            var (tickets, totalCount) = await _ticketRepository.GetFilteredTicketsAsync(
                searchTerm, projectId, statusId, userId, page, pageSize);

            _logger.LogInformation("تعداد {TotalCount} تیکت منطبق با فیلترها یافت شد.", totalCount);

            return (tickets.Adapt<List<TicketDto>>(), totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست فیلتر شده تیکت‌ها.");
            throw;
        }
    }

    public async Task CreateAsync(TicketDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد تیکت جدید.");

            var ticket = dto.Adapt<Ticket>();
            ticket.CreatedAt = DateTime.UtcNow;

            await _ticketRepository.AddAsync(ticket);

            _logger.LogInformation("تیکت با موفقیت ایجاد شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد تیکت جدید.");
            throw;
        }
    }

    public async Task UpdateAsync(TicketDto dto)
    {
        try
        {
            _logger.LogInformation("ویرایش تیکت با شناسه {Id}.", dto.Id);

            var ticketInDb = await _ticketRepository.GetByIdAsync(dto.Id);
            if (ticketInDb == null)
            {
                _logger.LogWarning("تیکت با شناسه {Id} جهت ویرایش یافت نشد.", dto.Id);
                return;
            }

            dto.Adapt(ticketInDb);
            await _ticketRepository.UpdateAsync(ticketInDb);

            _logger.LogInformation("تیکت با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش تیکت با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف تیکت با شناسه {Id}.", id);

            await _ticketRepository.DeleteAsync(id);

            _logger.LogInformation("تیکت با شناسه {Id} با موفقیت حذف شد.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف تیکت با شناسه {Id}.", id);
            throw;
        }
    }
}