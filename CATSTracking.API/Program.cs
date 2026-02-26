using CATSTracking.Library.Data;
using CATSTracking.Library.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;

namespace CATSTracking.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Enable detailed logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Error);

            #region Inject EF Core
            // EF Core implementation guided by Microsoft documentation
            // (Microsoft, 2023)

            //Adding EF Core
            builder.Services.AddDbContext<CATSContext>(options =>
            //    options.UseSqlServer(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            #endregion

            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-9.0
            builder.Services.AddScoped<SMSService>();
            builder.Services.AddScoped<EventLogService>();


            #region Identity

            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<CATSContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthorization();

            #endregion


            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();

            #region Customize Swagger Documentation
            builder.Services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                options.SwaggerDoc("v1", new OpenApiInfo { Title = "CATS API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT (JSON Web Token) obtained from the /v1/token/new endpoint"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            #endregion


            var app = builder.Build();

            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("\n\n\n\n=== INCOMING REQUEST ===");
                logger.LogInformation("\t\tPath: {Path}", context.Request.Path);
                logger.LogInformation("\t\tMethod: {Method}", context.Request.Method);
                logger.LogInformation("\t\tQuery: {Query}", context.Request.QueryString);
                logger.LogInformation("\t\tHost: {Host}", context.Request.Host);
                logger.LogInformation("\t\tScheme: {Scheme}", context.Request.Scheme);

                // Log headers
                foreach (var header in context.Request.Headers)
                {
                    logger.LogInformation("\t\tHeader: {Key} = {Value}", header.Key,header.Value);
                }

                await next();

                logger.LogInformation("\t\tResponse Status: {StatusCode}", context.Response.StatusCode);
                logger.LogInformation("=== END REQUEST ===\n\n\n\n");
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
