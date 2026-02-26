using CATSTracking.Library.Data;
using CATSTracking.Library.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        // Configure cookie authentication
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Identity/PathFinder"; // Redirect here if not authenticated
            });

        builder.Services.AddControllersWithViews();

        builder.Services.AddHttpClient<ApiService>(client =>
        {
            string ApiUri = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_URI")) ? "http://localhost:6000" : Environment.GetEnvironmentVariable("API_URI");
            client.BaseAddress = new Uri(ApiUri);
        });

        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(60);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

       


        // EF Core implementation guided by Microsoft documentation
        // (Microsoft, 2023)

            //Adding EF Core
        //builder.Services.AddDbContext<CATSContext>(options =>
            //options.UseSqlServer(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));
           // options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),b => b.MigrationsAssembly("CATSTracking.UI")));


        //.Net identity implementation guided by (Microsoft, 2024) and (Authentication made easy with asp.net core identity in .net 8, 2024)
        //Adding .NET Core Identity
    //     builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    // .AddEntityFrameworkStores<CATSContext>()
    // .AddDefaultTokenProviders();

        var app = builder.Build();


        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                var path = ctx.File.PhysicalPath?.Replace("\\", "/");
                if (path != null && path.EndsWith("service-worker.js",
                StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers["Cache-Control"] =
                    "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
            }
        });


        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}

/*
 * References
 * Agnihotri, A. [s.a.]. AJAX in .net core. [Online]. Available at: https://www.c-sharpcorner.com/article/ajax-in-net-core/ [Accessed 18 October 2024].
 * Authentication made easy with asp.net core identity in .net 8 2024. [Online]. Available at: https://www.youtube.com/watch?v=S0RSsHKiD6Y [Accessed 18 October 2024].
 * MESCIUS inc. 2024. AJAX data binding | asp.net core mvc controls | componentone, 2024. [Online]. Available at: https://developer.mescius.com/componentone/docs/mvc/online-mvc-core/AjaxBinding.html [Accessed 18 October 2024].
 * Microsoft. 2024. Introduction to identity on asp.net core, 26 April 2024. [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-8.0 [Accessed 18 October 2024].
 * Microsoft. 2023. Razor pages with entity framework core in asp.net core - tutorial 1 of 8, 28 July 2023. [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/data/ef-rp/intro?view=aspnetcore-8.0 [Accessed 18 October 2024].
 */