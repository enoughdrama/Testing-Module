using System.Collections.Generic;

namespace TestingApp.Models
{
    public enum QuestionType
    {
        SingleChoice,
        MultipleChoice,
        FreeText
    }

    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }
        public QuestionType Type { get; set; }
        public List<Answer> Answers { get; set; } = new List<Answer>();
    }

    public class Answer
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}