using CalidadYPrueba_T3.Data;
using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.Security;
using CalidadYPrueba_T3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddDbContext<LabTrackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LabTrack")
        ?? throw new InvalidOperationException("Falta la conexión ConnectionStrings:LabTrack.")));

builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue<int>("LabTrack:BloqueoCuentaMinutos", 15));
        options.Lockout.AllowedForNewUsers = true;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<LabTrackDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
    options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    options.Cookie.Name = "LabTrack.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

var app = builder.Build();

if (args.Contains("--crear-usuario", StringComparer.Ordinal))
{
    Environment.ExitCode = await CrearUsuarioCommand.EjecutarAsync(app.Services, app.Configuration);
    return;
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

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
