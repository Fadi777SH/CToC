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

        public static void Skra(ref byte[] bytes, ref int width, ref int height)
        {
            Vortice.Direct3D11. ID3D11Device device = null;
            Vortice.Direct3D11.ID3D11DeviceContext context = null;
            IDXGIAdapter adapter = null;
            IDXGIOutput output = null;
            IDXGIOutput1 output1 = null;
            IDXGIOutputDuplication duplication = null;
            Vortice.Direct3D11.ID3D11Texture2D staging = null;

            try
            {
                D3D11.D3D11CreateDevice(
                    null,
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    null,
                    out device,
                    out context);

                using (var dxgiDevice = device.QueryInterface<IDXGIDevice>())
                {
                    adapter = dxgiDevice.GetAdapter();
                }

                adapter.EnumOutputs(0, out output);

                output1 = output.QueryInterface<IDXGIOutput1>();

                duplication = output1.DuplicateOutput(device);

                bool frameAcquired = false;

                try
                {
                    var result = duplication.AcquireNextFrame(
                        300,
                        out var frameInfo,
                        out var desktopResource);

                    if (result.Failure)
                    {
                        // No new frame.
                        return;
                    }

                    frameAcquired = true;

                    using (desktopResource)
                    using (var texture =
                        desktopResource.QueryInterface<Vortice.Direct3D11.ID3D11Texture2D>())
                    {
                        var desc = texture.Description;

                        width = (int)desc.Width;
                        height = (int)desc.Height;

                        // ---------------------------------------------
                        // Create staging texture
                        // ---------------------------------------------

                        var stagingDesc = desc;

                        stagingDesc.Usage = ResourceUsage.Staging;
                        stagingDesc.CPUAccessFlags = CpuAccessFlags.Read;
                        stagingDesc.BindFlags = BindFlags.None;
                        stagingDesc.MiscFlags = ResourceOptionFlags.None;

                        staging = device.CreateTexture2D(stagingDesc);

                        // ---------------------------------------------
                        // GPU -> CPU staging texture
                        // ---------------------------------------------

                        context.CopyResource(staging, texture);

                        var map = context.Map(
                            staging,
                            0,
                            MapMode.Read,
                            Vortice.Direct3D11.MapFlags.None);

                        try
                        {
                            const int bytesPerPixel = 4;

                            int rowSize = width * bytesPerPixel;
                            int totalSize = rowSize * height;

                            // This is the important part:
                            // bytes now becomes the actual frame buffer.
                            if (bytes == null || bytes.Length != totalSize)
                            {
                                bytes = new byte[totalSize];
                            }

                            unsafe
                            {
                                byte* src = (byte*)map.DataPointer;

                                fixed (byte* dstPtr = bytes)
                                {
                                    byte* dst = dstPtr;

                                    for (int row = 0; row < height; row++)
                                    {
                                        Buffer.MemoryCopy(
                                            src + (row * map.RowPitch),
                                            dst + (row * rowSize),
                                            rowSize,
                                            rowSize);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            context.Unmap(staging, 0);
                        }
                    }
                }
                finally
                {
                    if (frameAcquired)
                    {
                        duplication.ReleaseFrame();
                    }
                }
            }
            finally
            {
                // Dispose everything we created.

                staging?.Dispose();
                duplication?.Dispose();
                output1?.Dispose();
                output?.Dispose();
                adapter?.Dispose();
                context?.Dispose();
                device?.Dispose();
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


