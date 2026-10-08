using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;

namespace SMR.Screen
{
    static class CaptureInterop
    {
        [DllImport(
                "d3d11.dll",
                EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
                SetLastError = true,
                CharSet = CharSet.Unicode,
                ExactSpelling = true,
                CallingConvention = CallingConvention.StdCall
                )]
        public static extern UInt32 CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
        public static IDirect3DDevice CreateDirect3DDeviceFromSharpDXDevice(SharpDX.Direct3D11.Device sharpDxDevice)
        {
            if (CreateDirect3D11DeviceFromDXGIDevice(sharpDxDevice.NativePointer, out var punk) != 0)
                return null;

            return WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(punk);
        }

    }
}
