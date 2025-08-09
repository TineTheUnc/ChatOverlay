using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcYoutube;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;
using Brushes = System.Windows.Media.Brushes;
using LiveChatMessage = GrpcYoutube.LiveChatMessage;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace ChatOverlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		private bool start = false;
		List<LiveChatMessage> data = [];
		private V3DataLiveChatMessageService.V3DataLiveChatMessageServiceClient Connection;
		private YouTubeService Service;
		private ChannelBase grpcChannel;
		private LiveBroadcast live;
		public HashSet<string> chat = [];
		public string PageToken;
		public string client_secretPath;
		
		private UpdateManager _um;
		private UpdateInfo _update;
		public MainWindow()
        {
            InitializeComponent();
			var source = new GithubSource(
					repoUrl: "https://github.com/TineTheUnc/ChatOverlay", 
					accessToken: null,
					false
				);
			_um = new UpdateManager(source);

			TextLog.Text = App.Log.ToString();
			App.Log.LogUpdated += LogUpdated;
			UpdateStatus();
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var overlay = new Chat(data);
			overlay.Show();
			if (File.Exists(Path.Combine(App.myAppFolder, "client_secret.json")))
			{
				ImportButton.Background = new SolidColorBrush(Colors.Gray);
				ImportButton.IsEnabled = false;
				client_secretPath = Path.Combine(App.myAppFolder, "client_secret.json");
			}
			else {
				ImportButton.Background = new SolidColorBrush(Colors.ForestGreen);
				ImportButton.IsEnabled = true;
			}
		}

		private async void BtnCheckUpdateClick(object sender, RoutedEventArgs e)
		{
			Working();
			try
			{
				// ConfigureAwait(true) so that UpdateStatus() is called on the UI thread
				_update = await _um.CheckForUpdatesAsync().ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				App.Log.LogError(ex, "Error checking for updates");
			}
			UpdateStatus();
		}

		private async void BtnDownloadUpdateClick(object sender, RoutedEventArgs e)
		{
			Working();
			try
			{
				// ConfigureAwait(true) so that UpdateStatus() is called on the UI thread
				await _um.DownloadUpdatesAsync(_update, Progress).ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				App.Log.LogError(ex, "Error downloading updates");
			}
			UpdateStatus();
		}

		private void BtnRestartApplyClick(object sender, RoutedEventArgs e)
		{
			_um.ApplyUpdatesAndRestart(_update);
		}

		private void LogUpdated(object sender, LogUpdatedEventArgs e)
		{
			// logs can be sent from other threads
			this.Dispatcher.InvokeAsync(() => {
				TextLog.Text = e.Text;
				ScrollLog.ScrollToEnd();
			});
		}

		private void Progress(int percent)
		{
			// progress can be sent from other threads
			this.Dispatcher.InvokeAsync(() => {
				TextStatus.Text = $"Downloading ({percent}%)...";
			});
		}

		private void Working()
		{
			App.Log.LogInformation("");
			BtnCheckUpdate.IsEnabled = false;
			BtnDownloadUpdate.IsEnabled = false;
			BtnRestartApply.IsEnabled = false;
			TextStatus.Text = "Working...";
		}

		private void UpdateStatus()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"Velopack version: {VelopackRuntimeInfo.VelopackNugetVersion}");
			sb.AppendLine($"This app version: {(_um.IsInstalled ? _um.CurrentVersion : "(n/a - not installed)")}");

			if (_update != null)
			{
				sb.AppendLine($"Update available: {_update.TargetFullRelease.Version}");
				BtnDownloadUpdate.IsEnabled = true;
			}
			else
			{
				BtnDownloadUpdate.IsEnabled = false;
			}

			if (_um.UpdatePendingRestart != null)
			{
				sb.AppendLine("Update ready, pending restart to install");
				BtnRestartApply.IsEnabled = true;
			}
			else
			{
				BtnRestartApply.IsEnabled = false;
			}

			TextStatus.Text = sb.ToString();
			BtnCheckUpdate.IsEnabled = true;
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
				File.Copy(dialog.FileName, Path.Combine(App.myAppFolder, "client_secret.json"), true);
				client_secretPath = Path.Combine(App.myAppFolder, "client_secret.json");
				ImportButton.Background = new SolidColorBrush(Colors.Gray);
				ImportButton.IsEnabled = false;
			}
		}

		private void Authorization(object sender, RoutedEventArgs e)
		{
			if (Service != null)
			{
				ClearAuthorizeAPI();
			}
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
			grpcChannel = GrpcChannel.ForAddress("dns:///youtube.googleapis.com:443", new GrpcChannelOptions
			{
				Credentials = ChannelCredentials.Create(new SslCredentials(), CallCredentials.FromInterceptor((context, metadata) =>
				{
					metadata.Add("Authorization", "Bearer " + credential.Token.AccessToken);
					metadata.Add("x-goog-api-client", "gl-dotnet/9.0 ChatOverlay/2.0");
					metadata.Add("User-Agent", "ChatOverlay/2.0");
					return Task.CompletedTask;
				}))
			});

			Connection = new V3DataLiveChatMessageService.V3DataLiveChatMessageServiceClient(grpcChannel);
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

		public async void Stop() {
			start = false;
			Connection = null;
			await grpcChannel?.ShutdownAsync();
			live = null;
			data = [];
			chat = [];
			PageToken = null;
		}


		private async void RunLoopAsync()
		{
			while (start)
			{
				var request = new LiveChatMessageListRequest
				{
					LiveChatId = live.Snippet.LiveChatId
				};
				request.Part.Add("snippet");
				request.Part.Add("authorDetails");
				request.MaxResults = 50;
				if (PageToken != null)
				{
					request.PageToken = PageToken;
				}
				using var call = Connection.StreamList(request);
				List<LiveChatMessage> data = [];
				await foreach (var response in call.ResponseStream.ReadAllAsync())
				{
					PageToken = response.NextPageToken;
					foreach (var message in response.Items)
					{
						if (!chat.Contains(message.Id))
						{
							chat.Add(message.Id);
							data.Add(message);
						}
					}

					if (!string.IsNullOrEmpty(response.OfflineAt))
					{
						Stop();
						break;
					}
					else {
					}
				}
				if (data.Count > 0) {
					var chatOverlay = new Chat(data);
					chatOverlay.Show();
				}
				await Task.Delay(10000);
			}
		}
	}
}