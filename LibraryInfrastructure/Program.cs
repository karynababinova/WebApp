using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LibraryInfrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using LibraryInfrastructure.Data;
using Microsoft.Extensions.Logging;
using LibraryDomain.Model;
using LibraryInfrastructure.Services;
using LibraryInfrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Warning);

builder.Services.AddDbContext<DbLibraryContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDataPortServiceFactory<Fanfic>, FanficDataPortServiceFactory>();
builder.Services.AddScoped<IFanficExcelTemplateService, FanficExcelTemplateService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var rawUserId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(rawUserId, out var userId))
                {
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<DbLibraryContext>();
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || user.IsBlocked)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbLibraryContext>();
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE users
        ADD COLUMN IF NOT EXISTS role character varying(32);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE users
        ADD COLUMN IF NOT EXISTS is_blocked boolean NOT NULL DEFAULT false;
        """);
    await db.Database.ExecuteSqlRawAsync($"""
        UPDATE users
        SET role = CASE
            WHEN username = 'admin' OR email LIKE '%@admin.local' THEN '{AppRoles.Admin}'
            ELSE '{AppRoles.Author}'
        END
        WHERE role IS NULL OR role = '';
        """);
    await DemoDataSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
