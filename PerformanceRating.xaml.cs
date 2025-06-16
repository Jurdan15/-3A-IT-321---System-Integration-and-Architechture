using Microcharts;
using SkiaSharp;
using MySql.Data.MySqlClient;
using System.Globalization;

namespace Chatbot_Admin
{
    public partial class PerformanceRating : ContentPage
    {
        private readonly string connectionString = "Server=localhost;Database=inquiry;User ID=root;Password=;";

        public PerformanceRating()
        {
            InitializeComponent();
            InitializeChart();
            LoadRatingStatistics();
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
            await LoadRatingStatistics();
        }

        private async void OnBreakdownClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PerformanceBreakdown());
        }

        private async Task LoadRatingStatistics()
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var ratingCounts = new Dictionary<int, int>
                {
                    {1, 0},
                    {2, 0},
                    {3, 0},
                    {4, 0},
                    {5, 0}
                };

                const string getRatingsQuery = "SELECT rating FROM feedback";

                using (var command = new MySqlCommand(getRatingsQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            int rating = reader.GetInt32(0);
                            if (rating >= 1 && rating <= 5)
                            {
                                ratingCounts[rating]++;
                            }
                        }
                    }
                }

                UpdatePieChart(ratingCounts);
                UpdateRatingListView(ratingCounts);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load rating statistics: {ex.Message}", "OK");
            }
        }

        private void UpdatePieChart(Dictionary<int, int> ratingCounts)
        {
            int totalCount = ratingCounts.Sum(x => x.Value);

            if (totalCount > 0)
            {
                var entries = new List<ChartEntry>();

                foreach (var rating in ratingCounts.OrderBy(x => x.Key))
                {
                    if (rating.Value > 0)
                    {
                        entries.Add(new ChartEntry(rating.Value)
                        {
                            Label = "",
                            ValueLabel = "",
                            Color = GetRatingColor(rating.Key),
                            TextColor = SKColors.Black
                        });
                    }
                }

                PieChartView.Chart = new PieChart
                {
                    Entries = entries,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 0,
                    LabelMode = LabelMode.None
                };
            }
        }

        private void UpdateRatingListView(Dictionary<int, int> ratingCounts)
        {
            int totalCount = ratingCounts.Sum(x => x.Value);

            var list = new List<RatingDisplay>();

            foreach (var rating in ratingCounts.OrderBy(x => x.Key))
            {
                if (rating.Value > 0)
                {
                    float percent = (float)rating.Value / totalCount * 100;
                    list.Add(new RatingDisplay
                    {
                        Label = GetRatingLabel(rating.Key),
                        Percentage = $"{percent:F1}%",
                        Color = Color.FromArgb(GetRatingColor(rating.Key).ToString())
                    });
                }
            }

            RatingListView.ItemsSource = list;
        }

        private string GetRatingLabel(int rating)
        {
            return rating switch
            {
                1 => "Very Dissatisfied (1)",
                2 => "Dissatisfied (2)",
                3 => "Neutral (3)",
                4 => "Satisfied (4)",
                5 => "Very Satisfied (5)",
                _ => "Unknown"
            };
        }

        private SKColor GetRatingColor(int rating)
        {
            return rating switch
            {
                1 => SKColor.Parse("#FF0000"),
                2 => SKColor.Parse("#FF6347"),
                3 => SKColor.Parse("#FFA500"),
                4 => SKColor.Parse("#90EE90"),
                5 => SKColor.Parse("#008000"),
                _ => SKColor.Parse("#808080")
            };
        }

        public class RatingDisplay
        {
            public string Label { get; set; }
            public string Percentage { get; set; }
            public Color Color { get; set; }
        }
    }
}
