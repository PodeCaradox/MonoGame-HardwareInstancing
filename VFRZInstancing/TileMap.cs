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

        private VertexDeclaration instanceVertexDeclaration;

        private VertexBuffer instanceBuffer;
        private VertexBuffer geometryBuffer;
        private IndexBuffer indexBuffer;

        #endregion

        #region Buffer Arrays

        private VertexBufferBinding[] bindings;
        private Instances[] instances;

        #endregion


        #region TileMapData

        private int sizeX;
        private int sizeZ;
        private Vector4[] _singleImageDimensions;
        private float _imageCount;

        #endregion

        #region CameraData

        private Vector2 _cameraPosition = new Vector2(0, 1500);
        private float scale = 1;
        private Matrix _transform;
        private Matrix _projection;

        #endregion

        #region Testing

        private bool changeTiles = false;
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
        }

        #endregion

        #region METHODS

   


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
                    this.instances[y * this.sizeX + x].World = new Vector3(pos.X, pos.Y - 84, 1 - pos.Y / (this.sizeZ * 16));
                    this.instances[y * this.sizeX + x].AtlasCoordinate = new Color((byte)randomTile.Next(0, 28), (byte)0, (byte)0, (byte)31);
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
            GeometryData[] _vertices = new GeometryData[4];


            #region filling vertices
            _vertices[0].World = new Color((byte)0, (byte)0, (byte)0, (byte)0);             //float3
            _vertices[0].AtlasCoordinate = new Color((byte)0, (byte)0, (byte)0, (byte)0);   //flaot2
            _vertices[1].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
            _vertices[1].AtlasCoordinate = new Color((byte)255, (byte)0, (byte)0, (byte)0);
            _vertices[2].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
            _vertices[2].AtlasCoordinate = new Color((byte)0, (byte)255, (byte)0, (byte)0);
            _vertices[3].World = new Color((byte)255, (byte)255, (byte)0, (byte)0);
            _vertices[3].AtlasCoordinate = new Color((byte)255, (byte)255, (byte)0, (byte)0);


            #endregion

            this.geometryBuffer = new VertexBuffer(this.GraphicsDevice, GeometryData.VertexDeclaration,
                                              4, BufferUsage.WriteOnly);
            this.geometryBuffer.SetData(_vertices);

            #region filling indices

            short[] _indices = new short[6];
            _indices[0] = 0; _indices[1] = 1; _indices[2] = 2;
            _indices[3] = 1; _indices[4] = 3; _indices[5] = 2;



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
            textures.Add(this.Game.Content.Load<Texture2D>("tiles"));

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
            _singleImageDimensions = new Vector4[textures.Count / 2 + textures.Count % 2];
            for (int i = 0; i < _singleImageDimensions.Length; i++)
            {
                //every time set two image dimensions (x,y first image)(z,w second image)
                _singleImageDimensions[i] = new Vector4(30,100,0,0);
            }

            #region if you have odd Number of Images
            if (textures.Count % 2 == 1)
            {
                _singleImageDimensions[textures.Count / 2] = new Vector4(30, 100, 0, 0);
            }
            #endregion

            #endregion

            #region Create Buffers

            this.GenerateCommonGeometry();


            this.instances = new Instances[this.InstanceCount];

            //for random testing of changes
            this.instances1 = new Instances[this.InstanceCount];
            this.instances2 = new Instances[this.InstanceCount];

            //the instances can change so we have a dynamic buffer
            this.instanceBuffer = new DynamicVertexBuffer(this.GraphicsDevice, Instances.VertexDeclaration, this.InstanceCount, BufferUsage.WriteOnly);
            this.InitializeInstances();

            // Creates the binding between the geometry and the instances.
            this.bindings = new VertexBufferBinding[2];
            this.bindings[0] = new VertexBufferBinding(geometryBuffer);
            this.bindings[1] = new VertexBufferBinding(instanceBuffer, 0, 1);

            #endregion

            #region Create Matrix

            _transform = Matrix.CreateTranslation(new Vector3(-_cameraPosition.X, -_cameraPosition.Y, 0)) *
                Matrix.CreateScale(scale, scale, 1) *
                Matrix.CreateTranslation(new Vector3(GraphicsDevice.Viewport.Width * 0.5f, GraphicsDevice.Viewport.Height * 0.5f, 0));

            Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, -1, out _projection);

            #endregion





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
            }else if (_ks.IsKeyDown(Keys.Down) || _ks.IsKeyDown(Keys.S))
            {
                _cameraPosition.Y += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                UpdateCamera();
            }
            if (_ks.IsKeyDown(Keys.Left) || _ks.IsKeyDown(Keys.A))
            {
                _cameraPosition.X -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                UpdateCamera();
            }else if (_ks.IsKeyDown(Keys.Right) || _ks.IsKeyDown(Keys.D))
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
                if(scale > 0.4f) scale -= 0.2f;

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

            previousMouseWheelValue = currentMouseWheelValue;
            before = _ks;
            if(ChangeArrayEachFrame)
                ChangeTilesInArray();

        }

        private void CreateNewArrays()
        {

            Random _randomHeight = new Random();

            int startPositionX = 0;
            int startPositionY = 0;
            // Set the position for each cube.
            for (Int32 y = 0; y < this.sizeZ; y++)
            {
                for (Int32 x = 0; x < this.sizeX; x++)
                {
                    var pos = new Vector2(x * 15 + startPositionX, x * 8 + startPositionY);
                    this.instances1[y * this.sizeX + x].World = new Vector3(pos.X, pos.Y - 84, 1 - pos.Y / (this.sizeZ * 16));
                    this.instances1[y * this.sizeX + x].AtlasCoordinate = new Color((byte)_randomHeight.Next(0, 28), (byte)0, (byte)0, (byte)31);
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
                    this.instances2[y * this.sizeX + x].World = new Vector3(pos.X, pos.Y - 84, 1 - pos.Y / (this.sizeZ * 16));
                    this.instances2[y * this.sizeX + x].AtlasCoordinate = new Color((byte)_randomHeight.Next(0, 28), (byte)0, (byte)0, (byte)31);
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
            this.effect.Parameters["ImageSizeArray"].SetValue(_singleImageDimensions);
            this.effect.Parameters["NumberOf2DTextures"].SetValue(_imageCount);
            this.effect.Parameters["SpriteTexture"].SetValue(this.texture);

            // Set the indices in the graphics device.
            this.GraphicsDevice.Indices = indexBuffer;

            // Apply the current technique pass.
            this.effect.CurrentTechnique.Passes[0].Apply();

            // Set the vertex buffer and draw the instanced primitives.
            this.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            this.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            this.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;


            this.GraphicsDevice.SamplerStates[0] = SS_PointBorder;
            this.GraphicsDevice.Textures[0] = texture;
            this.GraphicsDevice.SetVertexBuffers(bindings);
            this.GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, InstanceCount);

         
        }
       #endregion
    }
}
