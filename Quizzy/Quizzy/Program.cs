using Microsoft.EntityFrameworkCore;
using Quizzy.Core;
using Quizzy.Core.Repositories;
using Quizzy.Web.Hubs;
using Quizzy.Web.Services;

namespace Quizzy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Services
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<QuizzyDbContext>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<SessionCoordinator>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<GameHub>("/gamehub");
            app.MapHub<UserHub>("/userhub");

            app.Run();
        }
    }
}
