using Microsoft.Maui.Controls;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;

namespace Chatbot_Admin;

public partial class keyword_page : ContentPage
{
    private readonly string connectionString = "server=127.0.0.1;database=inquiry;user=root;password=;";

    public class KeywordItem
    {
        public int Id { get; set; }
        public string Keyword { get; set; }
    }

    public ObservableCollection<KeywordItem> Keywords { get; } = new();

    public keyword_page()
    {
        InitializeComponent();
        KeywordsCollectionView.ItemsSource = Keywords;
        LoadKeywords();
    }

    private async void OnSaveButtonClicked(object sender, EventArgs e)
    {
        var keyword = KeywordEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            await DisplayAlert("Error", "Please enter a keyword", "OK");
            return;
        }

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = "INSERT INTO intents (keyword) VALUES (@keyword)";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@keyword", keyword);

            await command.ExecuteNonQueryAsync();

            StatusLabel.Text = "Keyword saved successfully!";
            StatusLabel.TextColor = Colors.Lime;
            KeywordEntry.Text = "";
            await LoadKeywords();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
    }

    private async Task LoadKeywords()
    {
        try
        {
            Keywords.Clear();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = "SELECT id, keyword FROM intents ORDER BY keyword";
            using var command = new MySqlCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Keywords.Add(new KeywordItem
                {
                    Id = reader.GetInt32(0),
                    Keyword = reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error loading keywords: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
    }

    private async void OnDeleteKeywordClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var id = (int)button.CommandParameter;

        bool confirm = await DisplayAlert("Confirm", "Are you sure you want to delete this keyword?", "Yes", "No");
        if (!confirm) return;

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = "DELETE FROM intents WHERE id = @id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();

            StatusLabel.Text = "Keyword deleted successfully!";
            StatusLabel.TextColor = Colors.Lime;
            await LoadKeywords();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error deleting keyword: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
    }
}