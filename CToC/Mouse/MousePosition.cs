using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CToC.Mouse
{
    public class MousePosition
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator System.Windows.Point(POINT point)
            {
                return new System.Windows.Point(point.X, point.Y);
            }
        }




        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static System.Windows.Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }

    }
}
