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


        private Texture3D texture;
        private Effect effect;

        #region Buffers

        private StructuredBuffer instanceBuffer;
        private VertexBuffer geometryBuffer;
        private IndexBuffer indexBuffer;

        #endregion

        #region Buffer Arrays

        private Instances[] instances;
        private int instancesVertexBuffer;
        #endregion


        #region TileMapData

        private int sizeX;
        private int sizeZ;
        private Vector2[] _singleImageDimensions;
        private float _imageCount;

        #endregion

        #region CameraData

        private Vector2 _cameraPosition = new Vector2(0, 1000);
        private float scale = 1;
        private Matrix _transform;
        private Matrix _projection;

        #endregion

        #region Testing

        private bool changeTiles = false;
        private bool imageWidth32Pixel = false;
        //the 2 arrays that change every frame for drawing(1 or 2 will be drawn)
        private Instances[] instances1;
        private Instances[] instances2;
        public bool ChangeArrayEachFrame = false;
        private KeyboardState before = Keyboard.GetState();
        private int previousMouseWheelValue = Mouse.GetState().ScrollWheelValue;

        #endregion






        #endregion

        #region PROPERTIES

        public int InstanceCount
        {
            get { return this.sizeX * this.sizeZ; }
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
            this.sizeX = sizeX;
            this.sizeZ = sizeZ;
            _raster.MultiSampleAntiAlias = false;
            _raster.ScissorTestEnable = false;
            _raster.FillMode = FillMode.Solid;
            _raster.CullMode = CullMode.None;
            _raster.DepthClipEnable = false;
            this.instancesVertexBuffer = sizeX * sizeZ;
        }

        #endregion

        #region METHODS



        private RasterizerState _raster = new RasterizerState();
        /// <summary>
        /// Initialize all the cube instance. (sizeX * sizeZ)
        /// </summary>
        private void InitializeInstances()
        {
            Random randomTile = new Random();

            int startPositionX = 0;
            int startPositionY = 0;
            // Set the position for each cube.
            for (Int32 y = 0; y < this.sizeZ; y++)
            {
                for (Int32 x = 0; x < this.sizeX; x++)
                {
                    var pos = new Vector2(x * 16 + startPositionX, x * 8 + startPositionY);
                    this.instances[y * this.sizeX + x].World = new Vector3(pos.X, pos.Y, 1 - pos.Y / (this.sizeZ * 16));
                    this.instances[y * this.sizeX + x].AtlasCoordinate = new ImageRenderData((byte)randomTile.Next(0, 28), (byte)0, 0); 
                }

                //isometric offset
                startPositionY += 8;
                startPositionX -= 16;
            }

            CreateNewArrays();

            // Set the instace data to the instanceBuffer.

            this.instanceBuffer.SetData(this.instances);
        }


        /// <summary>
        /// Generate the common Rectangle geometry. (Only one Rectangle)
        /// </summary>
        private void GenerateCommonGeometry()
        {
            GeometryData[] _vertices = new GeometryData[4 * this.instancesVertexBuffer];


            #region filling vertices
            for (int i = 0; i < this.instancesVertexBuffer; i++)
            {

                _vertices[i * 4 + 0].World = new Color((byte)0, (byte)0, (byte)0, (byte)0);
                _vertices[i * 4 + 1].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 4 + 2].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
                _vertices[i * 4 + 3].World = new Color((byte)255, (byte)255, (byte)0, (byte)0);
              
            }



            #endregion

            this.geometryBuffer = new VertexBuffer(this.GraphicsDevice, typeof(GeometryData), _vertices.Length, BufferUsage.WriteOnly);
            this.geometryBuffer.SetData(_vertices);

            #region filling indices
            short[] _indices = new short[6 * this.instancesVertexBuffer];
            for (int i = 0; i < this.instancesVertexBuffer; i++)
            {

                _indices[i * 6 + 0] = (short)(0 + i * 4); _indices[i * 6 + 1] = (short)(1 + i * 4); _indices[i * 6 + 2] = (short)(2 + i * 4);
                _indices[i * 6 + 3] = (short)(1 + i * 4); _indices[i * 6 + 4] = (short)(3 + i * 4); _indices[i * 6 + 5] = (short)(2 + i * 4);
            }


            #endregion

            this.indexBuffer = new IndexBuffer(this.GraphicsDevice, typeof(short), _indices.Length, BufferUsage.WriteOnly);
            this.indexBuffer.SetData(_indices);
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
            //max 2048 otherwise it draws black maybe opengl limitation
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

            //for random testing of changes
            this.instances1 = new Instances[this.InstanceCount];
            this.instances2 = new Instances[this.InstanceCount];

            //the instances can change so we have a dynamic buffer
            this.instanceBuffer = new StructuredBuffer(this.GraphicsDevice, typeof(Instances), this.InstanceCount, BufferUsage.WriteOnly, ShaderAccess.Read);
            this.InitializeInstances();
            instanceBuffer.SetData(this.instances);


            #endregion

            #region Create Matrix

            _transform = Matrix.CreateTranslation(new Vector3(-_cameraPosition.X, -_cameraPosition.Y, 0)) *
                Matrix.CreateScale(scale, scale, 1) *
                Matrix.CreateTranslation(new Vector3(GraphicsDevice.Viewport.Width * 0.5f, GraphicsDevice.Viewport.Height * 0.5f, 0));

            Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, -1, out _projection);

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

            //InitializeInstancesTest();
            KeyboardState _ks = Keyboard.GetState();

            var currentMouseWheelValue = Mouse.GetState().ScrollWheelValue;
            if (_ks.IsKeyDown(Keys.Up) || _ks.IsKeyDown(Keys.W))
            {
                _cameraPosition.Y -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                UpdateCamera();
            }
            else if (_ks.IsKeyDown(Keys.Down) || _ks.IsKeyDown(Keys.S))
            {
                _cameraPosition.Y += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                UpdateCamera();
            }
            if (_ks.IsKeyDown(Keys.Left) || _ks.IsKeyDown(Keys.A))
            {
                _cameraPosition.X -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                UpdateCamera();
            }
            else if (_ks.IsKeyDown(Keys.Right) || _ks.IsKeyDown(Keys.D))
            {
                _cameraPosition.X += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                UpdateCamera();
            }

            if (currentMouseWheelValue > previousMouseWheelValue)
            {
                scale += 0.2f;

                UpdateCamera();
            }
            else if (currentMouseWheelValue < previousMouseWheelValue)
            {
                if (scale > 0.4f) scale -= 0.2f;

                UpdateCamera();
            }

            if (_ks.IsKeyDown(Keys.F1) && before.IsKeyUp(Keys.F1))
            {
                CreateNewArrays();
                ChangeTilesInArray();
            }
            else if (_ks.IsKeyDown(Keys.F2) && before.IsKeyUp(Keys.F2))
            {
                ChangeArrayEachFrame = !ChangeArrayEachFrame;
            }
            if (_ks.IsKeyDown(Keys.F3) && before.IsKeyUp(Keys.F3))
            {
                imageWidth32Pixel = !imageWidth32Pixel;
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i].AtlasCoordinate.Index = (byte)((imageWidth32Pixel) ? 1 : 0);
                }
            }

            previousMouseWheelValue = currentMouseWheelValue;
            before = _ks;
            if (ChangeArrayEachFrame)
                ChangeTilesInArray();

        }

        private void CreateNewArrays()
        {

            Random randomTile = new Random();

            int startPositionX = 0;
            int startPositionY = 0;
            // Set the position for each cube.
            for (Int32 y = 0; y < this.sizeZ; y++)
            {
                for (Int32 x = 0; x < this.sizeX; x++)
                {
                    var pos = new Vector2(x * 15 + startPositionX, x * 8 + startPositionY);
                    this.instances1[y * this.sizeX + x].World = new Vector3(pos.X, pos.Y, 1 - pos.Y / (this.sizeZ * 16));
                    this.instances1[y * this.sizeX + x].AtlasCoordinate = new ImageRenderData((byte)randomTile.Next(0, 28), 0, 0);
                }

                startPositionY += 8;
                startPositionX -= 15;
            }

            startPositionX = 0;
            startPositionY = 0;
            // Set the position for each cube.
            for (Int32 y = 0; y < this.sizeZ; y++)
            {
                for (Int32 x = 0; x < this.sizeX; x++)
                {
                    var pos = new Vector2(x * 15 + startPositionX, x * 8 + startPositionY);
                    this.instances2[y * this.sizeX + x].World = new Vector3(pos.X, pos.Y, 1 - pos.Y / (this.sizeZ * 16));
                    this.instances2[y * this.sizeX + x].AtlasCoordinate = new ImageRenderData((byte)randomTile.Next(0, 28), 0, 0);
                }

                startPositionY += 8;
                startPositionX -= 15;
            }
        }

        private void ChangeTilesInArray()
        {
            changeTiles = !changeTiles;
            if (changeTiles)
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i] = instances1[i];
                }
            }
            else
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i] = instances2[i];
                }
            }

            this.instanceBuffer.SetData(this.instances);
        }

        private void UpdateCamera()
        {
            _transform = Matrix.CreateTranslation(new Vector3(-_cameraPosition.X, -_cameraPosition.Y, 0)) *
                  Matrix.CreateScale(scale, scale, 1) *
                  Matrix.CreateTranslation(new Vector3(GraphicsDevice.Viewport.Width * 0.5f, GraphicsDevice.Viewport.Height * 0.5f, 0));
        }

        /// <summary>
        /// Draw the cube map using one single vertexbuffer.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            // Set the effect technique and parameters
            this.effect.CurrentTechnique = effect.Techniques["Instancing"];



            this.effect.Parameters["WorldViewProjection"].SetValue(_transform * _projection);
            this.effect.Parameters["TileBuffer"].SetValue(instanceBuffer);

            // Set the indices in the graphics device.
            this.GraphicsDevice.Indices = indexBuffer;

            // Apply the current technique pass.
            this.effect.CurrentTechnique.Passes[0].Apply();

            // Set the vertex buffer and draw the instanced primitives.
            this.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            this.GraphicsDevice.RasterizerState = _raster;
            this.GraphicsDevice.DepthStencilState = DepthStencilState.Default;


            this.GraphicsDevice.SamplerStates[0] = SS_PointBorder;

            this.GraphicsDevice.SetVertexBuffer(geometryBuffer);
            this.GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2 * this.instancesVertexBuffer, 1);


        }
        #endregion
    }
}
