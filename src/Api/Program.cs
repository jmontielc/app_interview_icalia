using Api.Auth;
using Api.Data;
using Api.Domain;
using Api.Errors;
using Api.Integrations;
using Api.Security;
using Api.Services;
using Api.Storage;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new SsnJsonConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BureauExceptionHandler>();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAppAuthorization();
builder.Services.AddBureauIntegrations(builder.Configuration);
builder.Services.AddDocumentStorage(builder.Configuration);

builder.Services.AddDataProtection();
builder.Services.AddSingleton<ISsnProtector, SsnProtector>();
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

builder.Services.AddScoped<ApplicantService>();
builder.Services.AddScoped<BureauCheckService>();
builder.Services.AddScoped<AuditLogger>();
builder.Services.AddScoped<DocumentService>();

// Only the Vue dev origin (or the configured production origin) may call the API from a browser.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevDataSeeder.InitializeAsync(app.Services, app.Configuration);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Lets the integration tests host this app.
public partial class Program
{
}
