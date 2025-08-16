using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Velopack;
using Velopack.Locators;
using MessageBox = System.Windows.MessageBox;

namespace ChatOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
	{
		public AutoExpireImageCache ImageCache { get; }
		public EmojiLoader EmojiLoader { get; } = new();

		public static MemoryLogger Log { get; private set; } = new();

		public static string myAppFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		public static string AssetsFolder = Path.Combine(myAppFolder, "Assets");
		public App()
		{
			ImageCache = new AutoExpireImageCache(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5));
			EmojiLoader = new();
		}

		[STAThread]
		private static void Main(string[] args)
		{
			try
			{
				VelopackApp.Build()
					.OnFirstRun((v) => {
						if (VelopackLocator.IsCurrentSet) {
							myAppFolder = VelopackLocator.Current.AppContentDir;
						}
						Directory.CreateDirectory(AssetsFolder);
					})
					.SetLogger(Log)
					.Run();

				var app = new App();
				app.InitializeComponent();
				app.Run();

			}
			catch (Exception ex)
			{
				MessageBox.Show("Unhandled exception: " + ex.ToString());
			}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
		}
	}

	public class EmojiLoader
	{
		public static string EmojiFolder = Path.Combine(App.AssetsFolder, "Emoji");
		public Dictionary<string, string> Emojis = [];
		private Dictionary<string, ImageSource> EmojiCache = [];
		public EmojiLoader(){
			Directory.CreateDirectory(EmojiFolder);
			if (!File.Exists(Path.Combine(EmojiFolder,"data.json")))
			{
				File.Create(Path.Combine(EmojiFolder, "data.json"));
			}
			string jsonString = File.ReadAllText(Path.Combine(EmojiFolder, "data.json"));


			Emojis = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
			Emojis ??= [];
		}

		public async Task<ImageSource> GetEmoji(string name)
		{
			if (EmojiCache.TryGetValue(name, out var image))
			{
				return image;
			}

			if (Emojis.TryGetValue(name, out var url))
			{
				using var httpClient = new HttpClient();
				var imageBytes = await httpClient.GetByteArrayAsync(url);

				var bitmap = new BitmapImage();
				using var stream = new MemoryStream(imageBytes);

				bitmap.BeginInit();
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.StreamSource = stream;
				bitmap.EndInit();
				bitmap.Freeze();

				EmojiCache[url] = bitmap;
				return bitmap;
			}
			else { 
				return null;
			}
		}
	}

	public class AutoExpireImageCache
	{
		private class CacheItem
		{
			public ImageSource Image { get; set; }
			public DateTime LastAccess { get; set; }
		}

		private Dictionary<string, CacheItem> cache = new();
		private TimeSpan expiration;
		private DispatcherTimer cleanupTimer;

		public AutoExpireImageCache(TimeSpan expiration, TimeSpan cleanupInterval)
		{
			this.expiration = expiration;

			cleanupTimer = new DispatcherTimer
			{
				Interval = cleanupInterval
			};
			cleanupTimer.Tick += (s, e) => Cleanup();
			cleanupTimer.Start();
		}

		public async Task<ImageSource> GetOrLoad(string url)
		{
			if (cache.TryGetValue(url, out var item))
			{
				item.LastAccess = DateTime.Now;
				return item.Image;
			}
			using var httpClient = new HttpClient();
			var imageBytes = await httpClient.GetByteArrayAsync(url);

			var bitmap = new BitmapImage();
			using var stream = new MemoryStream(imageBytes);

			bitmap.BeginInit();
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.StreamSource = stream;
			bitmap.EndInit();
			bitmap.Freeze();

			cache[url] = new CacheItem { Image = bitmap, LastAccess = DateTime.Now };
			return bitmap;
		}

		private void Cleanup()
		{
			var threshold = DateTime.Now - expiration;
			var keysToRemove = cache.Where(kv => kv.Value.LastAccess < threshold)
								   .Select(kv => kv.Key)
								   .ToList();

			foreach (var key in keysToRemove)
				cache.Remove(key);
		}
	}

}
