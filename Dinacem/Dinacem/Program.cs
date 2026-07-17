using Dinacem.Models;
using Dinacem.Models.Servicios;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ======================================
// Servicios
// ======================================

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AplicacionDbContexto>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Servicio para consultar RUC
builder.Services.AddHttpClient<RucService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ======================================
// Aplicación
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

app.UseAuthorization();

// ======================================
// Rutas
// ======================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ======================================

app.Run();