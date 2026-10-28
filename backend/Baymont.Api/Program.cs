using Baymont.Api.Middleware;
using Baymont.Application.DependencyInjection;
using Baymont.Infrastructure.DependencyInjection;
using Baymont.Infrastructure.Identity;
using Baymont.Infrastructure.Persistence;
using Baymont.Infrastructure.Seed;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Behind Railway's proxy: trust X-Forwarded-* so the request scheme is https (secure cookies).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Keep cookie-signing keys on the persistent volume, otherwise every redeploy logs everyone out.
builder.Services.AddDataProtection()
    .SetApplicationName("Baymont")
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
        // Malformed requests (bad JSON, wrong types, missing fields) get one short Arabic sentence.
        options.InvalidModelStateResponseFactory = context =>
            new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { message = ModelErrors.Describe(context.ModelState) }));
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await IdentitySeed.SeedAsync(app.Services);
    await DemoDataSeeder.SeedAsync(app.Services);
}

app.UseForwardedHeaders();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("dev");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();
// An unknown API address must answer with an error the screens can read, not with the web page.
app.MapFallback("/api/{**path}", () =>
    Results.Json(new { message = "العنوان المطلوب غير موجود." }, statusCode: StatusCodes.Status404NotFound));
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
