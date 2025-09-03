using OpenAI;
using QuestPDF.Infrastructure;
using Quizzy.Core;
using Quizzy.Core.Repositories;
using Quizzy.Core.Services;

namespace Quizzy
{
    public class Program
    {
        static bool seedOnStartup = false;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            QuestPDF.Settings.License = LicenseType.Community;

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // DI Services
            builder.Services.AddDbContext<QuizzyDbContext>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ILoginService, LoginService>();
            builder.Services.AddScoped<IQuizCreationService, QuizCreationService>();
            builder.Services.AddScoped<IReportingService, ReportingService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

            builder.Services.AddSingleton(new OpenAIClient("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"));
            builder.Services.AddScoped<IAIQuizGeneratorService, AIQuizGeneratorService>();

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
