using System;
using System.Collections.Generic;
using System.Linq;

namespace TestingApp.Models
{
    public class TestResult
    {
        public User User { get; set; }
        public DateTime CompletionTime { get; set; }
        public List<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
        public int TotalQuestions => UserAnswers.Count;
        public int CorrectAnswers => UserAnswers.Count(a => a.IsCorrect);
        public double Score => (double)CorrectAnswers / TotalQuestions * 100;
    }

    public class UserAnswer
    {
        public Question Question { get; set; }
        public List<Answer> SelectedAnswers { get; set; } = new List<Answer>();
        public string FreeTextAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public bool TimedOut { get; set; }
    }
}