using System.Text.Json;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Repositories;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Implementations;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.Mail;
using Microsoft.EntityFrameworkCore;
using Net.payOS;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddOptions<SmtpMailOptions>()
    .Bind(builder.Configuration.GetSection(SmtpMailOptions.SectionName));
builder.Services.AddOptions<EmailVerificationOptions>()
    .Bind(builder.Configuration.GetSection(EmailVerificationOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FlutterLocal", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<LoafNcattingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LoafNCattingMobile")));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICatRepository, CatRepository>();
builder.Services.AddScoped<ICatStatusRepository, CatStatusRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IGenderRepository, GenderRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
builder.Services.AddScoped<IOrderStatusRepository, OrderStatusRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IReservationStatusRepository, ReservationStatusRepository>();
builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IStoreLocationRepository, StoreLocationRepository>();
builder.Services.AddScoped<ITableStatusRepository, TableStatusRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var sessionTokenOptions = builder.Configuration.GetSection("SessionTokens").Get<SessionTokenOptions>() ?? new SessionTokenOptions();
builder.Services.AddSingleton(sessionTokenOptions);
builder.Services.AddSingleton<ISessionTokenService, InMemorySessionTokenService>();

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IMailService, SmtpMailService>();
builder.Services.AddScoped<IOtpGenerator, OtpGenerator>();
builder.Services.AddScoped<IVerificationEmailComposer, VerificationEmailComposer>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICatService, CatService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var payosConfig = builder.Configuration.GetSection("PayOS");
builder.Services.AddSingleton(new PayOS(
    payosConfig["ClientId"] ?? throw new InvalidOperationException("PayOS:ClientId is not configured"),
    payosConfig["ApiKey"] ?? throw new InvalidOperationException("PayOS:ApiKey is not configured"),
    payosConfig["ChecksumKey"] ?? throw new InvalidOperationException("PayOS:ChecksumKey is not configured")));
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStoreLocationService, StoreLocationService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<ILookupService, LookupService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("FlutterLocal");
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();


