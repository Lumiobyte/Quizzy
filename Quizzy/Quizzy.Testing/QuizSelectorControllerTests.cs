using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Quizzy.Controllers;
using Quizzy.Core.DTOs;
using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using System.Linq;

namespace Quizzy.Testing
{
    [TestFixture]
    public sealed class QuizSelectorControllerTests
    {
        QuizSelectorController quizSelectorController;
        Mock<IUnitOfWork> mockRepository;
        Mock<IQuizRepository> mockQuizRepository;

        static Guid requestingUserGuid = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        static Guid otherUserGuid = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab");

        Quiz[] quizzes = new Quiz[]
        {
            new Quiz
            {
                Id = Guid.NewGuid(),
                Title = "Requester's Quiz",
                QuizAuthorId = requestingUserGuid,
                Questions = new List<QuizQuestion>()
            },
            new Quiz
            {
                Id = Guid.NewGuid(),
                Title = "Requester's better Quiz",
                QuizAuthorId = requestingUserGuid,
                Questions = new List<QuizQuestion>()
                {
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        Text = "cool question",
                        Answers = new List<QuizAnswer>()
                    },
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        Text = "cooler question",
                        Answers = new List<QuizAnswer>()
                    }
                }
            },
            new Quiz
            {
                Id = Guid.NewGuid(),
                Title = "Other User's Quiz",
                QuizAuthorId = otherUserGuid,
                Questions = new List<QuizQuestion>()
            }
        };

        [SetUp]
        public void Setup()
        {
            mockRepository = new Mock<IUnitOfWork>();
            mockQuizRepository = new Mock<IQuizRepository>();
            quizSelectorController = new QuizSelectorController(mockRepository.Object);

            mockRepository.Setup(u => u.Quizzes).Returns(mockQuizRepository.Object);
            mockQuizRepository.Setup(r => r.GetAllWithDetailsAsync())
                .ReturnsAsync(quizzes.ToList());
        }

        [Test]
        public async Task TestGetAllQuizzes()
        {
            // Act
            var result = await quizSelectorController.GetAll();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            var payload = ok.Value as List<QuizResponse>;
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload!.Count, Is.EqualTo(3));
            CollectionAssert.AreEquivalent(
                new[] { "Requester's Quiz", "Requester's better Quiz", "Other User's Quiz" },
                payload.Select(p => p.name));
        }

        [Test]
        public async Task TestGetAllById()
        {
            // Act
            var result = await quizSelectorController.GetAllById(requestingUserGuid);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            var payload = ok.Value as List<QuizResponse>;
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload!.Count, Is.EqualTo(2));
            Assert.That(payload.All(p => p.authorId == requestingUserGuid), Is.True);
        }

        [Test]
        public async Task TestGetAllByName()
        {
            // Act
            var result = await quizSelectorController.GetAllByName("better");

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            var payload = ok.Value as List<QuizResponse>;
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload!.Count, Is.EqualTo(1));
            Assert.That(payload[0].name, Is.EqualTo("Requester's better Quiz"));
        }

        [Test]
        public async Task TestGetAllByIdAndName()
        {
            // Act
            var result = await quizSelectorController.GetAllByIdAndName(requestingUserGuid, "better");

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            var payload = ok.Value as List<QuizResponse>;
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload!.Count, Is.EqualTo(1));
            Assert.That(payload[0].authorId, Is.EqualTo(requestingUserGuid));
            Assert.That(payload[0].name, Is.EqualTo("Requester's better Quiz"));
        }
    }
}