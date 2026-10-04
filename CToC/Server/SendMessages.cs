using CToC.Screen;
using FFMpegCore.Pipes;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Windows.Input;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace CToC
{
    class Transfer
    {
        public byte[] Buffer;
        public bool[] Got;
        public int Received;
        public Transfer(int total, int count) { Buffer = new byte[total]; Got = new bool[count]; }
    }
    public class SoftwareBitmapFrame : IVideoFrame
    {
        private readonly byte[] _data;
        public int Width { get; }
        public int Height { get; }
        public string Format => "bgra";   // matches BitmapPixelFormat.Bgra8

        public SoftwareBitmapFrame(SoftwareBitmap sb)
        {
            using var bgra = SoftwareBitmap.Convert(sb, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            Width = bgra.PixelWidth;
            Height = bgra.PixelHeight;
            _data = new byte[Width * Height * 4];
            bgra.CopyToBuffer(_data.AsBuffer());
        }

        public void Serialize(Stream s) => s.Write(_data, 0, _data.Length);

        public Task SerializeAsync(Stream s, CancellationToken ct) =>
            s.WriteAsync(_data, 0, _data.Length, ct);

        public void Dispose() { }
    }
    public enum MessageType
    {
        Keyboard,
        MouseChange,
        Mousepoint,
        Fram,
        Error,
        MouseWheelChange
    }
    public struct UDPMessage
    {

        public MessageType type;
        public int MouseWheelDelta;
        public Key key;
        public bool IsKeyDown;
        public string ErrorMessage;
        public System.Windows.Input.MouseButton MouseSide;
        public System.Windows.Point Mousepoint;
        public int Width;
        public int Height;
        public MouseButtonState mousestate;


    };
    public struct UDPframeMessage
    {
       public int totalChunks;
       public  int CurrentChunkNumber;
        public int ShouldResizeTo;

        [MarshalAs(UnmanagedType.ByValArray,SizeConst =64000)]
        public byte[] ChunkByteArray;
        public UDPframeMessage(int tot,int current, byte[] framebyte,int resize)
        {
            totalChunks = tot;
            ShouldResizeTo = resize;
            CurrentChunkNumber =current;
            ChunkByteArray = new byte[64000];
            framebyte.CopyTo(ChunkByteArray);
            
        }

    };
}
