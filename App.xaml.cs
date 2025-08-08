
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ChatOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
	{
		public AutoExpireImageCache ImageCache { get; }

		public App()
		{
			// กำหนด expiration 10 นาที, cleanup ทุก 5 นาที
			ImageCache = new AutoExpireImageCache(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5));
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
