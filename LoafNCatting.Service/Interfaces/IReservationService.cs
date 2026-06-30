using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IReservationService
{
    Task<ReservationDto?> CreateReservationAsync(CreateReservationDto request);
    Task<List<ReservationDto>> GetUserReservationsAsync(int userId);
    Task<List<ReservationDto>> GetStaffReservationsAsync(int? statusId, DateOnly? date);
    Task<ReservationDto?> UpdateReservationStatusAsync(int id, StaffReservationStatusDto request);
}

