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
    public struct Instances : IVertexType
    {
        [FieldOffset(0)] public Vector3 World;
        //r = xAtlasCoordinate, g = yAtlasCoordinate, b = ImageIndex,a = shadowColor + ImageIndex
        [FieldOffset(12)] public Color AtlasCoordinate;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement[]{
                new VertexElement(0, VertexElementFormat.Vector3,
                                     VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color,
                                      VertexElementUsage.Color, 2)}
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }
}
