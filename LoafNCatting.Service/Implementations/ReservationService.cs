using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class ReservationService(
    IReservationRepository reservations,
    IReservationStatusRepository reservationStatuses,
    INotificationRepository notifications,
    ITableService tableService) : IReservationService
{
    public async Task<ReservationDto?> CreateReservationAsync(CreateReservationDto request)
    {
        var available = await tableService.GetAvailableTablesAsync(request.Date, request.Time, request.NumberOfGuests);
        if (!available.Any(table => table.TableId == request.TableId))
        {
            return null;
        }

        var status = await reservationStatuses.GetByNameAsync("Đang chờ");
        var reservation = new Reservation
        {
            UserId = request.UserId,
            Date = request.Date,
            Time = request.Time,
            GuestName = request.GuestName.Trim(),
            GuestPhoneNumber = request.GuestPhoneNumber.Trim(),
            NumberOfGuests = request.NumberOfGuests,
            Note = request.Note,
            StatusId = status.StatusId,
            TableId = request.TableId
        };

        await reservations.AddAsync(reservation);
        await AddNotificationAsync(request.UserId, "Đã nhận đặt bàn", "Lịch đặt bàn của bạn đang chờ xác nhận.", "reservation");
        await reservations.SaveChangesAsync();
        return await GetReservationDtoAsync(reservation.ReservationId);
    }

    public async Task<List<ReservationDto>> GetUserReservationsAsync(int userId)
    {
        var items = await reservations.GetUserReservationsAsync(userId);
        return items.Select(CafeDtoMapper.ToReservationDto).ToList();
    }

    private async Task<ReservationDto?> GetReservationDtoAsync(int reservationId)
    {
        var reservation = await reservations.GetByIdWithDetailsAsync(reservationId);
        return reservation is null ? null : CafeDtoMapper.ToReservationDto(reservation);
    }

    private async Task AddNotificationAsync(int? userId, string title, string content, string type)
    {
        if (!userId.HasValue)
        {
            return;
        }

        await notifications.AddAsync(new Notification
        {
            UserId = userId.Value,
            Title = title,
            Content = content,
            Type = type
        });
    }
}



