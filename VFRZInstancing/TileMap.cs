using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VFRZInstancing.Instancing;
using VFRZInstancing.HelperObjects;
/*--------------------------------------------------------
 * CubeMap.cs
 * 
 * Version: 1.0
 * Author: Filipe
 * Created: 20/03/2016 19:16:20
 * 
 * Notes:
 * Code mostly based on: http://stackoverflow.com/questions/9929103/need-help-using-instancing-in-xna-4-0
 * for testing purpose.
 * -------------------------------------------------------*/

namespace VFRZInstancing
{
    public class TileMap : DrawableGameComponent
    {
        #region FIELDS

        public static SamplerState SS_PointBorder = new SamplerState() { Filter = TextureFilter.Point, AddressU = TextureAddressMode.Clamp, AddressV = TextureAddressMode.Clamp };
        private Camera _camera;
        private Texture3D texture;
        private Effect effect;
        Random _randomTile = new Random();
        #region Buffers

        private StructuredBuffer instanceBuffer;
        private VertexBuffer geometryBuffer;
        private Instances[] instances;
        private int instancesVertexBuffer;

        #endregion

        #region TileMapData

        private Point _size;
        private Point _tileSize = new Point(32,16);
        private Point _tileSizehalf = new Point(16, 8);
        private Vector2[] _singleImageDimensions;
        private float _imageCount;

        #endregion


        #region Testing

        private bool imageWidth32Pixel = false;
        public bool ChangeArrayEachFrame = false;

        #endregion


        #endregion

        #region PROPERTIES

        public int InstanceCount
        {
            get { return this._size.X * this._size.Y; }
        }

        public bool ImageWidth32Pixel { get => imageWidth32Pixel; set => imageWidth32Pixel = value; }

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Creates a new CubeMap.
        /// </summary>
        /// <param name="game">Parent game instance.</param>
        /// <param name="sizeX">Map size on X.</param>
        /// <param name="sizeZ">Map size on Z.</param>
        public TileMap(Game game, int sizeX, int sizeZ) : base(game)
        {
            _size = new Point(sizeX, sizeZ);
            _raster.MultiSampleAntiAlias = false;
            _raster.ScissorTestEnable = true;
            _raster.FillMode = FillMode.Solid;
            _raster.CullMode = CullMode.CullCounterClockwiseFace;
            _raster.DepthClipEnable = true;
            this.instancesVertexBuffer = sizeX * sizeZ;
            var bounds = new Rectangle(0, 0, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);

            _camera = new Camera(new Vector2(0, 1000), bounds);
        }

        #endregion

        #region METHODS



        private RasterizerState _raster = new RasterizerState();
        private KeyboardState before;

        /// <summary>
        /// Initialize all the cube instance. (sizeX * sizeZ)
        /// </summary>
        private void InitializeInstances()
        {
            

            // Set the position for each tile.
            for (int y = 0; y < this._size.Y; y++)
            {
                for (int x = 0; x < this._size.X; x++)
                {
                    var pos = MapToScreenPos(new Point(x, y));
                    this.instances[y * this._size.X + x].World = new Vector3(pos.X, pos.Y, 1);
                    this.instances[y * this._size.X + x].AtlasCoordinate = new ImageRenderData((byte)_randomTile.Next(0, 28), (byte)0, 0);
                }
            }

            this.instanceBuffer.SetData(this.instances);
        }

        private void ChangeIds()
        {
            for (int i = 0; i < this.instances.Length; i++)
            {
                this.instances[i].AtlasCoordinate = new ImageRenderData((byte)_randomTile.Next(0, 28), (byte)0, 0);
            }
            this.instanceBuffer.SetData(this.instances);
        }


