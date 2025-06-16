namespace Chatbot_Admin
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
        }
        private async void OnLoginChatClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage());
        }


    }

}
