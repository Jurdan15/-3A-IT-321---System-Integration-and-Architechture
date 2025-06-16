using MySql.Data.MySqlClient;
namespace Chatbot_Admin
{
    public partial class Admin_Dashboard : ContentPage
    {
        private const string ConnectionString = "server=127.0.0.1;database=inquiry;user=root;password=;";



        public Admin_Dashboard()
        {
            InitializeComponent();
            bool isLoggedIn = Preferences.Get("IsAdminLoggedIn", false);

            if (!isLoggedIn)
            {
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }

        }
        private async void OnstatisticsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new statistics());
        }
        private async void OnPerformanceRatingClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PerformanceRating());
        }
        private async void OntimelyresponseClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Timely_Response());
        }
        private async void OnfullyansweredClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Fully_Answered());
        }

        private async void Onkeywords(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new keyword_page());
        }

        private async void OnTransactionLogs(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TransactionLogs());
        }

        private async void OnResponseLogs(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ResponseLogs());
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await LogAction("Admin has logged out");
            Preferences.Set("IsAdminLoggedIn", false);

            await Navigation.PushAsync(new LoginPage());
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
                // Handle logging errors (optional)
            }
        }
        private async void OnViewRawLogs(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ResponseLogsTable()); // Assuming this is the page you meant
        }



    }

}
