using contract_claim_system.Data;
using contract_claim_system.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register custom services
builder.Services.AddScoped<IClaimRepository>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new ClaimRepository(connectionString);
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();

// Register database initializer
builder.Services.AddTransient<auto_create_db>();

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireCoordinator", policy => policy.RequireRole("Coordinator", "Admin"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("RequireHR", policy => policy.RequireRole("HR", "Admin"));
    options.AddPolicy("RequireLecturer", policy => policy.RequireRole("Lecturer", "Admin"));
});

// Session & HTTP Context
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Middleware pipeline in correct order
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Database initialization
try
{
    using var scope = app.Services.CreateScope();
    var dbInitializer = scope.ServiceProvider.GetRequiredService<auto_create_db>();
    dbInitializer.InitializeSystem();
    Console.WriteLine("✅ Database initialization completed");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Database initialization warning: {ex.Message}");
}

Console.WriteLine("🚀 Application starting...");
app.Run();