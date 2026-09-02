using FinTrustFDManager.DAL.Data;
using Microsoft.EntityFrameworkCore;
using FinTrustFDManager.BAL.Common;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// ── Health Checks ──
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: new[] { "db" });

// ── Repositories ──
builder.Services.AddScoped<IFDIdentificationRepository, FDIdentificationRepository>();
builder.Services.AddScoped<IFDInterestRepository, FDInterestRepository>();
builder.Services.AddScoped<IFDCashFlowRepository, FDCashFlowRepository>();
builder.Services.AddScoped<IEntityRepository, EntityRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ICounterPartyRepository, CounterPartyRepository>();

builder.Services.AddScoped<IInterestFrequencyRepository, InterestFrequencyRepository>();
builder.Services.AddScoped<IDayCountConventionRepository, DayCountConventionRepository>();
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<ICashFlowRepository, CashFlowRepository>();
builder.Services.AddScoped<IInvestmentApprovalRepository, InvestmentApprovalRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IBenchmarkRepository, BenchmarkRepository>();
builder.Services.AddScoped<IBenchmarkRateHistoryRepository, BenchmarkRateHistoryRepository>();
builder.Services.AddScoped<IFDAmendmentRepository, FDAmendmentRepository>();

// ── Services ──
builder.Services.AddScoped<IFDIdentificationService, FDIdentificationService>();
builder.Services.AddScoped<IFDInterestService, FDInterestService>();
builder.Services.AddScoped<IFDCashFlowService, FDCashFlowService>();
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>();
builder.Services.AddScoped<IBenchmarkRateHistoryService, BenchmarkRateHistoryService>();
builder.Services.AddScoped<IFDAmendmentService, FDAmendmentService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddBAL();


// =====================================================
// CORS - ANGULAR FRONTEND
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://localhost",
                "http://localhost:80",
                "http://frontend",
                "http://frontend:80")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var jwtKey = builder.Configuration["Jwt:Key"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey!)
        )
    };
});

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ── Global Exception Handler ──
// Maps known exceptions to appropriate HTTP status codes.
// In production, never exposes internal details.
app.UseExceptionHandler(error =>
{
    error.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exception != null)
        {
            var statusCode = exception.Error switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            context.Response.StatusCode = statusCode;

            var message = app.Environment.IsDevelopment()
                ? exception.Error.Message
                : statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception.Error.Message;

            await context.Response.WriteAsJsonAsync(new { message });
        }
    });
});

app.UseCors("AngularPolicy");

// IMPORTANT: Authentication must come BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── Health Check endpoint (no auth required) ──
app.MapHealthChecks("/health");

app.Run();