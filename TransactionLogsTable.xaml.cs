using Google.Protobuf.WellKnownTypes;
using Microsoft.Maui.Controls;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Chatbot_Admin
{
    public partial class TransactionLogsTable : ContentPage
    {
        private string connectionString = "server=localhost;user=root;password=;database=inquiry;";

        public TransactionLogsTable()
        {
            InitializeComponent();
            LoadTransactionLogs();
        }

        private void OnReloadLogsClicked(object sender, EventArgs e)
        {
            LoadTransactionLogs();
        }

        private void LoadTransactionLogs()
        {
            var logs = new List<TransactionLog>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "SELECT session_id, session_desc, session_time FROM chatsessions ORDER BY session_time DESC";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime dt = reader.GetDateTime("session_time");
                            string Timestamp = dt.ToString("MMMM d, yyyy h:mmtt");
                            Timestamp = Timestamp.Substring(0, Timestamp.Length - 2) + Timestamp[^2..].ToLower();

                            logs.Add(new TransactionLog
                            {
                                ID = reader.GetInt32("session_id"),
                                Description = reader.GetString("session_desc"),
                                Timestamp = Timestamp
                            });

                        }
                    }
                }

                LogsCollectionView.ItemsSource = logs;
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to load logs: {ex.Message}", "OK");
            }
        }

    }

    public class TransactionLog
    {
        public int ID { get; set; }
        public string Description { get; set; }
        public string Timestamp { get; set; }
    }

     
    }
