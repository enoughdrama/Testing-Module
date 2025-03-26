using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TestingApp.Models;
using TestingApp.Services;

namespace TestingApp.Windows
{
    public partial class TestingWindow : Window
    {
        private readonly User _user;
        private List<Question> _questions = new List<Question>();
        private List<UserAnswer> _userAnswers = new List<UserAnswer>();
        private int _currentQuestionIndex = 0;
        private bool _isLoading = true;

        // Timer related fields
        private DispatcherTimer _questionTimer;
        private const int QuestionTimeLimit = 60; // Time limit in seconds
        private int _remainingTime;

        public TestingWindow(User user)
        {
            InitializeComponent();
            _user = user;

            // Initialize timer
            _questionTimer = new DispatcherTimer();
            _questionTimer.Interval = TimeSpan.FromSeconds(1);
            _questionTimer.Tick += QuestionTimer_Tick;

            // Show loading message
            TxtQuestionText.Text = "Loading questions...";
            TxtQuestionText.Opacity = 1;
            BtnNext.IsEnabled = false;

            // Handle window closing to prevent issues with timer
            this.Closing += (s, e) =>
            {
                if (_questionTimer.IsEnabled)
                {
                    _questionTimer.Stop();
                }
            };

            this.Loaded += async (s, e) =>
            {
                try
                {
                    // Load questions asynchronously after window is loaded
                    await LoadQuestionsAsync();
                }
                catch (Exception ex)
                {
                    // Only show error if window is still open
                    if (this.IsLoaded)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                    }
                }
            };
        }

