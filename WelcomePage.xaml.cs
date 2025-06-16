using Microsoft.Maui.Controls;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;

namespace bot
{
    public partial class WelcomePage : ContentPage
    {
        public WelcomePage()
        {
            InitializeComponent();  // This connects to the XAML
        }
        private async void OnStartChatClicked(object sender, EventArgs e)
        {
            int sessionId = 0;
            string connectionString = "Server=127.0.0.1;Database=inquiry;User=root;Password=;";
            using var connection = new MySqlConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                var query = @"INSERT INTO chatsessions (session_time, session_desc) 
                          VALUES (@session_time, @session_desc);
                          SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@session_time", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@session_desc", "User has started the chat");

                object result = await cmd.ExecuteScalarAsync();
                sessionId = Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Database Error", $"Could not log session: {ex.Message}", "OK");
                return;
            }

            // Pass sessionId to MainPage
            await Navigation.PushAsync(new MainPage(sessionId));
        }
       
     }
}