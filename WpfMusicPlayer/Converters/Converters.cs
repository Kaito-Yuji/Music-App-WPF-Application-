using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.ViewModels;

namespace WpfMusicPlayer.Converters
{
    public class ByteArrayToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not byte[] { Length: > 0 } imageData)
                return null;

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
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class TimeSpanToSecondsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is TimeSpan timeSpan ? timeSpan.TotalSeconds : 0.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is double seconds ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
        }
    }

    public class TimeSpanToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is TimeSpan timeSpan ? timeSpan.ToString(@"m\:ss") : "0:00";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class PlaybackStateToSymbolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is PlaybackState.Playing ? "⏸️" : "▶️";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class ShuffleToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? "🔀" : "➡️";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class RepeatModeToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is RepeatMode.RepeatOne ? "🔂" : "🔁";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>Tints a toggle button so an enabled mode is visible at a glance.</summary>
    public class ActiveToggleToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Active = Freeze(Color.FromRgb(0x1d, 0xb9, 0x54));
        private static readonly SolidColorBrush Inactive = Freeze(Color.FromRgb(0x28, 0x28, 0x28));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isActive = value switch
            {
                bool flag => flag,
                RepeatMode mode => mode != RepeatMode.Off,
                _ => false
            };

            return isActive ? Active : Inactive;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }

    public class KaraokeStateToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                KaraokeState.Active => Brushes.Orange,
                KaraokeState.Ready => Brushes.Green,
                _ => Brushes.Transparent
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class KaraokeStateToTooltipConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                KaraokeState.Active => "Karaoke mode is on - click to return to the original track",
                KaraokeState.Ready => "Stems are ready - click to play the instrumental",
                KaraokeState.NeedsSeparation => "Click to separate the vocals first (this takes a few minutes)",
                _ => "Karaoke mode - play a song first"
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>Shows a watermark only while the bound text is empty.</summary>
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>Short label for the mood column: an icon plus the mood name.</summary>
    public class MoodToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                Mood.Happy => "🙂 Happy",
                Mood.Energetic => "⚡ Energetic",
                Mood.Calm => "🌙 Calm",
                Mood.Sad => "💧 Sad",
                _ => "—"
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Colours the mood column so the four quadrants are distinguishable at a glance: warm for
    /// positive moods, cool for subdued ones.
    /// </summary>
    public class MoodToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Happy = Freeze(Color.FromRgb(0x5b, 0xd6, 0x7a));
        private static readonly SolidColorBrush Energetic = Freeze(Color.FromRgb(0xff, 0x9d, 0x4d));
        private static readonly SolidColorBrush Calm = Freeze(Color.FromRgb(0x6f, 0xb4, 0xe8));
        private static readonly SolidColorBrush Sad = Freeze(Color.FromRgb(0x9a, 0x8f, 0xd6));
        private static readonly SolidColorBrush Unknown = Freeze(Color.FromRgb(0x70, 0x70, 0x70));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                Mood.Happy => Happy,
                Mood.Energetic => Energetic,
                Mood.Calm => Calm,
                Mood.Sad => Sad,
                _ => Unknown
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }

    public class ListViewItemIndexConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not ListViewItem listViewItem)
                return "0";

            if (ItemsControl.ItemsControlFromItemContainer(listViewItem) is not ListView listView)
                return "0";

            return (listView.ItemContainerGenerator.IndexFromContainer(listViewItem) + 1).ToString();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
