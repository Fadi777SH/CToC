using CToC.Server;

using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media.Imaging;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Compression;
using Windows.System;
using Windows.UI.Composition;
using BinaryFormatter;
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

    public class ConversionClass
    {
        public static Bitmap Recordscreen(Rectangle screenbound)
        {
            Bitmap imagemap = new(screenbound.Width, screenbound.Height);
            using (Graphics g = Graphics.FromImage(imagemap))
            {
                g.CopyFromScreen(screenbound.Left, screenbound.Top, 0, 0, imagemap.Size);
            }
            return imagemap;

        }
        public static byte[]? BitmapTobyteConverter(Bitmap image)
        {
            using (MemoryStream stream = new())
            {
                try
                {
                    image.Save(stream, ImageFormat.Bmp);
                    return stream.ToArray();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                return null;
            }
        }
        public static Bitmap ByteToBitmMap(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                return new(stream);
            }
        }
        public static int frameWidth = 870;
        public static int frameHeight = 500;
        public static Bitmap compressbitmap(Bitmap image, int targetWidth, int TargetHeight)
        {
            int Width = 0;
            int Height = 0;
            double AspectRatioOrigin = image.Width / image.Height;
            double AspectRatio = targetWidth / (double)TargetHeight;
            if (AspectRatio > AspectRatioOrigin)
            {
                Width = (int)(TargetHeight * AspectRatioOrigin);
                Height = TargetHeight;

            }
            else
            {
                Width = targetWidth;
                Height = TargetHeight;
            }
            return Resizebitmap(image, frameWidth, frameHeight);

        }
        public static Bitmap Resizebitmap(Bitmap orgin , int width,int height)
        {
            Bitmap bitmap = new(width, height);
            using Graphics graphics = Graphics.FromImage(bitmap);
            
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            graphics.DrawImage(orgin, 0, 0, width, height);
            return bitmap;
            
        }

        public static void ClassToByteArray(Direct3D11CaptureFrame str)
        {
            BinaryFormatter.BinaryConverter binaryConverter=new();
            any a = new();
            a.d = str;
            var w = binaryConverter.Serialize(str);
            var f = binaryConverter.Deserialize<Direct3D11CaptureFrame>(w);
           // return b;
        }
        class any 
        {
            public Direct3D11CaptureFrame d { get; set; }
            public any()
            {

            }
        }

    }
    public class FrameCapture : IDisposable
    {
        int ScreenWidth = SystemInformation.VirtualScreen.Width;
        int ScreenHeight = SystemInformation.VirtualScreen.Height;
        int ABSX = SystemInformation.VirtualScreen.X;
        int ABSY = SystemInformation.VirtualScreen.Y;
        private Windows.UI.Composition.Visual _Visual;

        private  GraphicsCaptureItem _captureItem;
        public   GraphicsCaptureSession _graphicsCaptureSession;
        private  Direct3D11CaptureFrame _direct3D11Capture;
        public   Direct3D11CaptureFramePool _direct3D11CaptureFramePool;
        public delegate void FramArrivedHandler(Direct3D11CaptureFramePool Framepool, GraphicsCaptureSession graphics);
        public static event FramArrivedHandler? FrameArrived;
        public bool IsStreaming
        {
            get
            {
               return CurrentState;
            }
        }
        private bool CurrentState = false;
        public void Dispose()
        {
            if (_direct3D11CaptureFramePool != null)
                _direct3D11CaptureFramePool.Dispose();
            if(_graphicsCaptureSession!=null)
                _graphicsCaptureSession.Dispose();
            if(dev!=null)
                dev.Dispose();
            
        }

        private  Task<Windows.UI.Composition.ContainerVisual> GetVisual()
        {
            var dispatcherQueueHandler = DispatcherQueueController.CreateOnDedicatedThread();

            Windows.UI.Composition.ContainerVisual VS;
            var tcs = new TaskCompletionSource<ContainerVisual>(
                     TaskCreationOptions.RunContinuationsAsynchronously);

            dispatcherQueueHandler.DispatcherQueue.TryEnqueue(() =>
            {
                //ToDO -> you should get the optimal visual of the windows 
                var Com = new Windows.UI.Composition.Compositor();

                VS = Com.CreateContainerVisual();
                tcs.SetResult(VS);



            });


            return tcs.Task;
        }
        public static IDirect3DDevice GetDirectdevice()
        {
            using (var sharpDxDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware,
                                                     SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport))
            {
                var D = CaptureInterop.CreateDirect3DDeviceFromSharpDXDevice2(sharpDxDevice);
                return D;
            }
        }
        Windows.UI.WindowId windowId = new();
        DisplayId displayId = new();
        private DispatcherQueueController dispatcherQueueController;

        //private static Lazy<IDirect3DDevice> device = new(() => GetDirectdevice());
        public  IDirect3DDevice dev;
        private DirectXPixelFormat Format = DirectXPixelFormat.B8G8R8A8UIntNormalized;

        public  void Stream()
        {
            if (CurrentState == false)
            {
                _captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(displayId);
                dev = GetDirectdevice();

                _direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(dev, Format, 1, new(ScreenWidth, ScreenHeight));

                _direct3D11CaptureFramePool.FrameArrived += UdpServer._direct3D11CaptureFramePool_FrameArrived;
                _graphicsCaptureSession = _direct3D11CaptureFramePool.CreateCaptureSession(_captureItem);
                _graphicsCaptureSession.StartCapture();
            }
            CurrentState = true;


        }

        public void EndStream()
        {
            _direct3D11CaptureFramePool.FrameArrived -= UdpServer._direct3D11CaptureFramePool_FrameArrived;
            Dispose();
            CurrentState = false;
        }
    }
}

