using Microsoft.AspNetCore.Authentication;
using Quizzy.Core;
using Quizzy.Core.Services;
using Quizzy.Core.Repositories;

namespace Quizzy
{
    public class Program
    {
        static bool seedOnStartup = true;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // DI Services
            builder.Services.AddDbContext<QuizzyDbContext>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ILoginService, LoginService>();
            builder.Services.AddScoped<IQuizCreationService, QuizCreationService>();
            builder.Services.AddScoped<IReportingService, ReportingService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

            var app = builder.Build();

            if (seedOnStartup)
            {
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<QuizzyDbContext>();
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
