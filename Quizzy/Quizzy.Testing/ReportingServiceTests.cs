using Moq;
using NUnit.Framework;
using QuestPDF.Infrastructure;
using Quizzy.Core.Entities;
using Quizzy.Core.Services;

namespace Quizzy.Testing
{
    [TestFixture]
    public sealed class ReportingServiceTests
    {
        Mock<IEmailService> mockEmailService;
        ReportingService reportingService;

        static UserAccount host = new UserAccount
        {
            Id = Guid.NewGuid(),
            Username = "hostUser",
            Email = "host@example.com"
        };

        static Guid quizGuid = Guid.NewGuid();

        static QuizSession session = new QuizSession
        {
            Id = Guid.NewGuid(),
            GamePin = "1234",
            QuizHostId = host.Id,
            QuizHost = host,
            QuizId = quizGuid,
            Quiz = new Quiz { Id = quizGuid, Title = "Quizzy Basics" },
            Players = new List<QuizPlayer>
            {
                new QuizPlayer
                {
                    Id = Guid.NewGuid(),
                    Name = "Alexander the Great",
                    Answers = new List<QuizAnswer>
                    {
                        new QuizAnswer { Id = Guid.NewGuid(), IsCorrect = true },
                        new QuizAnswer { Id = Guid.NewGuid(), IsCorrect = false },
                    }
                },
                new QuizPlayer
                {
                    Id = Guid.NewGuid(),
                    Name = "Julius Caesar",
                    Answers = new List<QuizAnswer>
                    {
                        new QuizAnswer { Id = Guid.NewGuid(), IsCorrect = true },
                        new QuizAnswer { Id = Guid.NewGuid(), IsCorrect = true },
                    }
                }
            }
        };

        [SetUp]
        public void Setup()
        {
            /*
             * Since this is in a testing project, the reports are saved to bin/Debug/net8.0/ReportCache rather than the actual file
             * If you encounter issues with files not being deleted, please check that folder and delete any leftover files
             * 
             * In Addition, if you want to test the the functionality of this feature, you can just use an actual emailservice instance and change the test email, then customize the test to your needs
             */

            QuestPDF.Settings.License = LicenseType.Community;
            mockEmailService = new Mock<IEmailService>();
            reportingService = new ReportingService(mockEmailService.Object);
        }

        [Test]
        public async Task SendReportsForSessionSendsEmailPdf()
        {
            // Arrange
            string? capturedSubject = null;
            string? capturedBody = null;
            string[]? capturedAttachments = null;
            UserAccount? capturedReceiver = null;

            mockEmailService
                .Setup(s => s.SendEmailAsync(
                    It.IsAny<UserAccount>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string[]>()))
                .Callback<UserAccount, string, string, string[]>((receiver, subject, body, attachments) =>
                {
                    capturedReceiver = receiver;
                    capturedSubject = subject;
                    capturedBody = body;
                    capturedAttachments = attachments;

                    // File must exist when the email is being sent
                    Assert.That(attachments, Is.Not.Null.And.Length.EqualTo(1));
                    Assert.That(Path.GetExtension(attachments[0]).ToLowerInvariant(), Is.EqualTo(".pdf"));
                    Assert.That(Directory.GetParent(attachments[0])!.FullName, Does.Contain("ReportCache"));
                    Assert.That(File.Exists(attachments[0]), Is.True, "Attachment should exist during SendEmailAsync call");
                })
                .Returns(Task.CompletedTask);

            // Act
            await reportingService.SendReportsForSession(session);

            // Assert
            mockEmailService.Verify(s => s.SendEmailAsync(
                It.Is<UserAccount>(u => u == host),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string[]>()), Times.Once);

            Assert.That(capturedReceiver, Is.EqualTo(host));
            Assert.That(capturedSubject, Is.EqualTo("Report for Quiz Session: Quizzy Basics"));
            Assert.That(capturedBody, Does.Contain("The quiz session 'Quizzy Basics' has concluded."));
            Assert.That(capturedAttachments, Is.Not.Null.And.Length.EqualTo(1));

            // File should be deleted after sending
            var path = capturedAttachments![0];
            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public async Task GenerateReportCreatsFileButDoesNotDeleteIt()
        {
            // Act
            var path = await reportingService.GenerateReport(session);

            // Assert
            Assert.That(File.Exists(path), Is.True);
            try { if (File.Exists(path)) File.Delete(path); }
            catch { Assert.That(false, "The file could not be deleted"); }
        }
    }
}