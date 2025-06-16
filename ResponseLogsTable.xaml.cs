using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using MySql.Data.MySqlClient;

namespace Chatbot_Admin
{
    public partial class ResponseLogsTable : ContentPage
    {
        public ResponseLogsTable()
        {
            InitializeComponent();
            LoadResponseLogs();
        }

        private void LoadResponseLogs()
        {
            var responseLogs = new List<ResponseLogEntry>();

            string connectionString = "Server=localhost;Database=inquiry;Uid=root;Pwd=;";

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = @"
SELECT logs.input AS question,
       responses.resp_desc,
       responses.timestamp
FROM responses
INNER JOIN logs
    ON responses.log_id = logs.log_id
WHERE responses.resp_desc IS NOT NULL;";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while(reader.Read())
{
                    DateTime dt = reader.GetDateTime(reader.GetOrdinal("timestamp"));
                    string Timestamp = dt.ToString("MMMM d, yyyy h:mmtt");
                    Timestamp = Timestamp.Substring(0, Timestamp.Length - 2) +
                                Timestamp.Substring(Timestamp.Length - 2).ToLower();

                    var log = new ResponseLogEntry
                    {
                        Question = reader["question"].ToString(),
                        Response = reader["resp_desc"].ToString(),
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

    public class ResponseLogEntry
    {
        public string Question { get; set; }  
        public string Response { get; set; }
        public string Timestamp { get; set; }
    }

}
