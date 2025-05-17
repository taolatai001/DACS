using CSDL.Configurations;
using CSDL.Data;
using CSDL.Models;
using CSDL.Services;
using DinkToPdf.Contracts;
using DinkToPdf;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ✅ Cấu hình SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Cấu hình Identity
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ✅ Cấu hình xác thực Google
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.Events.OnCreatingTicket = async context =>
    {
        var email = context.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
        var roleManager = context.HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = context.Principal.Identity.Name
            };
            await userManager.CreateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, "Admin") && email == "tailacanhsat@gmail.com")
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            await userManager.AddToRoleAsync(user, "Admin");
        }
    };
});

// ✅ Thêm Authorization
builder.Services.AddAuthorization();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<CertificateService>();



// ✅ Thêm Controllers với Views
builder.Services.AddControllersWithViews();
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddHttpClient<OpenAIService>();

builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));
builder.Services.AddHttpClient<OpenRouterService>();
builder.Services.AddScoped<OpenRouterService>();

var app = builder.Build();

// ✅ Tạo Database và chạy Migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        dbContext.Database.Migrate();  // ✅ Kiểm tra lỗi Migration để không gây crash
    }
    catch (Exception ex)
    {
        Console.WriteLine("⚠️ Lỗi Migration: " + ex.Message);
    }

    // ✅ Tạo Role Admin & User nếu chưa có
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = new string[] { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!roleManager.RoleExistsAsync(role).Result)
        {
            roleManager.CreateAsync(new IdentityRole(role)).Wait();
        }
    }

    // ✅ Tạo tài khoản Admin nếu chưa có (thủ công)
    var userManager = services.GetRequiredService<UserManager<User>>();
    var adminEmail = "tailacanhsat@gmail.com";
    var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
    if (adminUser == null)
    {
        var newAdmin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Administrator",
            PhoneNumber = "0123456789",
            EmailConfirmed = true
        };

        var result = userManager.CreateAsync(newAdmin, "Admin@123").Result;
        if (result.Succeeded)
        {
            userManager.AddToRoleAsync(newAdmin, "Admin").Wait();
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();