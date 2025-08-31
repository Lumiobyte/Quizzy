using NUnit.Framework;
using Moq;
using Quizzy.Core.Services;
using Quizzy.Core.Repositories;
using Quizzy.Core.Entities;
using Quizzy.Core.DTOs;

namespace Quizzy.Testing
{
    [TestFixture]
    public sealed class QuizCreationServiceTests
    {
        QuizCreationService quizCreationService;
        Mock<IUnitOfWork> mockRepository;

        [SetUp]
        public void Setup()
        {
            mockRepository = new Mock<IUnitOfWork>();
            quizCreationService = new QuizCreationService(mockRepository.Object);

            mockRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        [Test]
        public async Task TestCanCreateQuiz()
        {
            // Arrange
            Quiz? newQuiz = null;
            mockRepository.Setup(r => r.Quizzes.AddAsync(It.IsAny<Quiz>()))
                          .Callback<Quiz>(q => newQuiz = q)
                          .Returns(Task.CompletedTask);

            var model = new QuizCreatorModel
            {
                Title = "New Quiz",
                Questions = new List<QuestionModel>
                {
                    new QuestionModel
                    {
                        Text = "What is 2 + 2?",
                        Answers = new List<AnswerModel>
                        {
                            new AnswerModel { Text = "3", IsCorrect = false },
                            new AnswerModel { Text = "4", IsCorrect = true }
                        }
                    }
                }
            };
            var userGuid = Guid.NewGuid();

            // Act
            await quizCreationService.GenerateQuiz(model, userGuid);

            // Assert
            mockRepository.Verify(r => r.Quizzes.AddAsync(It.IsAny<Quiz>()), Times.Once);
            ValidateQuiz(newQuiz);
        }

        [Test]
        public async Task TestCanUpdateQuiz()
        {
            // Arrange
            Quiz? newQuiz = null;
            mockRepository.Setup(r => r.Quizzes.AddAsync(It.IsAny<Quiz>()))
                          .Callback<Quiz>(q => newQuiz = q)
                          .Returns(Task.CompletedTask);

            var existingId = Guid.NewGuid();
            mockRepository.Setup(r => r.Quizzes.Remove(existingId));
            var model = new QuizCreatorModel
            {
                Title = "Updated Quiz",
                QuizSourceId = existingId,
                Questions = new List<QuestionModel>
                {
                    new QuestionModel
                    {
                        Text = "really interesting question",
                        Answers = new List<AnswerModel> { new() { Text = "the correct answer", IsCorrect = false } }
                    }
                }
            };

            // Act
            await quizCreationService.UpdateQuiz(model, Guid.NewGuid());

            // Assert
            mockRepository.Verify(r => r.Quizzes.Remove(existingId), Times.Once);
            mockRepository.Verify(r => r.Quizzes.AddAsync(It.IsAny<Quiz>()), Times.Once);
            ValidateQuiz(newQuiz);
            Assert.That(newQuiz!.Id, Is.EqualTo(existingId));
        }

        [Test]
        public async Task TestCanAssertQuizTypeCorrectly()
        {
            // Arrange
            Quiz? newQuiz = null;
            mockRepository.Setup(r => r.Quizzes.AddAsync(It.IsAny<Quiz>()))
                          .Callback<Quiz>(q => newQuiz = q)
                          .Returns(Task.CompletedTask);

            var model = new QuizCreatorModel
            {
                Title = "New Quiz",
                Questions = new List<QuestionModel>
                {
                    new QuestionModel
                    {
                        Text = "what is 1 + 1?",
                        Answers = new List<AnswerModel>
                        {
                            new AnswerModel { Text = "2", IsCorrect = false },
                        }
                    },
                    new QuestionModel
                    {
                        Text = "what is 1 + 1?",
                        Answers = new List<AnswerModel>
                        {
                            new AnswerModel { Text = "2", IsCorrect = true },
                            new AnswerModel { Text = "3", IsCorrect = false },
                        }
                    }
                }
            };
            var userGuid = Guid.NewGuid();

            // Act
            await quizCreationService.GenerateQuiz(model, userGuid);

            // Assert
            mockRepository.Verify(r => r.Quizzes.AddAsync(It.IsAny<Quiz>()), Times.Once);
            ValidateQuiz(newQuiz);
            Assert.That(newQuiz!.Questions.ToList()[0].QuestionType == Core.Enums.QuestionType.ShortAnswer);
            Assert.That(newQuiz!.Questions.ToList()[1].QuestionType == Core.Enums.QuestionType.MultipleChoice);
        }

        void ValidateQuiz(Quiz? quiz)
        {
            Assert.That(quiz, Is.Not.Null);
            Assert.That(quiz!.Id, Is.Not.EqualTo(Guid.Empty));
            var questions = quiz.Questions.ToList();
            var answers = quiz.Questions.SelectMany(q => q.Answers).ToList();
            Assert.That(questions, Is.Not.Empty);
            Assert.That(answers, Is.Not.Empty);
            Assert.That(questions.All(q => q.Id != Guid.Empty && q.QuizId == quiz.Id), Is.True);
            Assert.That(answers.All(a => a.Id != Guid.Empty && questions.Any(q => q.Answers.Any(qa => qa.Id == a.Id))), Is.True);
        }
    }
}
