using Microsoft.Maui.Controls;
using System;
using MySql.Data.MySqlClient;
using Microcharts;
using SkiaSharp;
using System.Collections.Generic;

namespace Chatbot_Admin
{
    public partial class TransactionLogs : ContentPage
    {
        private string connectionString = "server=localhost;user=root;password=;database=inquiry;";

        public TransactionLogs()
        {
            InitializeComponent();
            LoadBarChart();
        }

        private void OnReloadStatisticsClicked(object sender, EventArgs e)
        {
            LoadBarChart();
        }

        private void LoadBarChart()
        {
            int todayCount = 0, weekCount = 0, monthCount = 0;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT session_time 
                        FROM chatsessions 
                        WHERE session_desc = 'User has started the chat'";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime sessionTime = Convert.ToDateTime(reader["session_time"]);

                            if (sessionTime.Date == DateTime.Today)
                                todayCount++;

                            if (sessionTime >= DateTime.Today.AddDays(-6))
                                weekCount++;

                            if (sessionTime.Month == DateTime.Today.Month && sessionTime.Year == DateTime.Today.Year)
                                monthCount++;
                        }
                    }
                }

                var entries = new List<ChartEntry>
{
    new ChartEntry(todayCount)
    {
        Label = "Today",
        ValueLabel = todayCount.ToString(),
        Color = SKColor.Parse("#3498db"),
        ValueLabelColor = SKColors.Black
    },
    new ChartEntry(weekCount)
    {
        Label = "This Week",
        ValueLabel = weekCount.ToString(),
        Color = SKColor.Parse("#2ecc71"),
        ValueLabelColor = SKColors.Black
    },
    new ChartEntry(monthCount)
    {
        Label = "This Month",
        ValueLabel = monthCount.ToString(),
        Color = SKColor.Parse("#e67e22"),
        ValueLabelColor = SKColors.Black
    }
};

                BarChartView.Chart = new BarChart
{
    Entries = entries,
    LabelTextSize = 20, // Font size for labels
    ValueLabelTextSize = 30, // Font size for values
    LabelOrientation = Orientation.Horizontal,
    ValueLabelOrientation = Orientation.Vertical,
    Margin = 40, // Increased margin
    BackgroundColor = SKColors.White
};


            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Could not load data: {ex.Message}", "OK");
            }
        }

        private async void OnTableClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TransactionLogsTable());
        }
    }
}
