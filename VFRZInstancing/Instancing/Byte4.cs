using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VFRZInstancing.Instancing
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ImageRenderData
    {
        [FieldOffset(0)] public byte CoordinateX;
        [FieldOffset(1)] public byte CoordinateY;
        [FieldOffset(2)] public ushort Index;

        public ImageRenderData(byte coordinateX, byte cordinateY, ushort index)
        {
            this.CoordinateX = coordinateX;
            this.CoordinateY = cordinateY;
            this.Index = index;
        }
    }
}
