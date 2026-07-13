using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Service.Validation;

namespace LoafNCatting.Service.Implementations;

public class ReservationService(
    IReservationRepository reservations,
    IReservationStatusRepository reservationStatuses,
    INotificationWriter notifications,
    ITableService tableService,
    IRestaurantTableRepository tables,
    ITableStatusRepository tableStatuses,
    IUserRepository? users = null) : IReservationService
{
    public async Task<ReservationDto?> CreateReservationAsync(CreateReservationDto request)
    {
        var guestPhoneNumber = request.GuestPhoneNumber.Trim();
        if (request.Date.ToDateTime(request.Time) <= DateTime.Now ||
            !PhoneNumberValidator.IsValid(guestPhoneNumber))
        {
            return null;
        }

        var available = await tableService.GetAvailableTablesAsync(request.Date, request.Time, guestCount: 1);
        var assignedTables = SelectTablesForGuests(available, request.NumberOfGuests, request.TableId);
        if (assignedTables.Count == 0)
        {
            return null;
        }

        var status = await reservationStatuses.GetByNameAsync("Đang chờ");
        Reservation? primaryReservation = null;
        foreach (var assignedTable in assignedTables)
        {
            var reservation = new Reservation
            {
                UserId = request.UserId,
                Date = request.Date,
                Time = request.Time,
                GuestName = request.GuestName.Trim(),
                GuestPhoneNumber = guestPhoneNumber,
                NumberOfGuests = request.NumberOfGuests,
                Note = request.Note,
                StatusId = status.StatusId,
                Status = status,
                TableId = assignedTable.TableId
            };

            await reservations.AddAsync(reservation);
            primaryReservation ??= reservation;
        }

        await reservations.SaveChangesAsync();
        await notifications.CreateAsync(
            request.UserId,
            "Đã nhận đặt bàn",
            "Lịch đặt bàn của bạn đang chờ xác nhận.",
            "reservation");
        await NotifyStaffUsersAsync(
            "Đặt bàn mới",
            $"Khách hàng #{request.UserId} vừa tạo lịch đặt bàn #{primaryReservation!.ReservationId}.",
            "reservation");
        return await GetReservationDtoAsync(primaryReservation.ReservationId);
    }

    public async Task<List<ReservationDto>> GetUserReservationsAsync(int userId)
    {
        var items = await reservations.GetUserReservationsAsync(userId);
        return items.Select(CafeDtoMapper.ToReservationDto).ToList();
    }

    public async Task<List<ReservationDto>> GetStaffReservationsAsync(
        int? statusId,
        DateOnly? date)
    {
        var items = await reservations.GetStaffReservationsAsync(statusId, date);
        return items.Select(CafeDtoMapper.ToReservationDto).ToList();
    }

    public async Task<ReservationDto?> UpdateReservationStatusAsync(
        int id,
        StaffReservationStatusDto request)
    {
        var reservation = await reservations.GetByIdWithDetailsAsync(id);
        var targetStatus = await reservationStatuses.GetByIdAsync(request.StatusId);
        if (reservation is null ||
            targetStatus is null ||
            !CanTransition(reservation.Status.StatusName, targetStatus.StatusName))
        {
            return null;
        }

        reservation.StatusId = targetStatus.StatusId;
        reservation.Status = targetStatus;
        reservation.UpdatedAt = DateTime.UtcNow;
        await SyncTableStatusAsync(reservation, targetStatus.StatusName);
        reservations.Update(reservation);
        await reservations.SaveChangesAsync();
        await notifications.CreateAsync(
            reservation.UserId,
            NotificationTitleForReservationStatus(targetStatus.StatusName),
            NotificationContentForReservationStatus(reservation.ReservationId, targetStatus.StatusName),
            "reservation");
        return CafeDtoMapper.ToReservationDto(reservation);
    }

    private async Task<ReservationDto?> GetReservationDtoAsync(int reservationId)
    {
        var reservation = await reservations.GetByIdWithDetailsAsync(reservationId);
        return reservation is null ? null : CafeDtoMapper.ToReservationDto(reservation);
    }

    private static List<TableDto> SelectTablesForGuests(
        IEnumerable<TableDto> availableTables,
        int guestCount,
        int? requestedTableId)
    {
        var available = availableTables.ToList();
        if (requestedTableId.HasValue)
        {
            var requested = available.FirstOrDefault(table => table.TableId == requestedTableId.Value);
            if (requested is null)
            {
                return [];
            }

            return CompleteTableSelection(
                [requested],
                available.Where(table => table.TableId != requested.TableId),
                guestCount);
        }

        var singleTable = available
            .Where(table => table.Capacity >= guestCount)
            .OrderBy(table => table.Capacity)
            .FirstOrDefault();
        if (singleTable is not null)
        {
            return [singleTable];
        }

        return CompleteTableSelection([], available, guestCount);
    }

    private static List<TableDto> CompleteTableSelection(
        List<TableDto> selectedTables,
        IEnumerable<TableDto> remainingTables,
        int guestCount)
    {
        var totalCapacity = selectedTables.Sum(table => table.Capacity);
        foreach (var table in remainingTables.OrderByDescending(table => table.Capacity))
        {
            if (totalCapacity >= guestCount)
            {
                break;
            }

            selectedTables.Add(table);
            totalCapacity += table.Capacity;
        }

        return totalCapacity >= guestCount ? selectedTables : [];
    }

    private async Task SyncTableStatusAsync(Reservation reservation, string reservationStatus)
    {
        var nextTableStatusName = reservationStatus switch
        {
            "Đã xác nhận" => "Đã đặt",
            "Đã hủy" or "Hoàn thành" or "Không đến" => "Trống",
            _ => null
        };

        if (nextTableStatusName is null)
        {
            return;
        }

        if (nextTableStatusName == "Trống" &&
            await reservations.HasActiveReservationForTableAsync(
                reservation.TableId,
                reservation.ReservationId))
        {
            return;
        }

        var table = reservation.Table is not null && reservation.Table.TableId == reservation.TableId
            ? reservation.Table
            : await tables.GetByIdWithStatusAsync(reservation.TableId);
        var tableStatus = await GetTableStatusByNameAsync(nextTableStatusName);
        if (table is null || tableStatus is null)
        {
            return;
        }

        table.TableStatusId = tableStatus.TableStatusId;
        table.TableStatus = tableStatus;
        tables.Update(table);
    }

    private async Task<TableStatus?> GetTableStatusByNameAsync(string statusName)
    {
        var statuses = await tableStatuses.GetAllAsync();
        return statuses.FirstOrDefault(status =>
            string.Equals(status.StatusName, statusName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task NotifyStaffUsersAsync(string title, string content, string type)
    {
        if (users is null)
        {
            return;
        }

        var staffUsers = await users.GetAdminUsersAsync(roleId: null, search: null, active: true);
        foreach (var staff in staffUsers.Where(IsStaffUser))
        {
            await notifications.CreateAsync(staff.UserId, title, content, type);
        }
    }

    private static bool IsStaffUser(User user)
    {
        return string.Equals(user.Role?.RoleName, "Staff", StringComparison.OrdinalIgnoreCase);
    }

    private static string NotificationTitleForReservationStatus(string statusName)
    {
        return statusName switch
        {
            "Đã xác nhận" => "Đặt bàn đã được xác nhận",
            "Hoàn thành" => "Lịch đặt bàn đã hoàn thành",
            "Đã hủy" => "Lịch đặt bàn đã bị hủy",
            "Không đến" => "Lịch đặt bàn được ghi nhận không đến",
            _ => "Cập nhật đặt bàn"
        };
    }

    private static string NotificationContentForReservationStatus(int reservationId, string statusName)
    {
        return statusName switch
        {
            "Đã xác nhận" => $"Lịch đặt bàn #{reservationId} đã được nhân viên xác nhận.",
            "Hoàn thành" => $"Lịch đặt bàn #{reservationId} đã hoàn thành.",
            "Đã hủy" => $"Lịch đặt bàn #{reservationId} đã bị hủy.",
            "Không đến" => $"Lịch đặt bàn #{reservationId} đã được ghi nhận là không đến.",
            _ => $"Lịch đặt bàn #{reservationId} đã được cập nhật trạng thái {statusName}."
        };
    }

    private static bool CanTransition(string currentStatus, string targetStatus)
    {
        return currentStatus switch
        {
            "Đang chờ" => targetStatus is "Đã xác nhận" or "Đã hủy",
            "Đã xác nhận" => targetStatus is "Hoàn thành" or "Đã hủy" or "Không đến",
            _ => false
        };
    }
}



