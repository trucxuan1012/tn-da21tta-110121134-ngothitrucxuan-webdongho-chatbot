using XuanEmart.Areas.Admin.Repository;
using XuanEmart.Data;
using XuanEmart.Hubs;
using XuanEmart.Models;
using XuanEmart.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XuanEmart.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DataContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

//Set Session
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.IsEssential = true;
});

//Res Auth User
builder.Services.AddIdentity<AppUserModel, IdentityRole>()
	.AddEntityFrameworkStores<DataContext>().AddDefaultTokenProviders();
builder.Services.AddSignalR();
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.Configure<IdentityOptions>(options =>
{
	// Password settings.
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = true;
	options.Password.RequiredLength = 4;
	options.Password.RequiredUniqueChars = 1;

	options.User.RequireUniqueEmail = false;
});

var app = builder.Build();

// Middleware xử lý lỗi cho API: trả về JSON thay vì redirect khi gặp 401/403
app.Use(async (context, next) =>
{
    await next();
    if (context.Request.Path.StartsWithSegments("/api") &&
        (context.Response.StatusCode == 401 || context.Response.StatusCode == 403))
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"Unauthorized or Forbidden\"}");
    }
});

app.UseStatusCodePagesWithRedirects("/Home/Error?statuscode={0}");

//Đăng Ký Session
app.UseSession();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
	name: "category",
	pattern: "/category/{Slug?}",
	defaults : new { controller = "category", action = "index" });

app.MapControllerRoute(
    name: "brand",
    pattern: "/brand/{Slug?}",
    defaults: new { controller = "brand", action = "index" });

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chathub");
//Seeding Data
var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
SeedData.SeedingData(context);

//app.MapRazorPages();
app.Run();
