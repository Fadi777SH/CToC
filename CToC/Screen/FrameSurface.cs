using ABI.System;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Graphics.DirectX.Direct3D11;

namespace CToC.Screen
{

    public  class FrameSurfaceModedClass : IDirect3DSurface
    {
      

        public Direct3DSurfaceDescription Description { get; init; }
        public void Dispose()
        {

        }
  
    }
}
