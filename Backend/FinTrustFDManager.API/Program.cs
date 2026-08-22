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

// ── Repositories ──
builder.Services.AddScoped<IFDIdentificationRepository, FDIdentificationRepository>();
builder.Services.AddScoped<IFDInterestRepository, FDInterestRepository>();
builder.Services.AddScoped<IFDCashFlowRepository, FDCashFlowRepository>();
builder.Services.AddScoped<IEntityRepository, EntityRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ICounterPartyRepository, CounterPartyRepository>();
builder.Services.AddScoped<IBankRepository, BankRepository>();
builder.Services.AddScoped<IInterestFrequencyRepository, InterestFrequencyRepository>();
builder.Services.AddScoped<IDayCountConventionRepository, DayCountConventionRepository>();
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<ICashFlowRepository, CashFlowRepository>();
builder.Services.AddScoped<IInvestmentApprovalRepository, InvestmentApprovalRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

// ── Services ──
builder.Services.AddScoped<IFDIdentificationService, FDIdentificationService>();
builder.Services.AddScoped<IFDInterestService, FDInterestService>();
builder.Services.AddScoped<IFDCashFlowService, FDCashFlowService>();
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
            .WithOrigins("http://localhost:4200")
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

app.UseCors("AngularPolicy");

// IMPORTANT: Authentication must come BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();