        /// <summary>
        /// Generate the common Rectangle geometry. (Only one Rectangle)
        /// </summary>
        private void GenerateCommonGeometry()
        {
            GeometryData[] _vertices = new GeometryData[6 * this.instancesVertexBuffer];


            #region filling vertices
            for (int i = 0; i < this.instancesVertexBuffer; i++)
            {

                _vertices[i * 6 + 0].World = new Color((byte)0, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 1].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 2].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 3].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 4].World = new Color((byte)255, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 5].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);

            }
            #endregion

            this.geometryBuffer = new VertexBuffer(this.GraphicsDevice, typeof(GeometryData), _vertices.Length, BufferUsage.WriteOnly);
            this.geometryBuffer.SetData(_vertices);

        }

        #endregion

        #region OVERRIDE METHODS

        /// <summary>
        /// Load the CubeMap effect and texture.
        /// </summary>
        protected override void LoadContent()
        {
            this.effect = this.Game.Content.Load<Effect>("instance_effect");
            var textures = new List<Texture2D>();
            //Image need to be 2048 x 2048
            textures.Add(this.Game.Content.Load<Texture2D>("tiles30x64"));
            textures.Add(this.Game.Content.Load<Texture2D>("tiles32x64"));

            #region Init3DTexture
            //max 2048 otherwise it draws black(hardware limitations)
            //also depth max is 2048 images, so you can stack 2048 images
            this.texture = new Texture3D(graphicsDevice: GraphicsDevice, width: 2048, height: 2048, depth: textures.Count, mipMap: false, format: SurfaceFormat.Color);
            int textureSizeInPixels = this.texture.Width * this.texture.Height;
            var color = new Color[textureSizeInPixels * textures.Count];

            int counter = 0;

            foreach (var texture in textures)
            {
                texture.GetData(color, textureSizeInPixels * counter, textureSizeInPixels);
                counter++;
            }
            _imageCount = textures.Count;
            this.texture.SetData(color);

            //here somehow load how big a single Image is inside a Texture2D
            _singleImageDimensions = new Vector2[textures.Count];
            _singleImageDimensions[0] = new Vector2(30, 64);
            _singleImageDimensions[1] = new Vector2(32, 64);




            #endregion

            #region Create Buffers

            this.GenerateCommonGeometry();


            this.instances = new Instances[this.InstanceCount];

            //the instances can change so we have a StructuredBuffer buffer
            this.instanceBuffer = new StructuredBuffer(this.GraphicsDevice, typeof(Instances), this.InstanceCount, BufferUsage.WriteOnly, ShaderAccess.Read);
            this.InitializeInstances();
            instanceBuffer.SetData(this.instances);


            #endregion


            this.effect.Parameters["ImageSizeArray"].SetValue(_singleImageDimensions);
            this.effect.Parameters["NumberOf2DTextures"].SetValue(_imageCount);
            this.effect.Parameters["SpriteTexture"].SetValue(this.texture);

            base.LoadContent();

        }

        /// <summary>
        /// Update the CubeMap logic.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {

            _camera.UpdateInput((float)gameTime.ElapsedGameTime.TotalMilliseconds);

            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.F1) && before.IsKeyUp(Keys.F1))
            {
                ChangeIds();
            }
            else if (ks.IsKeyDown(Keys.F2) && before.IsKeyUp(Keys.F2))
            {
                ChangeArrayEachFrame = !ChangeArrayEachFrame;
            }
            if (ks.IsKeyDown(Keys.F3) && before.IsKeyUp(Keys.F3))
            {
                imageWidth32Pixel = !imageWidth32Pixel;
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i].AtlasCoordinate.Index = (byte)((imageWidth32Pixel) ? 1 : 0);
                }
            }
            before = ks;
            if (ChangeArrayEachFrame)
                ChangeIds();

        }

        private Vector2 MapToScreenPos(Point position)
        {
            int startPositionX = (int)(_tileSizehalf.X * position.X - _tileSizehalf.X * position.Y - _tileSizehalf.X);
            int startPositionY = (int)(_tileSizehalf.Y * position.X + _tileSizehalf.Y * position.Y);
            var pos = new Vector2(startPositionX, startPositionY);

            return pos;
        }


        /// <summary>
        /// Draw the cube map using one single vertexbuffer.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            // Set the effect technique and parameters
            this.effect.CurrentTechnique = effect.Techniques["Instancing"];



            this.effect.Parameters["WorldViewProjection"].SetValue(_camera.Transform * _camera.Projection);
            this.effect.Parameters["TileBuffer"].SetValue(instanceBuffer);

            // Set the indices in the graphics device.

            // Apply the current technique pass.
            this.effect.CurrentTechnique.Passes[0].Apply();

            // Set the vertex buffer and draw the instanced primitives.
            this.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            this.GraphicsDevice.RasterizerState = _raster;
            this.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            //this.instanceBuffer.SetData(this.instances);
            this.GraphicsDevice.SamplerStates[0] = SS_PointBorder;

            this.GraphicsDevice.SetVertexBuffer(geometryBuffer);
            //dont use DrawInsttanced its to slow for Sprintes insancing see: https://www.slideshare.net/DevCentralAMD/vertex-shader-tricks-bill-bilodeau
            this.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 2 * this.instancesVertexBuffer);
         
        }
        #endregion
    }
}
