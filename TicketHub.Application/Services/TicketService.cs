namespace TicketHub.Application.Services;

using Mapster;
using Microsoft.Extensions.Logging;
using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketService> _logger;
    private readonly IValidator<TicketDto> _validator;

    public TicketService(
        ITicketRepository ticketRepository,
        ILogger<TicketService> logger,
        IValidator<TicketDto> validator)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
        _validator = validator;
    }

    private async Task ValidateDtoAsync(TicketDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            throw new ValidationException(errors);
        }
    }

    public async Task<TicketDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("جستجوی تیکت با شناسه {Id}.", id);

        var ticket = await _ticketRepository.GetByIdAsync(id);

        if (ticket == null)
            throw new NotFoundException("تیکت", id);

        return ticket.Adapt<TicketDto>();
    }

    public async Task<(List<TicketDto> Tickets, int TotalCount)> GetFilteredTicketsAsync(
            string? searchTerm, List<int>? projectIds, List<int>? statusIds, int? userId, int page, int pageSize)
    {
        _logger.LogInformation("دریافت لیست تیکت‌ها با فیلتر. صفحه: {Page}، تعداد در صفحه: {PageSize}.", page, pageSize);

        var (tickets, totalCount) = await _ticketRepository.GetFilteredTicketsAsync(
            searchTerm, projectIds, statusIds, userId, page, pageSize);

        _logger.LogInformation("تعداد {TotalCount} تیکت منطبق با فیلترها یافت شد.", totalCount);

        return (tickets.Adapt<List<TicketDto>>(), totalCount);
    }

    public async Task CreateAsync(TicketDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ایجاد تیکت جدید.");

        var ticket = dto.Adapt<Ticket>();
        ticket.CreatedAt = DateTime.UtcNow;

        await _ticketRepository.AddAsync(ticket);

        _logger.LogInformation("تیکت با موفقیت ایجاد شد.");
    }

    public async Task UpdateAsync(TicketDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("ویرایش تیکت با شناسه {Id}.", dto.Id);

        var ticketInDb = await _ticketRepository.GetByIdAsync(dto.Id);
        if (ticketInDb == null)
            throw new NotFoundException("تیکت", dto.Id);

        dto.Adapt(ticketInDb);
        await _ticketRepository.UpdateAsync(ticketInDb);

        _logger.LogInformation("تیکت با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogWarning("درخواست حذف تیکت با شناسه {Id}.", id);

        var ticketInDb = await _ticketRepository.GetByIdAsync(id);
        if (ticketInDb == null)
            throw new NotFoundException("تیکت", id);

        await _ticketRepository.DeleteAsync(id);

        _logger.LogInformation("تیکت با شناسه {Id} با موفقیت حذف شد.", id);
    }
}