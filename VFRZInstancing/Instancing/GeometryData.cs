using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        [FieldOffset(4)] public Color AtlasCoordinate;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement[]{
                new VertexElement(0, VertexElementFormat.Color,
                                     VertexElementUsage.Color, 2),
                new VertexElement(4, VertexElementFormat.Color,
                                      VertexElementUsage.Color, 3)}
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }
}
