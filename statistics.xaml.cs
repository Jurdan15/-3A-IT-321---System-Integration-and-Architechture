using Microcharts;
using SkiaSharp;
using MySql.Data.MySqlClient;
using System.Globalization;

namespace Chatbot_Admin
{
    public class KeywordDisplayItem
    {
        public string Key { get; set; }
        public string Value { get; set; } // now a percentage string like "12.5%"
        public string ColorHex { get; set; }
    }

    public partial class statistics : ContentPage
    {
        private readonly string connectionString = "Server=localhost;Database=inquiry;User ID=root;Password=;";
        private const double SIMILARITY_THRESHOLD = 0.7;

        private Dictionary<string, int> keywordStats;
        private int othersCount;
        private List<KeyValuePair<string, int>> sortedKeywordCounts;
        private List<string> colorMap = new();

        public statistics()
        {
            InitializeComponent();
            InitializeChart();
            LoadKeywordStatistics();
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
            await LoadKeywordStatistics();
        }

        private async void OnBreakdownClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new InquiryCategoryBreakdown());
        }

        private async Task LoadKeywordStatistics()
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var keywords = new List<string>();
                const string getKeywordsQuery = "SELECT DISTINCT keyword FROM intents";

                using (var command = new MySqlCommand(getKeywordsQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        keywords.Add(reader.GetString(0).ToLower());
                    }
                }

                if (keywords.Count == 0)
                {
                    await DisplayAlert("Info", "No keywords found.", "OK");
                    return;
                }

                var intents = new List<string>();
                const string getIntentsQuery = "SELECT input FROM logs";

                using (var command = new MySqlCommand(getIntentsQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        intents.Add(reader.GetString(0).ToLower());
                    }
                }

                keywordStats = new Dictionary<string, int>();
                othersCount = 0;

                foreach (var keyword in keywords)
                {
                    keywordStats[keyword] = 0;
                }

                foreach (var intent in intents)
                {
                    bool matched = false;

                    foreach (var keyword in keywords)
                    {
                        if (intent.Contains(keyword))
                        {
                            keywordStats[keyword]++;
                            matched = true;
                            break;
                        }

                        foreach (var word in intent.Split(' '))
                        {
                            if (word.Length < 3) continue;

                            double similarity = CalculateSimilarity(keyword, word);
                            if (similarity >= SIMILARITY_THRESHOLD)
                            {
                                keywordStats[keyword]++;
                                matched = true;
                                break;
                            }
                        }

                        if (matched) break;
                    }

                    if (!matched)
                    {
                        othersCount++;
                    }
                }

                UpdatePieChart();
                UpdateKeywordListView();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load statistics: {ex.Message}", "OK");
            }
        }

        private void UpdatePieChart()
        {
            int totalCount = keywordStats.Sum(x => x.Value) + othersCount;

            if (totalCount > 0)
            {
                var entries = new List<ChartEntry>();
                var random = new Random();
                colorMap.Clear();

                foreach (var kvp in keywordStats.OrderByDescending(k => k.Value))
                {
                    if (kvp.Value > 0)
                    {
                        string color = GetRandomColor();
                        colorMap.Add(color);

                        entries.Add(new ChartEntry(kvp.Value)
                        {
                            Label = "",
                            ValueLabel = "",
                            Color = SKColor.Parse(color),
                            TextColor = SKColors.Black
                        });
                    }
                }

                if (othersCount > 0)
                {
                    string otherColor = "#CCCCCC";
                    colorMap.Add(otherColor);

                    entries.Add(new ChartEntry(othersCount)
                    {
                        Label = "",
                        ValueLabel = "",
                        Color = SKColor.Parse(otherColor),
                        TextColor = SKColors.Black
                    });
                }

                PieChartView.Chart = new PieChart
                {
                    Entries = entries,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 0,
                    LabelMode = LabelMode.None
                };
            }
            else
            {
                PieChartView.Chart = new PieChart
                {
                    Entries = Array.Empty<ChartEntry>(),
                    BackgroundColor = SKColors.Transparent
                };
            }
        }

        private void UpdateKeywordListView()
        {
            var combinedDict = new Dictionary<string, int>(keywordStats);
            if (othersCount > 0)
                combinedDict["Others"] = othersCount;

            int total = combinedDict.Sum(kvp => kvp.Value);

            sortedKeywordCounts = combinedDict
                .OrderByDescending(kvp => kvp.Value)
                .ToList();

            var displayList = new List<KeywordDisplayItem>();
            int index = 0;

            foreach (var kvp in sortedKeywordCounts)
            {
                string displayKey = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(kvp.Key);
                double percentage = total > 0 ? ((double)kvp.Value / total) * 100 : 0;
                string displayPercent = $"{percentage:F1}%";

                string colorHex = index < colorMap.Count ? colorMap[index] : "#FFFFFF";
                index++;

                displayList.Add(new KeywordDisplayItem
                {
                    Key = displayKey,
                    Value = displayPercent,
                    ColorHex = colorHex
                });
            }

            KeywordListView.ItemsSource = displayList;
        }

        private string GetRandomColor()
        {
            var random = new Random();
            return $"#{random.Next(0x1000000):X6}";
        }

        private double CalculateSimilarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0;

            if (a.Contains(b) || b.Contains(a))
                return 1.0;

            int maxLen = Math.Max(a.Length, b.Length);
            int distance = LevenshteinDistance(a, b);
            return 1.0 - (double)distance / maxLen;
        }

        private int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
