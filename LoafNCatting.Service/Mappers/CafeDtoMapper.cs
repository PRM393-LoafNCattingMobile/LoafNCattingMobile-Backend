using LoafNCatting.Service.DTOs;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Service.Mappers;

public static class CafeDtoMapper
{
    public static AuthResponseDto ToAuthResponse(User user, string token)
    {
        return new AuthResponseDto(
            user.UserId,
            user.Name,
            user.Email,
            user.PhoneNumber,
            user.Role.RoleName,
            token);
    }

    public static CategoryDto ToCategoryDto(Category category)
    {
        return new CategoryDto(category.CategoryId, category.Name, category.Description);
    }

    public static ProductDto ToProductDto(Product product)
    {
        return new ProductDto(
            product.ProductId,
            product.Name,
            product.Description,
            product.Price,
            product.DiscountPrice,
            product.UnitInStock,
            product.Picture,
            product.CategoryId,
            product.Category.Name,
            product.IsAvailable,
            product.IsAvailable && product.UnitInStock > 0);
    }

    public static CatDto ToCatDto(Cat cat)
    {
        return new CatDto(
            cat.CatId,
            cat.Name,
            cat.Age,
            cat.Gender?.GenderName,
            cat.Breed,
            cat.Picture,
            cat.Description,
            cat.FriendlinessRating,
            cat.CutenessRating,
            cat.PlayfulnessRating,
            cat.Status.StatusName);
    }

    public static TableDto ToTableDto(RestaurantTable table)
    {
        return new TableDto(
            table.TableId,
            table.TableName,
            table.Capacity,
            table.Area,
            table.Description,
            table.TableStatus.StatusName);
    }

    public static ReservationDto ToReservationDto(Reservation reservation)
    {
        return new ReservationDto(
            reservation.ReservationId,
            reservation.UserId,
            reservation.Date,
            reservation.Time,
            reservation.GuestName,
            reservation.GuestPhoneNumber,
            reservation.NumberOfGuests,
            reservation.Note,
            reservation.Status.StatusName,
            reservation.TableId,
            reservation.Table.TableName);
    }

    public static OrderDto ToOrderDto(Order order)
    {
        var details = order.OrderDetails
            .Select(detail => new OrderDetailDto(
                detail.ProductId,
                detail.Product.Name,
                detail.Quantity,
                detail.UnitPrice,
                detail.Subtotal))
            .ToList();

        var paymentStatus = order.Payments.FirstOrDefault()?.PaymentStatus ?? "Đang chờ";
        return new OrderDto(
            order.OrderId,
            order.OrderDate,
            order.TotalPrice,
            order.CustomerUserId,
            order.OrderStatus.OrderStatusName,
            paymentStatus,
            details);
    }

    public static CartDto ToCartDto(Cart cart)
    {
        var items = cart.CartItems
            .OrderBy(item => item.CreatedAt)
            .Select(item => new CartItemDto(
                ToProductDto(item.Product),
                item.Quantity,
                item.UnitPrice,
                item.UnitPrice * item.Quantity))
            .ToList();

        return new CartDto(
            cart.CartId,
            cart.UserId,
            items.Sum(item => item.Quantity),
            items.Sum(item => item.Subtotal),
            items);
    }

    public static NotificationDto ToNotificationDto(Notification notification)
    {
        return new NotificationDto(
            notification.NotificationId,
            notification.UserId,
            notification.Title,
            notification.Content,
            notification.Type,
            notification.IsRead,
            notification.CreatedAt);
    }

    public static MessageDto ToMessageDto(Message message, int customerUserId)
    {
        var sender = message.SenderUserId == customerUserId ? "customer" : "store";
        return new MessageDto(
            message.MessageId,
            message.ConversationId,
            message.SenderUserId,
            sender,
            message.Content,
            message.IsRead,
            message.SentAt);
    }
}


