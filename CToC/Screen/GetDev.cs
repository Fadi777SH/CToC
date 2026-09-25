using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace CToC.Screen
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
            IDirect3DDevice device = null;
            using (var dxgiDevice = sharpDxDevice.QueryInterface<SharpDX.DXGI.Device3>())
            {
                uint hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr pUnknown);
                if (hr == 0)
                {


                    device = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DDevice;
                    Marshal.Release(pUnknown);
                }
            }
            return device;
        }
        public static IDirect3DDevice CreateDirect3DDeviceFromSharpDXDevice2(SharpDX.Direct3D11.Device sharpDxDevice)
        {
            if (CreateDirect3D11DeviceFromDXGIDevice(sharpDxDevice.NativePointer, out var punk) != 0)
                return null;

            return WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(punk);
        }

    }
}
