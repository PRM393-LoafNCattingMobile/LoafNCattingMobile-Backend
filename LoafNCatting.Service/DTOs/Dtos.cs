namespace LoafNCatting.Service.DTOs;

public record AuthResponseDto(int UserId, string Name, string Email, string PhoneNumber, string RoleName, string Token);
public record EmailVerificationChallengeDto(string Email, DateTime ExpiresAtUtc);
public record LoginResultDto(AuthResponseDto? Auth, bool RequiresEmailVerification, string? Email);
public record RegisterRequestDto(string Name, string Email, string PhoneNumber, string Password);
public record LoginRequestDto(string Login, string Password);
public record VerifyEmailRequestDto(string Email, string VerificationCode);
public record ResendVerificationRequestDto(string Email);
public record CategoryDto(int CategoryId, string Name, string? Description);
public record ProductDto(int ProductId, string Name, string? Description, decimal Price, decimal? DiscountPrice, int UnitInStock, string? Picture, int CategoryId, string CategoryName, bool IsAvailable, bool CanOrder);
public record AdminProductRequestDto(string Name, string? Description, decimal Price, decimal? DiscountPrice, int UnitInStock, string? Picture, int CategoryId, bool IsAvailable);
public record StaffProductAvailabilityDto(int UnitInStock, bool IsAvailable);
public record CatDto(int CatId, string Name, int? Age, string? GenderName, string? Breed, string? Picture, string? Description, int? FriendlinessRating, int? CutenessRating, int? PlayfulnessRating, string StatusName);
public record AdminCatRequestDto(string Name, int? Age, int? GenderId, string? Breed, string? Picture, string? Description, int? FriendlinessRating, int? CutenessRating, int? PlayfulnessRating, int StatusId);
public record StaffCatStatusDto(int StatusId);
public record TableDto(int TableId, string TableName, int Capacity, string? Area, string? Description, string StatusName);
public record AdminTableRequestDto(string TableName, int Capacity, string? Area, string? Description, int TableStatusId);
public record StaffTableStatusDto(int TableStatusId);
public record ReservationDto(int ReservationId, int? UserId, DateOnly Date, TimeOnly Time, string GuestName, string GuestPhoneNumber, int NumberOfGuests, string? Note, string StatusName, int TableId, string TableName);
public record CreateReservationDto(int? UserId, DateOnly Date, TimeOnly Time, string GuestName, string GuestPhoneNumber, int NumberOfGuests, string? Note, int TableId);
public record CartItemRequestDto(int UserId, int ProductId, int Quantity);
public record CartItemDto(ProductDto Product, int Quantity, decimal UnitPrice, decimal Subtotal);
public record CartDto(int CartId, int UserId, int TotalQuantity, decimal TotalPrice, List<CartItemDto> Items);
public record OrderItemRequestDto(int ProductId, int Quantity);
public record CreateOrderRequestDto(int UserId, int? TableId, int? ReservationId, string? OrderType, string? Note, string PaymentMethod, List<OrderItemRequestDto> Items);
public record OrderDetailDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);
public record OrderDto(int OrderId, DateTime OrderDate, decimal TotalPrice, int? CustomerUserId, string StatusName, string PaymentStatus, List<OrderDetailDto> Items);
public record CreatePaymentLinkRequestDto(int OrderId);
public record PaymentLinkDto(int OrderId, long OrderCode, int Amount, string CheckoutUrl, string QrCode, string PaymentLinkId);
public record PaymentStatusDto(int OrderId, string PaymentStatus, string OrderStatus, bool IsPaid);
public record NotificationDto(int NotificationId, int? UserId, string Title, string Content, string? Type, bool IsRead, DateTime CreatedAt);
public record StoreLocationDto(string StoreName, string Address, string? PhoneNumber, string? OpeningHours, double Latitude, double Longitude);
public record ConversationDto(int ConversationId, int UserId, DateTime CreatedAt);
public record MessageDto(int MessageId, int ConversationId, int SenderUserId, string Sender, string Content, bool IsRead, DateTime SentAt);
public record CreateMessageDto(int ConversationId, int SenderUserId, string Content);
public record LookupItemDto(int Id, string Name, string? Description);
public record AdminCategoryRequestDto(string Name, string? Description);
public record AdminLookupsDto(
    List<LookupItemDto> Roles,
    List<LookupItemDto> OrderStatuses,
    List<LookupItemDto> ReservationStatuses,
    List<LookupItemDto> CatStatuses,
    List<LookupItemDto> TableStatuses,
    List<LookupItemDto> PaymentMethods,
    List<LookupItemDto> Genders,
    List<LookupItemDto> Categories);


