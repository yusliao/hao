using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    /// <summary>
    /// WINDOWS API
    /// </summary>
    public class NativeMethod
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hwdn, Int32 wMsg, Int32 mParam, Int32 lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool ReleaseCapture();

    }
}
