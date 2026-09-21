using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Animation;

namespace CToC.Screen
{

    public class TakeScreenSnippit
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetDesktopWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        public static Image CaptureDesktop()
        {
            return CaptureWindow(GetDesktopWindow());
        }

        public static Bitmap CaptureActiveWindow()
        {
            return CaptureWindow(GetForegroundWindow());
        }

        public static Bitmap CaptureWindow(IntPtr handle)
        {
            var rect = new Rect();
            GetWindowRect(handle, ref rect);
            var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            var result = new Bitmap(bounds.Width, bounds.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            }

            return result;
        }
    }

    public class RecordScreen
    {
        private Rectangle bounds;
        private string outputPath = "";
        private string tempPath = "";
        private int fileCount = 1;
        private List<string> inputImageSequence = new List<string>();
        static Stopwatch  watchtime = new Stopwatch();
        RecordScreen(Rectangle Screenbound)
        {
            bounds = Screenbound;
        }

        

        public static Bitmap Recordscreen(Rectangle screenbound)
        {
            Bitmap imagemap = new(screenbound.Width,screenbound.Height);
            using (Graphics g = Graphics.FromImage(imagemap))
            {
                g.CopyFromScreen(screenbound.Left, screenbound.Top,0,0, imagemap.Size);
            }
            return imagemap;
            
        }
        public static byte[] BitmapTobyteConverter(Bitmap image)
        {
            using (MemoryStream stream = new())
            {
                image.Save(stream, ImageFormat.Bmp);
                return stream.ToArray();
            }
        }
        public static Bitmap ByteToBitmMap(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                return new(stream);
            }
        }
    }
}


