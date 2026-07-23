using Dinacem.Models;
using Dinacem.Models.Servicios;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
var builder = WebApplication.CreateBuilder(args);

// ======================================
// Servicios MVC
// ======================================

builder.Services.AddControllersWithViews();
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddControllersWithViews();

// ======================================
// Entity Framework + SQL Server
// ======================================

builder.Services.AddDbContext<AplicacionDbContexto>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ======================================
// Servicio para consultar RUC
// ======================================

builder.Services.AddHttpClient<RucService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ======================================
// Sesiones
// ======================================

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Dinacem.Session";
});

builder.Services.Configure<CorreoConfiguracion>(
    builder.Configuration.GetSection("Correo"));

builder.Services.AddScoped<CorreoService>();

builder.Services.AddScoped<RendicionPdfService>();
// ======================================
// Construir aplicación
// ======================================

var app = builder.Build();

// ======================================
// Middleware
// ======================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// La sesión debe ir después de UseRouting
// y antes de acceder a Session en controladores.
app.UseSession();

app.UseAuthorization();

// ======================================
// Rutas
// ======================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ======================================
// Ejecutar aplicación
// ======================================

app.Run();