using Microcharts;
using SkiaSharp;
using MySql.Data.MySqlClient;
using Microcharts.Maui;

namespace Chatbot_Admin;

public partial class PerformanceBreakdown : ContentPage
{
    private readonly string connectionString = "Server=localhost;Database=inquiry;User ID=root;Password=;";

    public PerformanceBreakdown()
    {
        InitializeComponent();

        InitializeCharts();
        _ = LoadAllRatingStatistics();
    }

    private void InitializeCharts()
    {
        PieChartToday.Chart = new PieChart { Entries = Array.Empty<ChartEntry>(), BackgroundColor = SKColors.Transparent };
        PieChartWeek.Chart = new PieChart { Entries = Array.Empty<ChartEntry>(), BackgroundColor = SKColors.Transparent };
        PieChartMonth.Chart = new PieChart { Entries = Array.Empty<ChartEntry>(), BackgroundColor = SKColors.Transparent };
    }

    private async Task LoadAllRatingStatistics()
    {
        await LoadRatingStatisticsForPeriod("today", PieChartToday, RatingListViewToday);
        await LoadRatingStatisticsForPeriod("week", PieChartWeek, RatingListViewWeek);
        await LoadRatingStatisticsForPeriod("month", PieChartMonth, RatingListViewMonth);
    }

    private async Task LoadRatingStatisticsForPeriod(string period, ChartView chartView, ListView listView)
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

            string query = period switch
            {
                "today" => "SELECT rating FROM feedback WHERE DATE(timestamp) = CURDATE()",
                "week" => "SELECT rating FROM feedback WHERE YEARWEEK(timestamp, 1) = YEARWEEK(CURDATE(), 1)",
                "month" => "SELECT rating FROM feedback WHERE YEAR(timestamp) = YEAR(CURDATE()) AND MONTH(timestamp) = MONTH(CURDATE())",
                _ => "SELECT rating FROM feedback"
            };


            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

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

            UpdatePieChart(chartView, ratingCounts);
            UpdateRatingListView(listView, ratingCounts);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load {period} rating statistics: {ex.Message}", "OK");
        }
    }

    private void UpdatePieChart(ChartView chartView, Dictionary<int, int> ratingCounts)
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

            chartView.Chart = new PieChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent,
                LabelTextSize = 0,
                LabelMode = LabelMode.None
            };
        }
        else
        {
            chartView.Chart = new PieChart
            {
                Entries = Array.Empty<ChartEntry>(),
                BackgroundColor = SKColors.Transparent
            };
        }
    }

    private void UpdateRatingListView(ListView listView, Dictionary<int, int> ratingCounts)
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

        listView.ItemsSource = list;
    }

    private string GetRatingLabel(int rating) => rating switch
    {
        1 => "Very Dissatisfied (1)",
        2 => "Dissatisfied (2)",
        3 => "Neutral (3)",
        4 => "Satisfied (4)",
        5 => "Very Satisfied (5)",
        _ => "Unknown"
    };

    private SKColor GetRatingColor(int rating) => rating switch
    {
        1 => SKColor.Parse("#FF0000"),
        2 => SKColor.Parse("#FF6347"),
        3 => SKColor.Parse("#FFA500"),
        4 => SKColor.Parse("#90EE90"),
        5 => SKColor.Parse("#008000"),
        _ => SKColor.Parse("#808080")
    };

    public class RatingDisplay
    {
        public string Label { get; set; }
        public string Percentage { get; set; }
        public Color Color { get; set; }
    }
}
