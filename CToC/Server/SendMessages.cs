using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;

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
        public MouseButtonState mousestate;


    };
    public struct UDPframeMessage
    {
        public int totalChunks;
        public int FrameFingerPrint;
        public  int CurrentChunkNumber;
        public int TotalSizeOfTheFrame;
        public int ShouldResizeTo;

        [MarshalAs(UnmanagedType.ByValArray,SizeConst =64000)]
        public byte[] ChunkByteArray;
        public int FrameWidth;
        public int FrameHeight;
        
        public UDPframeMessage(int tot,int current, byte[] framebyte,int resize,int randomfingerprint,int totalsizeoftheframe, int W, int H)
        {

            FrameHeight = H;
            FrameHeight = W;
            FrameFingerPrint = randomfingerprint;
            TotalSizeOfTheFrame = totalsizeoftheframe;
            totalChunks = tot;
            ShouldResizeTo = resize;
            CurrentChunkNumber =current;
            ChunkByteArray = new byte[64000];
            framebyte.CopyTo(ChunkByteArray,0);
            
        }

    };
}
