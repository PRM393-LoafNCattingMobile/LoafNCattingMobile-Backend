using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class LookupService(
    IRoleRepository roles,
    IOrderStatusRepository orderStatuses,
    IReservationStatusRepository reservationStatuses,
    ICatStatusRepository catStatuses,
    ITableStatusRepository tableStatuses,
    IPaymentMethodRepository paymentMethods,
    IGenderRepository genders,
    ICategoryRepository categories) : ILookupService
{
    public async Task<AdminLookupsDto> GetAdminLookupsAsync()
    {
        var roleItems = await roles.GetAllAsync();
        var orderStatusItems = await orderStatuses.GetAllAsync();
        var reservationStatusItems = await reservationStatuses.GetAllAsync();
        var catStatusItems = await catStatuses.GetAllAsync();
        var tableStatusItems = await tableStatuses.GetAllAsync();
        var paymentMethodItems = await paymentMethods.GetAllAsync();
        var genderItems = await genders.GetAllAsync();
        var categoryItems = await categories.GetAllOrderedAsync();

        return new AdminLookupsDto(
            roleItems.Select(ToLookupItem).ToList(),
            orderStatusItems.Select(ToLookupItem).ToList(),
            reservationStatusItems.Select(ToLookupItem).ToList(),
            catStatusItems.Select(ToLookupItem).ToList(),
            tableStatusItems.Select(ToLookupItem).ToList(),
            paymentMethodItems.Select(ToLookupItem).ToList(),
            genderItems.Select(ToLookupItem).ToList(),
            categoryItems.Select(ToLookupItem).ToList());
    }

    private static LookupItemDto ToLookupItem(Role role) =>
        new(role.RoleId, role.RoleName, role.Description);

    private static LookupItemDto ToLookupItem(OrderStatus status) =>
        new(status.OrderStatusId, status.OrderStatusName, status.Description);

    private static LookupItemDto ToLookupItem(ReservationStatus status) =>
        new(status.StatusId, status.StatusName, status.Description);

    private static LookupItemDto ToLookupItem(CatStatus status) =>
        new(status.StatusId, status.StatusName, status.Description);

    private static LookupItemDto ToLookupItem(TableStatus status) =>
        new(status.TableStatusId, status.StatusName, status.Description);

    private static LookupItemDto ToLookupItem(PaymentMethod method) =>
        new(method.MethodId, method.MethodName, method.Description);

    private static LookupItemDto ToLookupItem(Gender gender) =>
        new(gender.GenderId, gender.GenderName, gender.Description);

    private static LookupItemDto ToLookupItem(Category category) =>
        new(category.CategoryId, category.Name, category.Description);
}
