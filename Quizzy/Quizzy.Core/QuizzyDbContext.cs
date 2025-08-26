using Microsoft.EntityFrameworkCore;
using Quizzy.Core.Entities;

namespace Quizzy.Core
{
    public class QuizzyDbContext : DbContext
    {
        
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }
        public DbSet<QuizPlayer> QuizPlayers { get; set; }
        public DbSet<QuizSession> QuizSessions { get; set; }
        public DbSet<PlayerAnswer> PlayerAnswers { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }


        public string DbPath { get; }

        public QuizzyDbContext()
        {
            DbPath = System.IO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Quizzy.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }

    }
}
