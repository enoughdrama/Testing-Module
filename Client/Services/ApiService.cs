using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using TestingApp.Models;

namespace TestingApp.Services
{
    public class EncryptedResponse
    {
        public bool Encrypted { get; set; }
        public string Data { get; set; }
    }

    public class ApiService
    {
        public static string XorEncryptToBase64(string input)
        {
            string key = "hwoqheoiqwhe12b3ub19g121938huuHSDBFdshbfwbqi";
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Input and key must be provided");
            }

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] resultBytes = new byte[inputBytes.Length];

            // XOR each byte of input with the corresponding byte of the key
            for (int i = 0; i < inputBytes.Length; i++)
            {
                resultBytes[i] = (byte)(inputBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            // Convert the result to base64
            return Convert.ToBase64String(resultBytes);
        }

        public static string XorDecryptFromBase64(string encodedInput)
        {
            string key = "hwoqheoiqwhe12b3ub19g121938huuHSDBFdshbfwbqi";

            if (string.IsNullOrEmpty(encodedInput) || string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Encoded input and key must be provided");
            }

            try
            {
                // Decode the base64 input
                byte[] encryptedBytes = Convert.FromBase64String(encodedInput);
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] resultBytes = new byte[encryptedBytes.Length];

                // XOR each byte of encrypted data with the corresponding byte of the key
                for (int i = 0; i < encryptedBytes.Length; i++)
                {
                    resultBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                // Convert the result to a string
                return Encoding.UTF8.GetString(resultBytes);
            }
            catch (FormatException ex)
            {
                throw new FormatException("Invalid base64 string", ex);
            }
        }

        // Singleton instance
        private static ApiService _instance;

        // Thread-safe lock object
        private static readonly object _lock = new object();

        // Get singleton instance
        public static ApiService Instance
        {
            get
            {
                // Double-check locking for thread safety
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ApiService();
                        }
                    }
                }
                return _instance;
            }
        }

        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string _authToken;

        // Private constructor to enforce singleton pattern
        private ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Get API base URL from configuration
            _baseUrl = "https://kip.qalyn.top/api";

            LogInfo("ApiService singleton instance created");
        }

        // Set the authentication token after login
        public void SetAuthToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                // Log error if token is empty
                LogError("Empty token provided to SetAuthToken");
                return;
            }

            LogInfo($"Setting auth token: {token.Substring(0, Math.Min(token.Length, 10))}...");
            _authToken = token;

            // Configure Authentication header correctly
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }

            // Add token with the correct format: "Bearer TOKEN_VALUE"
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            LogInfo($"Headers: {String.Join(", ", _httpClient.DefaultRequestHeaders.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
        }

        // Authentication method that only uses unique key
        public async Task<User> Authenticate(string fullName, string uniqueKey)
        {
            try
            {
                var authData = new
                {
                    uniqueKey,
                    fullName
                };

                LogInfo($"Authenticating user with key: {uniqueKey}");

                var response = await PostAsync<dynamic, AuthResponse>("/auth/authenticate", authData);

                if (response != null && response.Success && !string.IsNullOrEmpty(response.Token))
                {
                    LogInfo($"Authentication successful. Token received: {response.Token.Substring(0, Math.Min(response.Token.Length, 10))}...");

                    // Explicitly set token
                    SetAuthToken(response.Token);

                    // Create and return a User object
                    return new User
                    {
                        FullName = response.User.FullName,
                        UniqueKey = response.User.UniqueKey
                    };
                }
                else
                {
                    LogError("Authentication response was invalid or missing token");
                    throw new Exception("Authentication failed: Invalid response from server");
                }
            }
            catch (Exception ex)
            {
                LogError($"Authentication error: {ex.Message}");

                // More specific error for already used key
                if (ex.Message.Contains("already been used"))
                {
                    throw new Exception("This unique key has already been used by another student.");
                }

                throw new Exception("Authentication failed. Please check your unique key and try again.");
            }
        }

        // Get all questions
        public async Task<List<Question>> GetAllQuestions()
        {
            try
            {
                // Double check token is available
                if (string.IsNullOrEmpty(_authToken))
                {
                    LogError("Token is missing when trying to get questions");
                    throw new Exception("Authentication token is missing. Please log in again.");
                }

                LogInfo($"Getting questions with token: {_authToken.Substring(0, Math.Min(_authToken.Length, 10))}...");

                // Call the API which now returns encrypted data
                var encryptedResponse = await GetAsync<EncryptedResponse>("/questions");

                if (encryptedResponse != null && encryptedResponse.Encrypted)
                {
                    LogInfo("Received encrypted response from server");

                    // Decrypt the response
                    string decryptedJson = XorDecryptFromBase64(encryptedResponse.Data);

                    // Deserialize the decrypted JSON
                    var response = JsonSerializer.Deserialize<QuestionResponse>(decryptedJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (response != null && response.Success)
                    {
                        LogInfo($"Decrypted and retrieved {response.Data?.Count ?? 0} questions from server");

                        // Convert API question model to application question model
                        List<Question> questions = new List<Question>();

                        foreach (var apiQuestion in response.Data)
                        {
                            Question question = new Question
                            {
                                Id = int.TryParse(apiQuestion.Id, out int qId) ? qId : 0,
                                Text = apiQuestion.Text,
                                ImageUrl = apiQuestion.ImageUrl,
                                Type = (QuestionType)Enum.Parse(typeof(QuestionType), apiQuestion.Type),
                                Answers = new List<Answer>()
                            };

                            // Convert answers
                            if (apiQuestion.Answers != null)
                            {
                                foreach (var apiAnswer in apiQuestion.Answers)
                                {
                                    question.Answers.Add(new Answer
                                    {
                                        Id = int.TryParse(apiAnswer.Id, out int aId) ? aId : 0,
                                        Text = apiAnswer.Text,
                                        IsCorrect = apiAnswer.IsCorrect
                                    });
                                }
                            }

                            questions.Add(question);
                        }

                        return questions;
                    }
                    else
                    {
                        LogError("Question response was invalid after decryption");
                        throw new Exception("Failed to parse question data from server");
                    }
                }
                else
                {
                    LogError("Encrypted response was invalid or not properly marked as encrypted");
                    throw new Exception("Failed to receive proper encrypted response from server");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error getting questions: {ex.Message}");
                throw new Exception("Failed to load questions from server. Please try again.");
            }
        }

        // Submit test results
        public async Task<bool> SaveTestResult(dynamic resultData)
        {
            try
            {
                LogInfo($"Preparing to save test results");
                string jsonData = JsonSerializer.Serialize(resultData);
                string encryptedData = XorEncryptToBase64(jsonData);

                var encryptedPayload = new
                {
                    encrypted = true,
                    data = encryptedData
                };

                LogInfo($"Sending encrypted test results to server");

                var response = await PostAsync<dynamic, SubmitResultResponse>("/results", encryptedPayload);

                return response != null && response.Success;
            }
            catch (Exception ex)
            {
                LogError($"Error submitting results: {ex.Message}");
                throw new Exception("Failed to save test results. Please try again.");
            }
        }

        private async Task<TResponse> GetAsync<TResponse>(string endpoint) where TResponse : class
        {
            try
            {
                LogInfo($"GET request to {_baseUrl}{endpoint}");

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}{endpoint}");

                if (!string.IsNullOrEmpty(_authToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                    LogInfo($"Added Authorization header: Bearer {_authToken.Substring(0, Math.Min(_authToken.Length, 10))}...");
                }
                else
                {
                    LogError("Token missing for GET request");
                }

                var response = await _httpClient.SendAsync(request);

                LogInfo($"GET response: {(int)response.StatusCode} {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogError($"Error response: {errorContent}");
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException ex)
            {
                LogError($"HTTP request error: {ex.Message}");
                throw new Exception($"Server communication error: {ex.Message}");
            }
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                LogInfo($"POST request to {_baseUrl}{endpoint}");

                var json = JsonSerializer.Serialize(data);

                if (endpoint.Contains("/results"))
                {
                    LogInfo($"Results JSON: {json.Substring(0, Math.Min(json.Length, 500))}...");
                }

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{endpoint}");
                request.Content = content;

                if (!string.IsNullOrEmpty(_authToken) && !endpoint.Contains("/auth/"))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                    LogInfo($"Added Authorization header for POST: Bearer {_authToken.Substring(0, Math.Min(_authToken.Length, 10))}...");
                }

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogInfo($"POST response: {(int)response.StatusCode} {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    LogError($"Error response: {responseContent}");

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Error))
                        {
                            throw new Exception(errorResponse.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error response from server: {responseContent.Substring(0, Math.Min(responseContent.Length, 500))}");
                    }

                    response.EnsureSuccessStatusCode();
                }

                var result = JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (endpoint.Contains("/auth/"))
                {
                    try
                    {
                        var tokenProp = result.GetType().GetProperty("Token");
                        if (tokenProp != null)
                        {
                            var tokenValue = tokenProp.GetValue(result) as string;
                            if (!string.IsNullOrEmpty(tokenValue))
                            {
                                LogInfo($"Found token in response: {tokenValue.Substring(0, Math.Min(tokenValue.Length, 10))}...");
                                SetAuthToken(tokenValue);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed to extract token from response: {ex.Message}");
                    }
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                LogError($"HTTP request error: {ex.Message}");
                throw new Exception($"Server communication error: {ex.Message}");
            }
        }

        #region Logging
        private void LogInfo(string message)
        {
            //try
            //{
            //    string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api_log.txt");
            //    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n";
            //    System.IO.File.AppendAllText(logPath, logEntry);
            //}
            //catch { /* Ignore logging errors */ }
        }

        private void LogError(string message)
        {
            //try
            //{
            //    string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api_log.txt");
            //    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\n";
            //    System.IO.File.AppendAllText(logPath, logEntry);
            //}
            //catch { /* Ignore logging errors */ }
        }
        #endregion
    }

    // API Response classes
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public UserInfo User { get; set; }

        public class UserInfo
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string UniqueKey { get; set; }
            public string Role { get; set; }
        }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class QuestionResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<ApiQuestion> Data { get; set; }

        public class ApiQuestion
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public string ImageUrl { get; set; }
            public string Type { get; set; }
            public List<ApiAnswer> Answers { get; set; }

            public class ApiAnswer
            {
                public string Id { get; set; }
                public string Text { get; set; }
                public bool IsCorrect { get; set; }
            }
        }
    }

    public class SubmitResultResponse
    {
        public bool Success { get; set; }
        public dynamic Data { get; set; }
    }
}