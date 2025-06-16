using Microcharts;
using SkiaSharp;
using MySql.Data.MySqlClient;
using Microcharts.Maui;

namespace Chatbot_Admin
{
    public partial class TimelyResponseBreakdown : ContentPage
    {
        private readonly string connectionString = "Server=localhost;Database=inquiry;User ID=root;Password=;";

        public TimelyResponseBreakdown()
        {
            InitializeComponent();
            InitializeCharts();
            LoadAllResponseStatistics();
        }

        private void InitializeCharts()
        {
            TodayChart.Chart = new PieChart { Entries = Array.Empty<ChartEntry>(), BackgroundColor = SKColors.Transparent };
            WeekChart.Chart = new PieChart { Entries = Array.Empty<ChartEntry>(), BackgroundColor = SKColors.Transparent };
            MonthChart.Chart = new PieChart { Entries = Array.Empty<ChartEntry>(), BackgroundColor = SKColors.Transparent };
        }

        private async void OnReloadClicked(object sender, EventArgs e)
        {
            await LoadAllResponseStatistics();
        }

        private async Task LoadAllResponseStatistics()
        {
            await LoadResponseStatistics(TodayChart, DateTime.Today, DateTime.Today.AddDays(1));
            await LoadResponseStatistics(WeekChart, DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today.AddDays(1));
            await LoadResponseStatistics(MonthChart, new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today.AddDays(1));
        }

        private async Task LoadResponseStatistics(ChartView chartView, DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var responseCounts = new Dictionary<string, int>
                {
                    { "Yes", 0 },
                    { "Somewhat", 0 },
                    { "No", 0 }
                };

                string query = @"SELECT timely_response FROM feedback 
                                 WHERE timestamp >= @startDate AND timestamp < @endDate AND timely_response IS NOT NULL";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string response = reader.GetString(0).Trim();
                    string normalizedResponse = NormalizeResponse(response);

                    if (responseCounts.ContainsKey(normalizedResponse))
                    {
                        responseCounts[normalizedResponse]++;
                    }
                }

                UpdatePieChart(chartView, responseCounts);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load response statistics: {ex.Message}", "OK");
            }
        }

        private string NormalizeResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "No";

            response = response.Trim().ToLower();

            if (response.Contains("yes") || response == "y")
                return "Yes";
            if (response.Contains("somewhat") || response.Contains("partial") || response == "s")
                return "Somewhat";

            return "No";
        }

        private void UpdatePieChart(ChartView chartView, Dictionary<string, int> responseCounts)
        {
            int totalCount = responseCounts.Sum(x => x.Value);

            if (totalCount > 0)
            {
                var entries = new List<ChartEntry>();
                var order = new[] { "Yes", "Somewhat", "No" };

                foreach (var response in order)
                {
                    if (responseCounts[response] > 0)
                    {
                        float percentage = (float)responseCounts[response] / totalCount * 100;
                        entries.Add(new ChartEntry(responseCounts[response])
                        {
                            Label = response,
                            ValueLabel = $"{percentage:F1}%",
                            Color = GetResponseColor(response),
                            TextColor = SKColors.Black
                        });
                    }
                }

                chartView.Chart = new PieChart
                {
                    Entries = entries,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 30,
                    LabelMode = LabelMode.RightOnly,
                    LabelColor = SKColors.Black
                };
            }
            else
            {
                chartView.Chart = new PieChart
                {
                    Entries = new List<ChartEntry>(),
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 30
                };
            }
        }

        private SKColor GetResponseColor(string response) => response switch
        {
            "Yes" => SKColor.Parse("#4CAF50"),
            "Somewhat" => SKColor.Parse("#FFC107"),
            "No" => SKColor.Parse("#F44336"),
            _ => SKColor.Parse("#808080")
        };
    }
}