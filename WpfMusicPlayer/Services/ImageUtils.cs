using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfMusicPlayer.Services
{
    public static class ImageUtils
    {
        /// <summary>
        /// Shrinks embedded artwork to a display-sized JPEG. Album art in tags is routinely
        /// 1000x1000 or larger; keeping the originals for every track in a big library costs
        /// hundreds of megabytes of memory for pixels that are never shown at that size.
        /// </summary>
        public static byte[]? CreateThumbnail(byte[]? source, int maxSize = 200)
        {
            if (source == null || source.Length == 0)
                return null;

            try
            {
                using var input = new MemoryStream(source);
                var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count == 0)
                    return null;

                var frame = decoder.Frames[0];
                var longestEdge = Math.Max(frame.PixelWidth, frame.PixelHeight);
                if (longestEdge <= 0)
                    return null;

                // Already small enough - re-encoding would only lose quality
                if (longestEdge <= maxSize)
                    return source;

                var scale = (double)maxSize / longestEdge;
                var scaled = new TransformedBitmap(frame, new ScaleTransform(scale, scale));
                scaled.Freeze();

                var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
                encoder.Frames.Add(BitmapFrame.Create(scaled));

                using var output = new MemoryStream();
                encoder.Save(output);
                return output.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not create album art thumbnail: {ex.Message}");
                return null;
            }
        }
    }
}
