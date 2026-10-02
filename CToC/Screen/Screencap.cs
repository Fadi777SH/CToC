using BinaryFormatter;
using CToC.Server;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media.Imaging;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Compression;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Composition;


namespace CToC.Screen
{

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

        public static async Task<byte[]> ClassToByteArray(InMemoryRandomAccessStream stream)
        {
            stream.Seek(0);
            var bytes = new byte[stream.Size];
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(bytes);
            reader.DetachStream(); // keeps the original stream open
            return bytes;
        }
        public static async Task<InMemoryRandomAccessStream>  ByteArrayToClass(byte[] Bytes)
        {
            var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(Bytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }
            stream.Seek(0);
            return stream;
        }
        public static byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
                gzip.Write(data, 0, data.Length);
            return output.ToArray();   // after gzip is disposed/flushed
        }

        public static byte[] Decompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }
        class any
        {
            public SoftwareBitmap s { get; set; }

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
               return StreamCurrentState;
            }
        }
        private bool StreamCurrentState = false;
        public void Dispose()
        {
            if (_direct3D11CaptureFramePool != null)
                _direct3D11CaptureFramePool.Dispose();
            if(_graphicsCaptureSession!=null)
                _graphicsCaptureSession.Dispose();
            if(dev!=null)
                dev.Dispose();
            
        }

        public static IDirect3DDevice GetDirectdevice()
        {
            using (var sharpDxDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware,
                                                     SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport))
            {
                var D = CaptureInterop.CreateDirect3DDeviceFromSharpDXDevice(sharpDxDevice);
                return D;
            }
        }
        Windows.UI.WindowId windowId = new();
        DisplayId displayId = new();


        //private static Lazy<IDirect3DDevice> device = new(() => GetDirectdevice());
        public static IDirect3DDevice dev;
        private DirectXPixelFormat Format = DirectXPixelFormat.B8G8R8A8UIntNormalized;

     

        public void Stream()
        {
            if (StreamCurrentState == false)
            {
                try
                {
                    _captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(displayId);
                    
                    dev = GetDirectdevice();
                    

                    _direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(dev, Format, 1, new(ScreenWidth, ScreenHeight));
                    

                    _direct3D11CaptureFramePool.FrameArrived += UdpServer._direct3D11CaptureFramePool_FrameArrived;
                    _graphicsCaptureSession = _direct3D11CaptureFramePool.CreateCaptureSession(_captureItem);
                    _graphicsCaptureSession.StartCapture();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            StreamCurrentState = true;


        }

        public void EndStream()
        {
            _direct3D11CaptureFramePool.FrameArrived -= UdpServer._direct3D11CaptureFramePool_FrameArrived;
            Dispose();
            StreamCurrentState = false;
        }

    }
}

