using DevExpress.DirectX.Common.Direct3D;
using DevExpress.DirectX.StandardInterop.Direct3D;
using SharpGen.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.System;
using Windows.UI.Composition;

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
        static Stopwatch watchtime = new Stopwatch();
        RecordScreen(Rectangle Screenbound)
        {
            bounds = Screenbound;
        }



        public static Bitmap Recordscreen(Rectangle screenbound)
        {
            Bitmap imagemap = new(screenbound.Width, screenbound.Height);
            using (Graphics g = Graphics.FromImage(imagemap))
            {
                g.CopyFromScreen(screenbound.Left, screenbound.Top, 0, 0, imagemap.Size);
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
    public class FrameCapture
    {
        int ScreenWidth = SystemInformation.VirtualScreen.Width;
        int ScreenHeight = SystemInformation.VirtualScreen.Height;
        int ABSX = SystemInformation.VirtualScreen.X;
        int ABSY = SystemInformation.VirtualScreen.Y;
        private Windows.UI.Composition.Visual _Visual;
        private GraphicsCaptureItem _captureItem;
        private GraphicsCaptureSession _graphicsCaptureSession;
        private Direct3D11CaptureFrame _direct3D11Capture;
        private Direct3D11CaptureFramePool _direct3D11CaptureFramePool;
        
        private Task<Windows.UI.Composition.ContainerVisual> GetVisual()
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
            //if (false)
                //tcs.SetException(new InvalidOperationException("Dispatcher queue is shut down."));

            return tcs.Task;
        }
        private IDirect3DDevice GetDirectdevice()
        {
            using var sharpDxDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware,
                                                     SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);
            var D = CaptureInterop.CreateDirect3DDeviceFromSharpDXDevice2(sharpDxDevice);
            return D;
        }
        private DispatcherQueueController dispatcherQueueController;

        private static Lazy<IDirect3DDevice> device;
        private IDirect3DDevice dev => GetDirectdevice();
        private DirectXPixelFormat Format = DirectXPixelFormat.B8G8R8A8UIntNormalized;

        public  async  Task Stream()
        {

           dispatcherQueueController = DispatcherQueueController.CreateOnDedicatedThread();
           

            dispatcherQueueController.DispatcherQueue.TryEnqueue(() =>
            {
                var Com = new Windows.UI.Composition.Compositor();
                if (Com != null)
                {
                    _Visual = Com.CreateContainerVisual();
                    _Visual.Size = new System.Numerics.Vector2(ScreenWidth, ScreenHeight);

                }

                if (_Visual != null)
                {
                    _captureItem = GraphicsCaptureItem.CreateFromVisual(_Visual);
                    
                }


                try
                {



                    _direct3D11CaptureFramePool = Direct3D11CaptureFramePool.Create(dev, Format, 1, new(ScreenWidth, ScreenHeight));

       
                    _graphicsCaptureSession = _direct3D11CaptureFramePool.CreateCaptureSession(_captureItem);
                       
                    
                    
                    
                    _direct3D11CaptureFramePool.FrameArrived += _direct3D11CaptureFramePool_FrameArrived;
                    _graphicsCaptureSession.StartCapture();

                   

                }
                catch(COMException ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
                finally
                {

                    if (_graphicsCaptureSession != null)
                        _graphicsCaptureSession.Dispose();

                    if (_direct3D11CaptureFramePool != null)
                        _direct3D11CaptureFramePool.Dispose();

                    if (_direct3D11Capture != null)
                        _direct3D11Capture.Dispose();
                    _direct3D11CaptureFramePool?.FrameArrived -= _direct3D11CaptureFramePool_FrameArrived;
                }
            });
            

        }

        private void _direct3D11CaptureFramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {

            var frame =sender.TryGetNextFrame();
            if (frame != null)
            {
                var Surface = frame.Surface;
                
            }
        }
    }
}


