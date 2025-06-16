using Microcharts;
using SkiaSharp;
using MySql.Data.MySqlClient;
using System.Globalization;

namespace Chatbot_Admin
{
    public partial class Timely_Response : ContentPage
    {
        private readonly string connectionString = "Server=localhost;Database=inquiry;User ID=root;Password=;";

        public Timely_Response()
        {
            InitializeComponent();
            InitializeChart();
            LoadResponseStatistics();
        }

        private void InitializeChart()
        {
            PieChartView.Chart = new PieChart
            {
                Entries = Array.Empty<ChartEntry>(),
                BackgroundColor = SKColors.Transparent
            };
        }

        private async void OnReloadStatisticsClicked(object sender, EventArgs e)
        {
            await LoadResponseStatistics();
        }

        private async void OnBreakdownClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TimelyResponseBreakdown());
        }

        private async Task LoadResponseStatistics()
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Initialize response counts
                var responseCounts = new Dictionary<string, int>
                {
                    {"No", 0},
                    {"Yes", 0},
                    {"Somewhat", 0}
                };

                const string getResponsesQuery = "SELECT timely_response FROM feedback WHERE timely_response IS NOT NULL";

                using (var command = new MySqlCommand(getResponsesQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string response = reader.GetString(0).Trim();
                        string normalizedResponse = NormalizeResponse(response);

                        if (responseCounts.ContainsKey(normalizedResponse))
                        {
                            responseCounts[normalizedResponse]++;
                        }
                    }
                }

                UpdatePieChart(responseCounts);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load response statistics: {ex.Message}", "OK");
            }
        }

        private string NormalizeResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "No"; // Default if empty

            response = response.Trim().ToLower();

            if (response.Contains("yes") || response == "y")
                return "Yes";
            if (response.Contains("somewhat") || response.Contains("partial") || response == "s")
                return "Somewhat";

            return "No"; // Default case
        }

        private void UpdatePieChart(Dictionary<string, int> responseCounts)
        {
            int totalCount = responseCounts.Sum(x => x.Value);

            if (totalCount > 0)
            {
                var entries = new List<ChartEntry>();
                LegendPanel.Children.Clear();

                var orderedResponses = new List<string> { "Yes", "Somewhat", "No" };

                foreach (var response in orderedResponses)
                {
                    if (responseCounts[response] > 0)
                    {
                        float percentage = (float)responseCounts[response] / totalCount * 100;

                        var color = GetResponseColor(response);
                        var labelText = $"{response}: {percentage:F1}%";

                        // Add to chart entries WITHOUT labels inside chart
                        entries.Add(new ChartEntry(responseCounts[response])
                        {
                            Label = response,
                            ValueLabel = "",  // Empty so it does not show on chart slices
                            Color = color,
                            TextColor = SKColors.Black
                        });

                        // Create legend item: color box first, then label
                        var legendItem = new HorizontalStackLayout { Spacing = 10 };

                        var colorBox = new BoxView
                        {
                            WidthRequest = 20,
                            HeightRequest = 20,
                            Color = Color.FromArgb(color.ToString()),
                            CornerRadius = 4,
                            VerticalOptions = LayoutOptions.Center
                        };

                        var label = new Label
                        {
                            Text = labelText,
                            FontSize = 16,
                            VerticalOptions = LayoutOptions.Center,
                            TextColor = Colors.Black
                        };

                        legendItem.Add(colorBox);
                        legendItem.Add(label);

                        LegendPanel.Add(legendItem);
                    }
                }

                PieChartView.Chart = new PieChart
                {
                    Entries = entries,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 0,
                    LabelMode = LabelMode.None,
                    LabelColor = SKColors.Transparent
                };
            }
        }

        private SKColor GetResponseColor(string response)
        {
            // Static colors for each response type
            return response switch
            {
                "Yes" => SKColor.Parse("#4CAF50"), // Green
                "Somewhat" => SKColor.Parse("#FFC107"), // Amber
                "No" => SKColor.Parse("#F44336"), // Red
                _ => SKColor.Parse("#808080") // Gray (default)
            };
        }
    }
}