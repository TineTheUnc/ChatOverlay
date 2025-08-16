using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using LiveChatMessage = GrpcYoutube.LiveChatMessage;
using MessageBox = System.Windows.MessageBox;
using mType = GrpcYoutube.LiveChatMessageSnippet.Types.TypeWrapper.Types.Type;
using Point = System.Windows.Point;
using Screen = System.Windows.Forms.Screen;
namespace ChatOverlay
{


	public partial class Chat : Window
	{
		public List<RichLine> Lines { get; } = [];
		List<LiveChatMessage> data = [];
		[GeneratedRegex(@":[A-Za-z0-9_-]+:" 
		+ @"|(?:"                                     
		+ @"(?:[\uD800-\uDBFF][\uDC00-\uDFFF])"     
		+ @"(?:\uFE0F|\uFE0E)?"                      
		+ @"(?:\u200D(?:[\uD800-\uDBFF][\uDC00-\uDFFF])(?:\uFE0F|\uFE0E)?)*" 
		+ @")"
		+ @"|(?:[\u2600-\u27BF]\uFE0F?)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
		private static partial Regex MyRegex();
		private System.Windows.Threading.DispatcherTimer hideTimer;
		private TimeSpan hideAfter = TimeSpan.FromSeconds(10); 

		public Chat()
		{
			InitializeComponent();
			hideTimer = new System.Windows.Threading.DispatcherTimer
			{
				Interval = hideAfter
			};
			hideTimer.Tick += HideTimer_Tick;
			Loaded += OnLoaded;
		}

		private void HideTimer_Tick(object sender, EventArgs e)
		{
			hideTimer.Stop();
			this.Visibility = Visibility.Hidden;
		}


		public async Task AddMessages(List<LiveChatMessage> data)
		{
			foreach (var message in data)
			{
				var line = new RichLine();
				var Bgcolors = Colors.Black;
				if (message.Snippet.SuperChatDetails != null)
				{
					if (message.Snippet.SuperChatDetails.Tier > 1)
					{
						Bgcolors = message.Snippet.SuperChatDetails.Tier switch
						{
							1 => Colors.Blue,
							2 => Colors.LightBlue,
							3 => Colors.Green,
							4 => Colors.Yellow,
							5 => Colors.Orange,
							6 => Colors.Magenta,
							7 => Colors.Red,
							_ => Colors.LightGray,
						};
					}
				}

				if (message.Snippet.SuperStickerDetails != null)
				{
					if (message.Snippet.SuperStickerDetails.Tier > 1)
					{
						Bgcolors = message.Snippet.SuperChatDetails.Tier switch
						{
							1 => Colors.Blue,
							2 => Colors.LightBlue,
							3 => Colors.Green,
							4 => Colors.Yellow,
							5 => Colors.Orange,
							6 => Colors.Magenta,
							7 => Colors.Red,
							8 => Colors.Red,
							9 => Colors.Red,
							10 => Colors.Red,
							11 => Colors.Red,
							_ => Colors.LightGray,
						};
					}
				}
				var colors = Brushes.LightGray;
				if ((bool)message.AuthorDetails.IsChatSponsor)
				{
					colors = Brushes.Green;
				}
				if ((bool)message.AuthorDetails.IsChatModerator)
				{
					colors = Brushes.Blue;
				}
				if ((bool)message.AuthorDetails.IsChatOwner)
				{
					colors = Brushes.Yellow;
				}
				if (message.Snippet.Type == mType.SuperChatEvent)
				{
					line.Parts.Add(new InlinePart { Text = $"[Super Chat] ", Color = colors });
				}
				if (message.Snippet.Type == mType.SuperStickerEvent)
				{
					line.Parts.Add(new InlinePart { Text = $" [Super Sticker] ", Color = colors });
				}
				if (!string.IsNullOrEmpty(message.AuthorDetails.ProfileImageUrl))
				{

					line.Parts.Add(new InlinePart { Text = message.AuthorDetails.DisplayName, Color = colors, ImageUrl = message.AuthorDetails.ProfileImageUrl });
				}
				if (!string.IsNullOrEmpty(message.Snippet.DisplayMessage))
				{
					line.Parts.Add(new InlinePart { Text = " : ", Color = Brushes.White });
					var m = message.Snippet.DisplayMessage;
					var matches = MyRegex().Matches(m);
					var result = new List<string>();

					int lastIndex = 0;

					foreach (Match match in matches)
					{
						// ข้อความก่อน emoji
						if (match.Index > lastIndex)
						{
							result.Add(m[lastIndex..match.Index]);
						}

						// Emoji / shortcode
						string token = match.Value;
						if (token.StartsWith(':') && token.EndsWith(':'))
							token = token.Substring(1, token.Length - 2);

						result.Add(token);

						lastIndex = match.Index + match.Length;
					}

					if (lastIndex < m.Length)
						result.Add(m[lastIndex..]);
					var app = (App)Application.Current;
					foreach (var part in result)
					{
						var emoji = await app.EmojiLoader.GetEmoji(part);
						if (emoji != null)
						{
							line.Parts.Add(new InlinePart { Image = emoji });
						}
						else
						{
							line.Parts.Add(new InlinePart { Text = part, Color = Brushes.White });
						}
					}
				}
				line.Color = Bgcolors;
				
				AppendLine(line);
			}
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			MakeClickThrough();
			await Dispatcher.InvokeAsync(PositionToBottomLeft, System.Windows.Threading.DispatcherPriority.Render);
		}


		public async void AppendLine(RichLine line)
		{
			Lines.Add(line);

			int maxLines = 10;
			while (Lines.Count > maxLines)
			{
				Lines.RemoveAt(0);
				if (ChatPanel.Children.Count > 0)
					ChatPanel.Children.RemoveAt(0);
			}

			var border = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(102, 0, 0, 0)), 
				Padding = new Thickness(4),
				CornerRadius = new CornerRadius(4),
				Margin = new Thickness(2)
			};
			var screen = Screen.PrimaryScreen;
			var workingArea = screen.WorkingArea;
			var textBlock = new TextBlock { FontSize = 14, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White, MaxWidth = workingArea.Width * 3 / 8 };

			RenderPartsAsync(line.Parts, textBlock);

			textBlock.Effect = new DropShadowEffect
			{
				Color = line.Color,
				BlurRadius = 3,
				ShadowDepth = 1,
				Opacity = 0.8
			};

			border.Child = textBlock;

			ChatPanel.Children.Add(border);
			if (this.Visibility != Visibility.Visible)
				this.Visibility = Visibility.Visible;

			hideTimer.Stop();
			hideTimer.Start();
			AnimateScroll();
			ChatPanel.UpdateLayout();
			await Dispatcher.InvokeAsync(PositionToBottomLeft, System.Windows.Threading.DispatcherPriority.Render);
		}

