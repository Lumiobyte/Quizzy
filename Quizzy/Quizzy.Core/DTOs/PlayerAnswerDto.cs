namespace Quizzy.Core.DTOs
{
    public record PlayerAnswerDto(string Name, int SelectedIndex, double ResponseTimeSeconds);
}
