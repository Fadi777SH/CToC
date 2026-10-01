using CToC.Screen;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace CToC
{
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
        public byte[] FramByte;
        public MouseButtonState mousestate;


    };

}