		private void AnimateScroll()
		{
			if (ChatPanel.Parent is ScrollViewer scrollViewer)
			{
				scrollViewer.ScrollToBottom();
			}
		}

		public async void RenderPartsAsync(IEnumerable<InlinePart> parts, TextBlock textBlock)
		{
			textBlock.Inlines.Clear();

			foreach (var part in parts)
			{
				if (!string.IsNullOrEmpty(part.ImageUrl))
				{
					var inlineImage = await LoadImageAsync(part.ImageUrl);
					if (inlineImage != null)
						textBlock.Inlines.Add(inlineImage);
				}

				if (!string.IsNullOrEmpty(part.Text))
				{
					textBlock.Inlines.Add(new Run(part.Text) { Foreground = part.Color ?? Brushes.LightGray });
				}

				if (part.Image != null) {
					var image = new Image
					{
						Source = part.Image,
						Width = 20,
						Height = 20,
						Margin = new Thickness(0, 0, 0, 0),
						VerticalAlignment = VerticalAlignment.Center
					};

					textBlock.Inlines.Add(new InlineUIContainer(image) { BaselineAlignment = BaselineAlignment.Center });
				}
			}
		}

		public static async Task<InlineUIContainer> LoadImageAsync(string imageUrl)
		{
			try
			{

				var app = (App)Application.Current;
				var imageSource = await app.ImageCache.GetOrLoad(imageUrl);

				var image = new Image
				{
					Source = imageSource,
					Width = 20,
					Height = 20,
					Margin = new Thickness(0, 0, 5, 0),
					Clip = new EllipseGeometry(new Point(10, 10), 10, 10),
					VerticalAlignment = VerticalAlignment.Center
				};

				return new InlineUIContainer(image) { BaselineAlignment = BaselineAlignment.Center };
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading image: " + ex.Message);
				return null;
			}
		}

		private void PositionToBottomLeft()
		{
			var screen = Screen.PrimaryScreen;
			var workingArea = screen.WorkingArea;
			Left = workingArea.Left - 5;
			Top = workingArea.Bottom - ActualHeight - 195;
		}

		private void MakeClickThrough()
		{
			var hwnd = new WindowInteropHelper(this).Handle;
			int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
		}

		const int GWL_EXSTYLE = -20;
		const int WS_EX_LAYERED = 0x80000;
		const int WS_EX_TRANSPARENT = 0x20;

		[DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	}

	public class RichLine
	{
		public List<InlinePart> Parts { get; set; } = [];
		public Color Color { get; set; } = Colors.Black;
	}

	public class InlinePart
	{
		public string Text { get; set; }
		public Brush Color { get; set; }
		public string ImageUrl { get; set; }

		public ImageSource Image { get; set; }
	}
}
