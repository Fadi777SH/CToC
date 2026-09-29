using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Windows.Graphics.DirectX.Direct3D11;

namespace CToC.Server
{
    public class UDPMesaages
    {
        enum MessageType
        {
            Keyboard,
            MouseChange,
            point,
            Fram,
            Error,
            MouseWheelChange
        }
        struct UDPMessage
        {
            public MessageType type;
            public int MouseWheelDelta;
            public Key key;
            public string ErrorMessage;
            public System.Windows.Input.MouseButton MouseSide;
            public System.Windows.Point point;
            public int Width;
            public int Height;
            public byte[] FramByte;
            public MouseButtonState mousestate;
            public IDirect3DSurface surface;

        };
    }
}
