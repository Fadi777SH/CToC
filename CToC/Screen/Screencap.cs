using DevExpress.DirectX.Common.Direct3D;
using DevExpress.DirectX.StandardInterop.Direct3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Media.Animation;
using Vortice.DXGI;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Windows.Devices.Display.Core;
using Windows.Graphics;
using Windows.Graphics.DirectX.Direct3D11;
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



    public class ScreenTarge
    {
        DisplayTarget? displayTarget;
        DisplayTask? displayTask;
        DisplayTaskPool? DisplayTaskPool;
        DisplaySource? displaySource;
        DisplaySurface? displaySurface;
        DisplayAdapter? displayAdapter;
        DisplayManager? displayManager;
        public  DisplayTarget? GetDisplayTargets()
        {
            displayManager = DisplayManager.Create(DisplayManagerOptions.None);
            foreach(var dev in displayManager.GetCurrentTargets())
            {
                if (dev != null) return dev;
            }
            return null;
        }

        public void RunTask()
        {
            var Des = GetDescription();
            displayTarget = GetDisplayTargets();
            var ID = displayTarget?.Adapter.Id;
            displayAdapter = displayTarget?.Adapter;
            var displayDev = displayManager?.CreateDisplayDevice(displayAdapter);
            displaySurface = displayDev?.CreatePrimary(displayTarget, Des);
            DisplayTaskPool = displayDev?.CreateTaskPool();
            displayTask = DisplayTaskPool?.CreateTask();
            displaySource = displayDev?.CreateScanoutSource(displayTarget);
            var scanout = displayDev?.CreateSimpleScanout(displaySource, displaySurface, 1, 0);
            displayTask?.SetScanout(scanout);
            DisplayTaskPool?.ExecuteTask(displayTask);
            
        }

        public static void Skra(ref byte[] bytes , ref int width , ref int height)
        {
            D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport,
                null, out Vortice.Direct3D11.ID3D11Device device, out Vortice.Direct3D11.ID3D11DeviceContext context);

            using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
            using var adapter = dxgiDevice.GetAdapter();

            adapter.EnumOutputs(0, out IDXGIOutput output);
            using var output1 = output.QueryInterface<IDXGIOutput1>();
            using var duplication = output1.DuplicateOutput(device);
            output.Dispose();

            Vortice.Direct3D11.ID3D11Texture2D staging = null;
            Texture2DDescription stagingDesc = default;

            var result = duplication.AcquireNextFrame(500, out var frameInfo, out var desktopResource);
            if (result.Failure)
                 // timeout or no new frame yet

            using (desktopResource)
            {
                using var texture = desktopResource.QueryInterface<Vortice.Direct3D11.ID3D11Texture2D>();

                // Create the staging texture once (or if size changed)
                var desc = texture.Description;
                if (staging == null || stagingDesc.Width != desc.Width || stagingDesc.Height != desc.Height)
                {
                    staging?.Dispose();

                    desc.Usage = ResourceUsage.Staging;
                    desc.CPUAccessFlags = CpuAccessFlags.Read;
                    desc.BindFlags = BindFlags.None;
                    desc.MiscFlags = ResourceOptionFlags.None;

                    staging = device.CreateTexture2D(desc);
                    stagingDesc = desc;
                }

                context.CopyResource(staging, texture);

                var map = context.Map(staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

                width = (int)stagingDesc.Width;
                 height = (int)stagingDesc.Height;
                int bytesPerPixel = 4; // BGRA8
                var frameBytes = new byte[width * height * bytesPerPixel];

                unsafe
                {
                    byte* src = (byte*)map.DataPointer;
                    fixed (byte* dstPtr = frameBytes)
                    {
                        byte* dst = dstPtr;
                        for (int row = 0; row < height; row++)
                        {
                            Buffer.MemoryCopy(
                                src + row * map.RowPitch,
                                dst + row * width * bytesPerPixel,
                                width * bytesPerPixel,
                                width * bytesPerPixel);
                        }
                    }
                }

                context.Unmap(staging, 0);
                bytes = frameBytes;
                //OnFrameCaptured(frameBytes, width, height);
                

                duplication.ReleaseFrame();
            }
        }

        static public void OnFrameCaptured(byte[] frambyte , int width , int height)
        {

        }
        static public Bitmap BytesToBitmap(byte[] frameBytes, int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            System.Runtime.InteropServices.Marshal.Copy(frameBytes, 0, bmpData.Scan0, frameBytes.Length);

            bmp.UnlockBits(bmpData);
            return bmp;
        }
        public static DisplayPrimaryDescription GetDescription()
        {
            Direct3DMultisampleDescription noMsaa = new(1, 0);

            DisplayPrimaryDescription description = new(
                1920, 1080,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                Windows.Graphics.DirectX.DirectXColorSpace.RgbFullG22NoneP709,
                false,
                noMsaa);
            return description;
        }


    }
}


