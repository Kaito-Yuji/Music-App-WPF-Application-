using System.Globalization;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WpfMusicPlayer.Converters
{
    public class ByteArrayToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is byte[] imageData && imageData.Length > 0)
            {
                try
                {
                    var image = new BitmapImage();
                    using var stream = new MemoryStream(imageData);
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                catch
                {
                    // Return null if image can't be loaded
                }
            }
            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeSpanToSecondsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
                return timeSpan.TotalSeconds;
            return 0.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double seconds)
                return TimeSpan.FromSeconds(seconds);
            return TimeSpan.Zero;
        }
    }

    public class PlayModeToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Models.PlayMode playMode)
            {
                return playMode switch
                {
                    Models.PlayMode.Normal => "⏭️",
                    Models.PlayMode.RepeatOne => "🔂",
                    Models.PlayMode.RepeatAll => "🔁",
                    Models.PlayMode.Shuffle => "🔀",
                    _ => "⏭️"
                };
            }
            return "⏭️";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShuffleToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isShuffleOn)
            {
                return isShuffleOn ? "🔀" : "➡️"; // Shuffle icon when on, forward arrow when off
            }
            return "➡️";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RepeatModeToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Models.RepeatMode repeatMode)
            {
                return repeatMode switch
                {
                    Models.RepeatMode.Off => "🔁", // Normal repeat icon
                    Models.RepeatMode.RepeatAll => "🔁", // Repeat all icon (could be highlighted)
                    Models.RepeatMode.RepeatOne => "🔂", // Repeat one icon
                    _ => "🔁"
                };
            }
            return "🔁";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ListViewItemIndexConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ListViewItem listViewItem)
            {
                var listView = ItemsControl.ItemsControlFromItemContainer(listViewItem) as ListView;
                if (listView != null)
                {
                    int index = listView.ItemContainerGenerator.IndexFromContainer(listViewItem);
                    return (index + 1).ToString();
                }
            }
            return "0";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
