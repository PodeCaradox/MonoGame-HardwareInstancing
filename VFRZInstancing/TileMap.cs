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


        private Texture2D texture;
        private Effect effect;

        private VertexDeclaration instanceVertexDeclaration;

        private VertexBuffer instanceBuffer;
        private VertexBuffer geometryBuffer;
        private IndexBuffer indexBuffer;

        private bool changeTiles = false;
        private VertexBufferBinding[] bindings;
        private TileInfo[] instances;

        //the 2 arrays that change every frame for drawing(1 or 2 will be drawn)
        private TileInfo[] instances1;
        private TileInfo[] instances2;
        struct TileInfo
        {
            public Vector3 World;
               //vector 3 so later can be Textures3D be used and z is which texture.
            public Vector3 AtlasCoordinate;
        };

        private Int32 sizeX;
        private Int32 sizeZ;
        private float scale = 1;
        public bool ChangeArrayEachFrame = false;
        private Vector2 positionRounded = new Vector2(0, 0);
        KeyboardState before = Keyboard.GetState();
        int previousMouseWheelValue = Mouse.GetState().ScrollWheelValue;
        Matrix transform;
        #endregion

        #region PROPERTIES

        public Int32 InstanceCount
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
        /// Initialize the VertexBuffer declaration for one cube instance.
        /// </summary>
        private void InitializeInstanceVertexBuffer()
        {
            VertexElement[] _instanceStreamElements = new VertexElement[2];

            // Position
            _instanceStreamElements[0] = new VertexElement(0, VertexElementFormat.Vector3,
                        VertexElementUsage.Position, 1);

            // Texture coordinate
            _instanceStreamElements[1] = new VertexElement(12, VertexElementFormat.Vector3,
                    VertexElementUsage.TextureCoordinate, 1);

            this.instanceVertexDeclaration = new VertexDeclaration(_instanceStreamElements);
        }

        /// <summary>
        /// Initialize all the cube instance. (sizeX * sizeZ)
        /// </summary>
        private void InitializeInstances()
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
                    this.instances[y * this.sizeX + x].World = new Vector3(pos.X, pos.Y - 84, 1 - pos.Y / (this.sizeZ * 16));
                    this.instances[y * this.sizeX + x].AtlasCoordinate = new Vector3(_randomHeight.Next(0, 28), 0,0);
                }

                startPositionY += 8;
                startPositionX -= 15;
            }

            CreateNewArrays();

            // Set the instace data to the instanceBuffer.

            this.instanceBuffer.SetData(this.instances);
        }

       
        /// <summary>
        /// Generate the common cube geometry. (Only one cube)
        /// </summary>
        private void GenerateCommonGeometry()
        {
            VertexPositionTexture[] _vertices = new VertexPositionTexture[4];
            int x = 0;
            int y = 0;
            int scaledummy = 2;//idk why but yeah needs to be scaled x2 or it draws wrong
            int width = 30* scaledummy;
            int height = 100 * scaledummy;
            var texelwidth = 1f;
            var texelheight = 1f;


            #region filling vertices
            _vertices[0].Position = new Vector3(x, y, 0);
            _vertices[0].TextureCoordinate = new Vector2(0, 0);
            _vertices[1].Position = new Vector3(x + width, y , 0);
            _vertices[1].TextureCoordinate = new Vector2(texelwidth, 0);
            _vertices[2].Position = new Vector3(x, y + height, 0);
            _vertices[2].TextureCoordinate = new Vector2(0, texelheight);
            _vertices[3].Position = new Vector3(x + width, y + height,0);
            _vertices[3].TextureCoordinate = new Vector2(texelwidth, texelheight);

          
            #endregion

            this.geometryBuffer = new VertexBuffer(this.GraphicsDevice, VertexPositionTexture.VertexDeclaration,
                                              4, BufferUsage.WriteOnly);
            this.geometryBuffer.SetData(_vertices);

            #region filling indices

            short[] _indices = new short[36];
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
            this.texture = this.Game.Content.Load<Texture2D>("tile_land_macros");

            this.InitializeInstanceVertexBuffer();
            this.GenerateCommonGeometry();
            this.instances = new TileInfo[this.InstanceCount];
            this.instances1 = new TileInfo[this.InstanceCount];
            this.instances2 = new TileInfo[this.InstanceCount];
            this.instanceBuffer = new DynamicVertexBuffer(this.GraphicsDevice, instanceVertexDeclaration, this.InstanceCount, BufferUsage.WriteOnly);
            this.InitializeInstances();

            // Creates the binding between the geometry and the instances.
            this.bindings = new VertexBufferBinding[2];
            this.bindings[0] = new VertexBufferBinding(geometryBuffer);
            this.bindings[1] = new VertexBufferBinding(instanceBuffer, 0, 1);

            transform = Matrix.CreateTranslation(new Vector3(-positionRounded.X, -positionRounded.Y, 0)) *
                  Matrix.CreateScale(scale, scale, 1) *
                  Matrix.CreateTranslation(new Vector3(GraphicsDevice.Viewport.Width * 0.5f, GraphicsDevice.Viewport.Height * 0.5f, 0));

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
                positionRounded.Y -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                UpdateCamera();
            }else if (_ks.IsKeyDown(Keys.Down) || _ks.IsKeyDown(Keys.S))
            {
                positionRounded.Y += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                UpdateCamera();
            }
            if (_ks.IsKeyDown(Keys.Left) || _ks.IsKeyDown(Keys.A))
            {
                positionRounded.X -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                UpdateCamera();
            }else if (_ks.IsKeyDown(Keys.Right) || _ks.IsKeyDown(Keys.D))
            {
                positionRounded.X += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                UpdateCamera();
            }

            if (currentMouseWheelValue > previousMouseWheelValue)
            {
                scale++;

                UpdateCamera();
            }
            else if (currentMouseWheelValue < previousMouseWheelValue)
            {
                if(scale > 1) scale--;

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
                    this.instances1[y * this.sizeX + x].AtlasCoordinate = new Vector3(_randomHeight.Next(0, 28), 0, 0);
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
                    this.instances2[y * this.sizeX + x].AtlasCoordinate = new Vector3(_randomHeight.Next(0, 28), 0, 0);
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

            
        }

        private void UpdateCamera()
        {
            transform = Matrix.CreateTranslation(new Vector3(-positionRounded.X, -positionRounded.Y, 0)) *
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

            this.instanceBuffer.SetData(this.instances);

            Matrix _projection;
            Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, -1, out _projection);
            this.effect.Parameters["WorldViewProjection"].SetValue(transform * _projection);

            //tiles in the atlas(tile sheet) Texture(not correct number just try an error some times so it looks correct on screen)
            this.effect.Parameters["NumberOfTextures"].SetValue(new Vector2(136.53333f, 40.96f));

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
            this.GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 12, InstanceCount);

         
        }
       #endregion
    }
}
