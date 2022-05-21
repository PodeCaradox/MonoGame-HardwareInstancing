using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VFRZInstancing.Instancing
{
    [StructLayout(LayoutKind.Explicit)]
    public struct GeometryData : IVertexType
    {
        [FieldOffset(0)] public Color World;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (

            new VertexElement[]{
                new VertexElement(0, VertexElementFormat.Color,
                                     VertexElementUsage.Color, 0) }
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }
}
