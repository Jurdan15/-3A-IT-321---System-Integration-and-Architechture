using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using MySql.Data.MySqlClient;
using System;

namespace bot
{
    public partial class FeedbackPage : ContentPage
    {
        private int _selectedRating = 0;
        private string _timelyResponse = "";
        private string _fullyAnswered = "";
        private int _sessionId;

        public FeedbackPage(int sessionId)
        {
            InitializeComponent();
            _sessionId = sessionId;
            ChatEndedLabel.IsVisible = false;
            EndButtonsContainer.IsVisible = false;
        }

        private void OnStarTapped(object sender, EventArgs e)
        {
            if (sender is Microsoft.Maui.Controls.Shapes.Path tappedStar &&
                tappedStar.Parent is Grid container &&

                container.Parent is HorizontalStackLayout starContainer &&
                e is TappedEventArgs tappedArgs &&
                int.TryParse(tappedArgs.Parameter?.ToString(), out int rating))
            {
                _selectedRating = rating;

                for (int i = 0; i < starContainer.Children.Count; i++)
                {
                    if (starContainer.Children[i] is Grid grid &&
                     grid.Children[0] is Microsoft.Maui.Controls.Shapes.Path path)

                    {
                        path.Fill = i < rating
                            ? new SolidColorBrush(Color.FromArgb("#FFD700"))
                            : new SolidColorBrush(Color.FromArgb("#D3D3D3"));
                    }
                }
            }
        }

        private void OnTimelyResponseChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton rb && e.Value)
                _timelyResponse = rb.Value.ToString();
        }

        private void OnFullyAnsweredChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton rb && e.Value)
                _fullyAnswered = rb.Value.ToString();
        }

        private async void OnSubmitFeedback(object sender, EventArgs e)
        {
            var comment = CommentsEditor.Text?.Trim();
            string connectionString = "Server=127.0.0.1;Database=inquiry;User=root;Password=;";

            using var connection = new MySqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                string feedbackQuery = @"INSERT INTO feedback 
(rating, timely_response, fully_answered, comments, timestamp, session_id)
VALUES (@rating, @timely, @answered, @comments, @timestamp, @session_id)";
                using var feedbackCmd = new MySqlCommand(feedbackQuery, connection);
                feedbackCmd.Parameters.AddWithValue("@rating", _selectedRating);
                feedbackCmd.Parameters.AddWithValue("@timely", _timelyResponse);
                feedbackCmd.Parameters.AddWithValue("@answered", _fullyAnswered);
                feedbackCmd.Parameters.AddWithValue("@comments", comment ?? string.Empty);
                feedbackCmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                feedbackCmd.Parameters.AddWithValue("@session_id", _sessionId);
                await feedbackCmd.ExecuteNonQueryAsync();

                string sessionQuery = @"INSERT INTO chatsessions (session_time, session_desc) 
VALUES (@session_time, @session_desc)";
                using var sessionCmd = new MySqlCommand(sessionQuery, connection);
                sessionCmd.Parameters.AddWithValue("@session_time", DateTime.UtcNow);
                sessionCmd.Parameters.AddWithValue("@session_desc", "User has left the chat");
                await sessionCmd.ExecuteNonQueryAsync();

                ChatEndedLabel.Text = $"The visitor has left the chat.\nThe chat ended. ({DateTime.Now:h:mm tt})";
                ChatEndedLabel.IsVisible = true;
                EndButtonsContainer.IsVisible = true;

                await DisplayAlert("Thank you!", $"Your rating: {_selectedRating}\nFeedback has been submitted.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to submit feedback: {ex.Message}", "OK");
            }
        }

        private async void OnRestartChat(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage(_sessionId));
        }

        private void OnExitButtonClicked(object sender, EventArgs e)
        {
#if WINDOWS || MACCATALYST || IOS
            System.Diagnostics.Process.GetCurrentProcess().Kill();
#elif ANDROID
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#else
            System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        }
    }
}
