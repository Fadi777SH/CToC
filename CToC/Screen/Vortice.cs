using System;
using System.Collections.Generic;
using System.Text;
using Vortice;
using Vortice.Direct3D;
using Vortice.Direct3D11;
namespace SuperMassiveRemote.Screen
{
    public class CVortice
    {
        public void ss()
        {
            var dev = D3D11.D3D11CreateDevice(DriverType.Hardware, DeviceCreationFlags.None, FeatureLevel.Level_9_1);
            var con = dev.CreateDeferredContext();

        }

    }
}
