using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using MySql.Data.MySqlClient;

namespace Chatbot_Admin
{
    public partial class ResponseLogs : ContentPage
    {
        public ResponseLogs()
        {
            InitializeComponent();
            LoadResponseLogs();
        }

        private void LoadResponseLogs()
        {
            var responseLogs = new List<ResponseLog>();

            // Use your actual MySQL connection settings
            string connectionString = "Server=localhost;Database=inquiry;Uid=root;Pwd=;";

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = @"
                    SELECT logs.input AS question, 
                           response_rating.response_description AS response, 
                           response_rating.rating, 
                           response_rating.timestamp,
                           response_rating.session_id
                    FROM logs
                    INNER JOIN response_rating 
                        ON logs.session_id = response_rating.session_id
                    WHERE logs.input IS NOT NULL";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read()) 
                {

                    DateTime dt = reader.GetDateTime(reader.GetOrdinal("timestamp"));
                    string Timestamp = dt.ToString("MMMM d, yyyy h:mmtt");
                    Timestamp = Timestamp.Substring(0, Timestamp.Length - 2) +
                                Timestamp.Substring(Timestamp.Length - 2).ToLower();
                    var log = new ResponseLog
                    {
                        Question = reader["question"].ToString(),
                        Response = reader["response"].ToString(),
                        Rating = Convert.ToInt32(reader["rating"]),
                        Timestamp = Timestamp
                    };
                    responseLogs.Add(log);
                }

                ResponseList.ItemsSource = responseLogs;
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Database connection failed: {ex.Message}", "OK");
            }
        }
    }

    public class ResponseLog
    {
        public string Question { get; set; }
        public string Response { get; set; }
        public int Rating { get; set; }
        public string Timestamp { get; set; }

        public string RatingText => Rating == 1 ? "Like" : "Dislike";
    }
}
