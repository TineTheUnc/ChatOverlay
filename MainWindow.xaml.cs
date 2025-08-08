using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ChatOverlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		private bool start = false;
		List<LiveChatMessage> data = [];
		private YouTubeService Service;
		private LiveBroadcast live;
		public HashSet<string> chat = [];
		public string PageToken;
		public string client_secretPath;
		readonly string myAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatOverlat");

		public MainWindow()
        {
            InitializeComponent();
			Directory.CreateDirectory(myAppFolder);
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var overlay = new Chat(data);
			overlay.Show();
			if (File.Exists(Path.Combine(myAppFolder, "client_secret.json")))
			{
				ImportButton.Background = new SolidColorBrush(Colors.Gray);
				ImportButton.IsEnabled = false;
				client_secretPath = Path.Combine(myAppFolder, "client_secret.json");
			}
			else {
				ImportButton.Background = new SolidColorBrush(Colors.ForestGreen);
				ImportButton.IsEnabled = true;
			}
		}

		private void Import_File(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Title = "Save client_secret File",
				Filter = "JSON files (*.json)|*.json",
				Multiselect = false
			};
			if (dialog.ShowDialog() == true)
			{
				// เขียนข้อมูลลงไฟล์
				File.Copy(dialog.FileName, Path.Combine(myAppFolder, "client_secret.json"), true);
				client_secretPath = Path.Combine(myAppFolder, "client_secret.json");
				ImportButton.Background = new SolidColorBrush(Colors.Gray);
				ImportButton.IsEnabled = false;
			}
		}

		private void Authorization(object sender, RoutedEventArgs e)
		{
			ClearAuthorizeAPI();
			if ((string)AuthorizationButton.Content == "Authorization")
			{
				if (File.Exists(this.client_secretPath))
				{
					Stream steam = File.Open(this.client_secretPath, FileMode.Open, FileAccess.Read);
					AuthorizationAPI(steam);
					AuthorizationButton.Background = new SolidColorBrush(Colors.Gray);
				}
				else
				{
					MessageBox.Show("Need file client_secret.json in " + this.client_secretPath + " folder.");
				}
			}
		}

		public static async void ClearAuthorizeAPI()
		{
			await new FileDataStore("YouTubeLiveChat.Chat").ClearAsync();
		}


		public async void AuthorizationAPI(Stream client_secrets)
		{
			UserCredential credential;
			credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(client_secrets).Secrets,
						[YouTubeService.Scope.YoutubeReadonly],
						"user", CancellationToken.None, new FileDataStore("YouTubeLiveChat.Chat")
						);
			Service = new(new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = "YouTubeLiveChat"
			});
		}



		private void Start(object sender, RoutedEventArgs e) 
		{
			if ((string)StartButton.Content == "Start")
			{
				if (InputBox.Text != string.Empty)
				{
					StartAPI(InputBox.Text);
				}
				else
				{
					MessageBox.Show("Need live id");
				}
			}
			else {
				Stop();
				StartButton.Content = "Start";
				StartButton.Background = new SolidColorBrush(Colors.ForestGreen);
			}
		}

		public async void StartAPI(string LiveId)
		{

			if (Service != null)
			{

				var Live = Service.LiveBroadcasts.List("snippet");
				Live.Id = LiveId;
				var Liveresponse = await Live.ExecuteAsync();
				live = Liveresponse.Items.FirstOrDefault();
				if (live == null)
				{
					MessageBox.Show("Can not find live stream with LiveId : " + LiveId + "\nYou can get only live stream from your account.\n" + "You can reauthorize if you not connect your account.");
				}
				else {
					start = true;
					StartButton.Content = "Stop";
					StartButton.Background = new SolidColorBrush(Colors.Red);
					RunLoopAsync();
				}
			}
			else
			{
				MessageBox.Show("Need authorize.\nYou can see it in controls menu.");
			}

			
		}

		public void Stop() {
			start = false;
			live = null;
			data = [];
			chat = [];
			PageToken = null;
		}


		private async void RunLoopAsync()
		{
			while (start)
			{
				var Chat = Service.LiveChatMessages.List(live.Snippet.LiveChatId, "snippet,authorDetails");
				Chat.MaxResults = 50;
				if (PageToken != null)
				{
					Chat.PageToken = PageToken;
				}
				var Chatresponse = await Chat.ExecuteAsync();
				List<LiveChatMessage> data = [];
				PageToken = Chatresponse.NextPageToken;
				foreach (var item in Chatresponse.Items)
				{
					if (!chat.Contains(item.ETag))
					{
						data.Add(item);

					}
					chat.Add(item.ETag);
				}
				var chatOverlay = new Chat(data);
				chatOverlay.Show();
				await Task.Delay(10000); 
			}
		}
	}
}