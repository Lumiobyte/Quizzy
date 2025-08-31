using Quizzy.Core.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Quizzy.Core.Services
{
    public class ReportingService(IEmailService emailService) : IReportingService
    {
        public async Task SendReportsForSession(QuizSession session) // This assumes that the session passed in contains ALL details of the quiz, and that no loading from DB is necessary
        {
            var attachments = new List<string>() { await GenerateReport(session) };
            await emailService.SendEmailAsync(
                session.QuizHost,
                $"Report for Quiz Session: {session.Quiz.Title}",
                $"The quiz session '{session.Quiz.Title}' has concluded. You can view the results in your dashboard.",
                attachments.ToArray()
            );

            foreach (var file in attachments) // This should only run once but just in case
            {
                try { if (File.Exists(file)) File.Delete(file); }
                catch { /* uh i guess it isnt there :/ */ }
            }
        }

        public async Task<string> GenerateReport(QuizSession session) // Make sure that the resulting file is deleted after use to avoid unnessessary clogging
        {
            var orderedPlayers = GetPlayersByScoreOrder(session);
            var quizTitle = session.Quiz?.Title ?? "Untitled";
            var gamePin = session.GamePin ?? string.Empty;
            var cacheDir = Path.Combine(AppContext.BaseDirectory, "ReportCache");
            Directory.CreateDirectory(cacheDir);

            var fileName = $"QuizReport_{session.Id:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.pdf";
            var filePath = Path.Combine(cacheDir, fileName);

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Text("Quizzy - Session Report")
                                .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                            row.ConstantItem(120).AlignRight().Text(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"));
                        });

                        page.Content().Column(col =>
                        {
                            col.Spacing(12);

                            col.Item().Text($"Quiz: {quizTitle}").SemiBold();
                            if (!string.IsNullOrWhiteSpace(gamePin))
                                col.Item().Text($"Session PIN: {gamePin}");

                            col.Item().Text($"Players: {orderedPlayers.Count}");

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(36);   // Placement/Score
                                    columns.RelativeColumn(3);    // Player
                                    columns.ConstantColumn(80);   // Correct
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("#");
                                    header.Cell().Element(HeaderCell).Text("Player");
                                    header.Cell().Element(HeaderCell).Text("Correct");
                                });

                                for (int i = 0; i < orderedPlayers.Count; i++)
                                {
                                    var player = orderedPlayers[i];
                                    var correct = player.Answers?.Count(a => a.IsCorrect) ?? 0;

                                    table.Cell().Element(DataCell).Text((i + 1).ToString());
                                    table.Cell().Element(DataCell).Text(player.Name ?? "Unknown");
                                    table.Cell().Element(DataCell).Text(correct.ToString());
                                }
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                })
                .GeneratePdf(filePath);
            });

            return filePath;
        }

        static IContainer HeaderCell(IContainer container) =>
                container.DefaultTextStyle(x => x.SemiBold())
                         .PaddingVertical(6).PaddingHorizontal(8)
                         .Background(Colors.Grey.Lighten3)
                         .BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

        static IContainer DataCell(IContainer container) =>
            container.PaddingVertical(6).PaddingHorizontal(8)
                     .BorderBottom(1).BorderColor(Colors.Grey.Lighten3);

        List<QuizPlayer> GetPlayersByScoreOrder(QuizSession session)
            => session.Players.OrderByDescending(p => p.Answers.Sum(a => a.IsCorrect ? 1 : 0))
                .ToList();
    }
}