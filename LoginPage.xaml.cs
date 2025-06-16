using MySql.Data.MySqlClient;

namespace Chatbot_Admin;

public partial class LoginPage : ContentPage
{
    private const string ConnectionString = "server=127.0.0.1;database=inquiry;user=root;password=;";

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a password.", "OK");
            return;
        }

        if (await IsPasswordCorrect(PasswordEntry.Text))
        {
            Preferences.Set("IsAdminLoggedIn", true);
            await LogAction("Admin has logged in");
            await Navigation.PushAsync(new Admin_Dashboard());
        }
        else
        {
            await DisplayAlert("Login Failed", "Incorrect password.", "OK");
        }
    }

    private async Task<bool> IsPasswordCorrect(string inputPassword)
    {
        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("SELECT password FROM users WHERE username = 'Admin'", conn);
            var dbPassword = (string?)await cmd.ExecuteScalarAsync();

            return dbPassword == inputPassword;
        }
        catch
        {
            await DisplayAlert("Error", "Database connection failed.", "OK");
            return false;
        }
    }

    private async Task LogAction(string description)
    {
        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("INSERT INTO userlogs (log_desc, timestamp) VALUES (@desc, NOW())", conn);
            cmd.Parameters.AddWithValue("@desc", description);
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {

        }
    }
}