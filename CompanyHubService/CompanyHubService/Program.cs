using Microsoft.EntityFrameworkCore;
using CompanyHubService.Data;
using CompanyHubService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.AspNetCore.Identity;
using CompanyHubService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CompanyHubService.Seeders;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<CompanyHubDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<CompanyHubDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "Compedia",
            ValidAudience = "CompediaClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("JWT_SECRET_PLACEHOLDER")) // Use the same key as in token generation
        };
    });

// Configure Identity options if needed (password settings, lockout, etc.)
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.SignIn.RequireConfirmedEmail = false;
});


// Add Kafka configuration from appsettings.json
var kafkaConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
    SaslMechanism = SaslMechanism.Plain,
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslUsername = builder.Configuration["Kafka:SaslUsername"],
    SaslPassword = builder.Configuration["Kafka:SaslPassword"]
};

// Register Kafka producer as a singleton service
builder.Services.AddSingleton<IProducer<string, string>>(new ProducerBuilder<string, string>(kafkaConfig).Build());
builder.Services.AddHttpClient();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<RoleSeeder>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddTransient<EmailService>();
builder.Services.AddScoped<IPdfExtractionService, PdfExtractionService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleSeeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
    await roleSeeder.InitializeRolesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
