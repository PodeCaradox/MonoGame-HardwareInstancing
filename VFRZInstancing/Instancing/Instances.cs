using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VFRZInstancing.Instancing
{
    /// <summary>
    /// Instances for Drawing
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Instances
    {
        [FieldOffset(0)] public Vector3 World;
        [FieldOffset(12)] public ImageRenderData AtlasCoordinate;

      
    }
}
