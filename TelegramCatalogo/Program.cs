using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TelegramCatalogo.Data;
using TelegramCatalogo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<SlugService>();
builder.Services.AddScoped<ImagenService>();

builder.Services.AddDbContext<AplicacionDbContexto>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ========================================
// AUTENTICACIÓN POR COOKIES
// ========================================

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";

        options.Cookie.Name = "TelegramCatalogo.Admin";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();
var hashTemporal =
    BCrypt.Net.BCrypt.HashPassword("123");

Console.WriteLine(
    "HASH ADMIN: " + hashTemporal
);
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// IMPORTANTE:
// Authentication debe ir antes de Authorization

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();