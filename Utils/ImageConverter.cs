using ImageMagick;
using System.Text;

namespace StarcoreDiscordBot
{
    public class ImageConverter
    {

        public static StringBuilder ConvertImage(MagickImage image, BitMode Mode)
        {

            int width = image.Width;
            int height = image.Height;

            using var c = image.GetPixelsUnsafe();

            StringBuilder frame = new StringBuilder();
            for (int y1 = 0; y1 < height; y1++)
            {
                for (int x1 = 0; x1 < width; x1++)
                {
                    var p = c.GetPixel(x1, y1).ToColor();
                    if (p.A < 100)
                    {
                        frame.Append(ColorToChar(Mode, 0, 0, 0));
                    }
                    else
                    {
                        byte[] b = p.ToByteArray();
                        frame.Append(ColorToChar(Mode, b[0], b[1], b[2]));
                    }
                }
                frame.Append("\n");
            }

            return frame;

        }
        private static char ColorToChar(BitMode mode, byte r, byte g, byte b)
        {
            return mode switch
            {
                BitMode.Bit3 => ColorToChar3Bit(r, g, b),
                BitMode.Bit5 => ColorToChar5Bit(r, g, b),
                _ => '0',
            };
        }
        private static char ColorToChar3Bit(byte r, byte g, byte b)
        {
            return (char)((0xe100) + ((r >> 5) << 6) + ((g >> 5) << 3) + (b >> 5));
        }
        private static char ColorToChar5Bit(byte r, byte g, byte b)
        {
            return (char)((uint)0x3000 + ((r >> 3) << 10) + ((g >> 3) << 5) + (b >> 3));
        }

        public enum BitMode
        {
            Bit3,
            Bit5,
        }

    }
}
