using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Screen = System.Windows.Forms.Screen;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using Application = System.Windows.Application;
using LiveChatMessage = GrpcYoutube.LiveChatMessage;
using mType = GrpcYoutube.LiveChatMessageSnippet.Types.TypeWrapper.Types.Type;
namespace ChatOverlay
{
	public partial class Chat : Window
	{
		public List<RichLine> Lines { get; } = [];
		List<LiveChatMessage> data = [];


		public Chat(List<LiveChatMessage> data)
		{
			InitializeComponent();
			this.data = data;
			Loaded += OnLoaded;
		}

		private async void AutoCloseWindow(int milliseconds)
		{
			await Task.Delay(milliseconds);
			this.Close();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			MakeClickThrough();
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
				if ((bool)message.AuthorDetails.IsChatModerator) {
					colors = Brushes.Blue;
				}
				if ((bool)message.AuthorDetails.IsChatOwner)
				{
					colors = Brushes.Yellow;
				}
				if (message.Snippet.Type == mType.SuperChatEvent)
				{
					line.Parts.Add(new InlinePart { Text = $" [Super Chat] ", Color = colors });
				}
				if (message.Snippet.Type == mType.SuperStickerEvent)
				{
					line.Parts.Add(new InlinePart { Text = $" [Super Sticker] ", Color = colors });
				}
				if (!string.IsNullOrEmpty(message.AuthorDetails.ProfileImageUrl))
				{

					line.Parts.Add(new InlinePart {Text = message.AuthorDetails.DisplayName, Color = colors, ImageUrl = message.AuthorDetails.ProfileImageUrl });
				}
				if (!string.IsNullOrEmpty(message.Snippet.DisplayMessage))
				{
					line.Parts.Add(new InlinePart { Text = " : ", Color = Brushes.White });
					line.Parts.Add(new InlinePart { Text = message.Snippet.DisplayMessage, Color = Brushes.White });
				}
				line.Color = Bgcolors;
				Lines.Add(line);
			}

			RenderLines();
			Dispatcher.InvokeAsync(PositionToBottomLeft, System.Windows.Threading.DispatcherPriority.Render);
			AutoCloseWindow(10000);
		}

		private void RenderLines()
		{
			ChatPanel.Children.Clear();

			foreach (var line in Lines)
			{
				var border = new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(102, 0, 0, 0)), // #66000000 (โปร่งแสงดำ)
					Padding = new Thickness(4),
					CornerRadius = new CornerRadius(4),
					Margin = new Thickness(2)
				};

				var textBlock = new TextBlock { FontSize = 26, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White };

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
					Width = 30,
					Height = 30,
					Margin = new Thickness(0, 0, 5, 0),
					Clip = new EllipseGeometry(new Point(15, 15), 15, 15),
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
	}
}