        private async System.Threading.Tasks.Task LoadQuestionsAsync()
        {
            try
            {
                _isLoading = true;
                _questions = await ApiService.Instance.GetAllQuestions();

                foreach (var question in _questions)
                {
                    _userAnswers.Add(new UserAnswer
                    {
                        Question = question,
                        SelectedAnswers = new List<Answer>(),
                        FreeTextAnswer = string.Empty
                    });
                }

                // Display the first question once loaded
                _isLoading = false;
                BtnNext.IsEnabled = true;

                if (this.IsLoaded) // Only display if window is still open
                {
                    DisplayCurrentQuestion();
                }
            }
            catch (Exception ex)
            {
                if (this.IsLoaded) // Only show error if window is still open
                {
                    MessageBox.Show($"Failed to load questions: {ex.Message}",
                                   "Error",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                    Close();
                }
            }
        }

        private void QuestionTimer_Tick(object sender, EventArgs e)
        {
            if (!this.IsLoaded) return; // Don't process timer events if window is closed

            _remainingTime--;
            TxtTimer.Text = _remainingTime.ToString();

            // Change color to red when time is running out (less than 10 seconds)
            if (_remainingTime <= 10)
            {
                TxtTimer.Foreground = new SolidColorBrush(Colors.Red);
            }

            if (_remainingTime <= 0)
            {
                // Stop the timer
                _questionTimer.Stop();

                // Mark current question as incorrect
                var userAnswer = _userAnswers[_currentQuestionIndex];
                userAnswer.IsCorrect = false;
                userAnswer.TimedOut = true;

                // Move to next question automatically without showing message box
                if (_currentQuestionIndex < _questions.Count - 1)
                {
                    _currentQuestionIndex++;
                    if (this.IsLoaded) // Only display if window is still open
                    {
                        DisplayCurrentQuestion();
                    }
                }
                else
                {
                    if (this.IsLoaded) // Only finish if window is still open
                    {
                        FinishTest();
                    }
                }
            }
        }

        private void StartQuestionTimer()
        {
            // Check if window is still open
            if (!this.IsLoaded) return;

            // Reset timer
            _remainingTime = QuestionTimeLimit;
            TxtTimer.Text = _remainingTime.ToString();
            TxtTimer.Foreground = new SolidColorBrush(Colors.White);

            // Start the timer
            _questionTimer.Start();
        }

        private void DisplayCurrentQuestion()
        {
            // Check if window is still open
            if (!this.IsLoaded || _isLoading) return;

            // Stop any existing timer
            if (_questionTimer.IsEnabled)
            {
                _questionTimer.Stop();
            }

            var question = _questions[_currentQuestionIndex];
            var userAnswer = _userAnswers[_currentQuestionIndex];

            // Update question number and text
            TxtQuestionNumber.Opacity = 0;
            TxtQuestionText.Opacity = 0;

            TxtQuestionNumber.Text = $"Question {_currentQuestionIndex + 1} of {_questions.Count}";
            TxtQuestionText.Text = question.Text;

            // Setup animations
            AnimateFadeIn(TxtQuestionNumber, 0);
            AnimateFadeIn(TxtQuestionText, 100);

            // Handle question image
            if (!string.IsNullOrEmpty(question.ImageUrl))
            {
                try
                {
                    ImgQuestion.Source = new BitmapImage(new Uri(question.ImageUrl));
                    ImgQuestionContainer.Visibility = Visibility.Visible;
                    ImgQuestion.Opacity = 0;
                    AnimateFadeIn(ImgQuestion, 200);
                }
                catch (Exception ex)
                {
                    ImgQuestionContainer.Visibility = Visibility.Collapsed;
                    if (this.IsLoaded) // Only show error if window is still open
                    {
                        MessageBox.Show($"Failed to load image: {ex.Message}", "Image Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                ImgQuestionContainer.Visibility = Visibility.Collapsed;
            }

            // Display answer options
            PnlAnswers.Children.Clear();

            // Always add answer options regardless of whether there's an image
            switch (question.Type)
            {
                case QuestionType.SingleChoice:
                    CreateRadioButtons(question, userAnswer);
                    break;

                case QuestionType.MultipleChoice:
                    CreateCheckBoxes(question, userAnswer);
                    break;

                case QuestionType.FreeText:
                    CreateTextBox(userAnswer);
                    break;
            }

            // Ensure the answer section is visible
            if (PnlAnswers.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollViewer.Visibility = Visibility.Visible;
            }

            // Update next button text
            BtnNext.Content = _currentQuestionIndex == _questions.Count - 1 ? "Finish" : "Next Question";

            // Start the timer for this question
            StartQuestionTimer();
        }

        private void AnimateFadeIn(UIElement element, int delayMs)
        {
            element.Opacity = 0;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void CreateRadioButtons(Question question, UserAnswer userAnswer)
        {
            // Create a radio button group to ensure exclusive selection
            string groupName = $"Question{question.Id}";

            int index = 0;
            foreach (var answer in question.Answers)
            {
                var container = new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3E8")),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 15),
                    Padding = new Thickness(15),
                    Background = new SolidColorBrush(Colors.White),
                    Opacity = 0
                };

                var radioButton = new RadioButton
                {
                    Content = answer.Text,
                    FontSize = 16,
                    Tag = answer,
                    Margin = new Thickness(5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    GroupName = groupName
                };

                if (userAnswer.SelectedAnswers.Any(a => a.Id == answer.Id))
                {
                    radioButton.IsChecked = true;
                }

                container.Child = radioButton;
                PnlAnswers.Children.Add(container);
                AnimateFadeIn(container, 300 + (index * 100));

                index++;
            }
        }

        private void CreateCheckBoxes(Question question, UserAnswer userAnswer)
        {
            int index = 0;
            foreach (var answer in question.Answers)
            {
                var container = new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3E8")),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 15),
                    Padding = new Thickness(15),
                    Background = new SolidColorBrush(Colors.White),
                    Opacity = 0
                };

                var checkBox = new CheckBox
                {
                    Content = answer.Text,
                    FontSize = 16,
                    Tag = answer,
                    Margin = new Thickness(5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                if (userAnswer.SelectedAnswers.Any(a => a.Id == answer.Id))
                {
                    checkBox.IsChecked = true;
                }

                container.Child = checkBox;
                PnlAnswers.Children.Add(container);
                AnimateFadeIn(container, 300 + (index * 100));

                index++;
            }
        }

        private void CreateTextBox(UserAnswer userAnswer)
        {
            var container = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3E8")),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5),
                Background = new SolidColorBrush(Colors.White),
                Opacity = 0
            };

            var textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 150,
                FontSize = 16,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = userAnswer.FreeTextAnswer,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10)
            };

            container.Child = textBox;
            PnlAnswers.Children.Add(container);
            AnimateFadeIn(container, 300);
        }

        private void SaveCurrentAnswer()
        {
            if (_isLoading) return;

            var question = _questions[_currentQuestionIndex];
            var userAnswer = _userAnswers[_currentQuestionIndex];

            userAnswer.SelectedAnswers.Clear();
            userAnswer.FreeTextAnswer = string.Empty;

            switch (question.Type)
            {
                case QuestionType.SingleChoice:
                    foreach (var child in PnlAnswers.Children)
                    {
                        if (child is Border radioBorder && radioBorder.Child is RadioButton radioButton && radioButton.IsChecked == true)
                        {
                            userAnswer.SelectedAnswers.Add((Answer)radioButton.Tag);
                        }
                    }
                    break;

                case QuestionType.MultipleChoice:
                    foreach (var child in PnlAnswers.Children)
                    {
                        if (child is Border checkBorder && checkBorder.Child is CheckBox checkBox && checkBox.IsChecked == true)
                        {
                            userAnswer.SelectedAnswers.Add((Answer)checkBox.Tag);
                        }
                    }
                    break;

                case QuestionType.FreeText:
                    if (PnlAnswers.Children.Count > 0 && PnlAnswers.Children[0] is Border textBorder &&
                        textBorder.Child is TextBox textBox)
                    {
                        userAnswer.FreeTextAnswer = textBox.Text;
                    }
                    break;
            }

            // Determine if answer is correct
            if (question.Type == QuestionType.FreeText)
            {
                // For free text, just check if there's an answer
                userAnswer.IsCorrect = !string.IsNullOrWhiteSpace(userAnswer.FreeTextAnswer);
            }
            else if (question.Type == QuestionType.SingleChoice)
            {
                // For single choice, check if the selected answer is correct
                userAnswer.IsCorrect = userAnswer.SelectedAnswers.Count == 1 &&
                                      userAnswer.SelectedAnswers[0].IsCorrect;
            }
            else if (question.Type == QuestionType.MultipleChoice)
            {
                // For multiple choice, all correct answers must be selected and no incorrect ones
                var correctAnswers = question.Answers.Where(a => a.IsCorrect).ToList();
                var selectedCorrectAnswers = userAnswer.SelectedAnswers.Where(a => a.IsCorrect).ToList();
                var selectedIncorrectAnswers = userAnswer.SelectedAnswers.Where(a => !a.IsCorrect).ToList();

                userAnswer.IsCorrect = selectedCorrectAnswers.Count == correctAnswers.Count &&
                                      !selectedIncorrectAnswers.Any();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded || _isLoading) return;

            // Stop timer for current question
            _questionTimer.Stop();

            TxtError.Visibility = Visibility.Collapsed;
            SaveCurrentAnswer();

            var userAnswer = _userAnswers[_currentQuestionIndex];
            var question = _questions[_currentQuestionIndex];

            bool isValid = true;
            if (question.Type == QuestionType.SingleChoice && !userAnswer.SelectedAnswers.Any())
            {
                isValid = false;
            }
            else if (question.Type == QuestionType.FreeText && string.IsNullOrWhiteSpace(userAnswer.FreeTextAnswer))
            {
                isValid = false;
            }

            if (!isValid)
            {
                TxtError.Text = "Please provide an answer before proceeding.";
                TxtError.Visibility = Visibility.Visible;
                // Restart timer since we're staying on this question
                StartQuestionTimer();
                return;
            }

            if (_currentQuestionIndex < _questions.Count - 1)
            {
                _currentQuestionIndex++;
                DisplayCurrentQuestion();
            }
            else
            {
                FinishTest();
            }
        }

        private async void FinishTest()
        {
            if (!this.IsLoaded || _isLoading) return;

            // Show loading indicator
            BtnNext.IsEnabled = false;
            BtnNext.Content = "Saving results...";

            var result = new TestResult
            {
                User = _user,
                CompletionTime = DateTime.Now,
                UserAnswers = _userAnswers
            };

            try
            {
                // Prepare test results for API in a better format
                var apiResultData = new
                {
                    completionTime = result.CompletionTime,
                    userAnswers = result.UserAnswers.Select(ua => new
                    {
                        question = ua.Question.Id.ToString(),
                        questionText = ua.Question.Text, // Add question text to help with matching
                        selectedAnswers = ua.SelectedAnswers.Select(a => a.Id.ToString()).ToList(),
                        selectedAnswerText = ua.SelectedAnswers.Count > 0 ? ua.SelectedAnswers[0].Text : null, // Add answer text
                        freeTextAnswer = ua.FreeTextAnswer,
                        isCorrect = ua.IsCorrect,
                        timedOut = ua.TimedOut
                    }).ToList()
                };

                // Save results to API
                bool success = await ApiService.Instance.SaveTestResult(apiResultData);

                if (success)
                {
                    // Create result window but don't show it yet
                    var resultWindow = new ResultWindow(result);
                    resultWindow.Owner = this;

                    // Check if current window is still loaded before showing the result
                    if (this.IsLoaded)
                    {
                        resultWindow.ShowDialog();
                        this.Close();
                    }
                }
                else
                {
                    if (this.IsLoaded) // Only show error if window is still open
                    {
                        MessageBox.Show("Failed to save test results. Please try again.",
                                       "Error",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Error);

                        // Re-enable finish button
                        BtnNext.IsEnabled = true;
                        BtnNext.Content = "Finish";
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.IsLoaded) // Only show error if window is still open
                {
                    MessageBox.Show($"Error saving results: {ex.Message}",
                                   "Error",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);

                    // Re-enable finish button
                    BtnNext.IsEnabled = true;
                    BtnNext.Content = "Finish";
                }
            }
        }
    }
}