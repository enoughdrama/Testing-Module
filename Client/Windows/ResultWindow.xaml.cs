using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Linq;
using TestingApp.Models;

namespace TestingApp.Windows
{
    public partial class ResultWindow : Window
    {
        private readonly TestResult _result;

        public ResultWindow(TestResult result)
        {
            InitializeComponent();
            _result = result;

            SetupResultData();
        }

        private void SetupResultData()
        {
            // Calculate the score explicitly
            int totalQuestions = _result.TotalQuestions;
            int correctAnswers = _result.CorrectAnswers;
            double score = totalQuestions > 0 ? ((double)correctAnswers / totalQuestions) * 100 : 0;

            // Set user info
            TxtUserInfo.Text = $"Completed by: {_result.User.FullName}";
            TxtCompletionTime.Text = $"Completed on {_result.CompletionTime:MMM dd, yyyy} at {_result.CompletionTime:hh:mm tt}";

            // Set score
            TxtScore.Text = $"{score:F0}%";

            // Set summary stats
            TxtTotalQuestions.Text = totalQuestions.ToString();
            TxtCorrectAnswers.Text = correctAnswers.ToString();
            TxtIncorrectAnswers.Text = (totalQuestions - correctAnswers).ToString();
            TxtFinalScore.Text = $"{score:F1}%";

            // Count timed out questions
            int timedOutQuestions = _result.UserAnswers.Count(a => a.TimedOut);

            // Set feedback based on score
            string feedback;
            if (score >= 90)
            {
                feedback = "Excellent! You've demonstrated exceptional understanding of the material.";
            }
            else if (score >= 80)
            {
                feedback = "Great job! You have a strong grasp of the concepts covered.";
            }
            else if (score >= 70)
            {
                feedback = "Good work! You understand most of the key concepts.";
            }
            else if (score >= 60)
            {
                feedback = "You've passed with an adequate understanding. Consider reviewing some topics.";
            }
            else
            {
                feedback = "More review is recommended to strengthen your understanding of the material.";
            }

            // Add information about timed out questions if any
            if (timedOutQuestions > 0)
            {
                feedback += $"\n\nNote: You ran out of time on {timedOutQuestions} question{(timedOutQuestions > 1 ? "s" : "")}.";
            }

            TxtFeedback.Text = feedback;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Animate the user info panel
            var userInfoPanel = (FrameworkElement)TxtUserInfo.Parent;
            AnimateScaleIn(userInfoPanel, 0);

            // Animate the score circle
            var scoreCircle = (FrameworkElement)TxtScore.Parent;
            AnimateScaleIn(scoreCircle, 300);

            // Animate the summary section
            var summarySection = (FrameworkElement)((FrameworkElement)TxtTotalQuestions.Parent).Parent;
            AnimateSlideIn(summarySection, 600);
        }

        private void AnimateScaleIn(UIElement element, int delayMs)
        {
            element.Opacity = 0;

            // Create scale animation
            ScaleTransform scale = new ScaleTransform(0.8, 0.8);
            element.RenderTransform = scale;

            var scaleXAnim = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };

            var scaleYAnim = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };

            // Create fade animation
            var fadeAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };

            // Apply animations
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
            element.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        private void AnimateSlideIn(UIElement element, int delayMs)
        {
            element.Opacity = 0;

            // Create translate animation
            TranslateTransform translate = new TranslateTransform(0, 50);
            element.RenderTransform = translate;

            var slideAnim = new DoubleAnimation
            {
                From = 50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };

            // Create fade animation
            var fadeAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };

            // Apply animations
            translate.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            element.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}