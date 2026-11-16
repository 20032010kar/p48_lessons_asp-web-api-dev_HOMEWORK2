using System.Globalization;
using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using p48_lessons_asp.Data;
using p48_lessons_asp.Models;
using p48_lessons_asp.Services.Random;
using p48_lessons_asp.Services.Slug;
using Slugify;
using Serilog;

namespace p48_lessons_asp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Running web-application");
                var builder = WebApplication.CreateBuilder(args);

                // 1. Подключаем Serilog для логирования
                builder.Host.UseSerilog();

                // 2. Настраиваем культуру для FluentValidation (чтобы ошибки были на английском)
                ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en");

                // 3. Регистрация кастомных сервисов
                builder.Services.AddSingleton<IRandomService, RandomService>();
                builder.Services.AddSingleton<ISlugHelper, SlugHelper>();
                builder.Services.AddSingleton<ISlugService, SlugService>();

                // 4. Регистрация AutoMapper и FluentValidation
                builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
                builder.Services.AddValidatorsFromAssemblyContaining<Program>();

                // 5. Проверяем и получаем строку подключения из appsettings.json
                // (Убедись, что в appsettings.json строка называется именно "DefaultConnection")
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("No connection string found. Please provide 'DefaultConnection' in appsettings.json");
                }

                // 6. Регистрируем DataContext ОДИН раз (без дублирования)
                builder.Services.AddDbContext<DataContext>(options =>
                    options.UseSqlServer(connectionString)
                );

                // 7. Контроллеры и документация Swagger
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                    {
                        Title = "ASP.Net Core WebAPI Lessons",
                        Version = "v1.0",
                        Description = "Documentation for REST API on ASP.Net Core",
                        Contact = new Microsoft.OpenApi.OpenApiContact
                        {
                            Name = "dsgnrr",
                            Email = "dsgnrr@rnicrosoft-supprot.com"
                        }
                    });

                    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }
                    c.EnableAnnotations();
                });

                var app = builder.Build();

                // 8. Настройка middleware
                app.UseSerilogRequestLogging();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application stopped with fatal error");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}