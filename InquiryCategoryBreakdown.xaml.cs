using Microcharts;
using SkiaSharp;
using MySql.Data.MySqlClient;
using System.Globalization;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;


namespace Chatbot_Admin
{
    public partial class InquiryCategoryBreakdown : ContentPage
    {
        private readonly string connectionString = "Server=localhost;Database=inquiry;User ID=root;Password=;";
        private const double SIMILARITY_THRESHOLD = 0.7;

        public InquiryCategoryBreakdown()
        {
            InitializeComponent();
            LoadBreakdown();
        }

        private async void LoadBreakdown()
        {
            await LoadStatisticsForPeriod("today", TodayPieChart, TodayKeywordListView);
            await LoadStatisticsForPeriod("week", WeekPieChart, WeekKeywordListView);
            await LoadStatisticsForPeriod("month", MonthPieChart, MonthKeywordListView);
        }

        private async Task LoadStatisticsForPeriod(string period, Microcharts.Maui.ChartView chartView, ListView listView)

        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // 1. Get keywords
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

                if (keywords.Count == 0) return;

                // 2. Get logs for period
                var logs = new List<string>();
                string dateCondition = period switch
                {
                    "today" => "DATE(timestamp) = CURDATE()",
                    "week" => "YEARWEEK(timestamp, 1) = YEARWEEK(CURDATE(), 1)",
                    "month" => "MONTH(timestamp) = MONTH(CURDATE()) AND YEAR(timestamp) = YEAR(CURDATE())",
                    _ => ""
                };

                if (string.IsNullOrEmpty(dateCondition)) return;

                string getLogsQuery = $"SELECT input FROM logs WHERE {dateCondition}";

                using (var command = new MySqlCommand(getLogsQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        logs.Add(reader.GetString(0).ToLower());
                    }
                }

                var keywordStats = new Dictionary<string, int>();
                var colorMap = new List<string>();
                int othersCount = 0;

                foreach (var keyword in keywords)
                    keywordStats[keyword] = 0;

                foreach (var intent in logs)
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
                        othersCount++;
                }

                // 3. Update Pie Chart
                int totalCount = keywordStats.Sum(x => x.Value) + othersCount;
                var entries = new List<ChartEntry>();
                var random = new Random();

                foreach (var kvp in keywordStats.OrderByDescending(k => k.Value))
                {
                    if (kvp.Value > 0)
                    {
                        string color = GetRandomColor(random);
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

                chartView.Chart = new PieChart
                {
                    Entries = entries,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 0,
                    LabelMode = LabelMode.None
                };

                // 4. Update List View
                var combinedDict = new Dictionary<string, int>(keywordStats);
                if (othersCount > 0)
                    combinedDict["Others"] = othersCount;

                int total = combinedDict.Sum(kvp => kvp.Value);
                var sortedKeywordCounts = combinedDict.OrderByDescending(kvp => kvp.Value).ToList();
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

                listView.ItemsSource = displayList;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load {period} statistics: {ex.Message}", "OK");
            }
        }

        private string GetRandomColor(Random random)
        {
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
        private async void OnPrintToPdfClicked(object sender, EventArgs e)
        {
            try
            {
                var charts = new[]
                {
            new { Chart = TodayPieChart.Chart, Title = "Today's Inquiry Distribution" },
            new { Chart = WeekPieChart.Chart, Title = "This Week's Inquiry Distribution" },
            new { Chart = MonthPieChart.Chart, Title = "This Month's Inquiry Distribution" }
        };

                var pdf = new PdfDocument();

                foreach (var item in charts)
                {
                    if (item.Chart == null) continue;

                    // Render the Microcharts chart to SKImage using SkiaSharp
                    using var surface = SKSurface.Create(new SKImageInfo(600, 600));
                    item.Chart.DrawContent(surface.Canvas, 600, 600);
                    surface.Canvas.Flush();
                    using var image = surface.Snapshot();
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    using var ms = new MemoryStream();
                    data.SaveTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    // Create a new PDF page
                    var page = pdf.AddPage();
                    page.Width = XUnit.FromMillimeter(210);
                    page.Height = XUnit.FromMillimeter(297);

                    using var gfx = XGraphics.FromPdfPage(page);
                    var img = XImage.FromStream(() => ms);

                    gfx.DrawString(item.Title, new XFont("Arial", 16, XFontStyle.Bold), XBrushes.Black,
                        new XRect(0, 30, page.Width, 30), XStringFormats.TopCenter);

                    gfx.DrawImage(img, 80, 70, 400, 400);
                }

                var pdfPath = Path.Combine(FileSystem.AppDataDirectory, $"InquiryBreakdown_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                using var fileStream = File.Create(pdfPath);
                pdf.Save(fileStream);

                await DisplayAlert("Success", $"PDF saved at:\n{pdfPath}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to print to PDF:\n{ex.Message}", "OK");
            }
        }

    }
}
