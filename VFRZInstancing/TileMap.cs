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
using System.Diagnostics;
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
        private Texture3D _texture;
        private Effect _effect;
        Random _randomTile = new Random();

        #region Buffers

        private StructuredBuffer _allTiles;
        private StructuredBuffer _visibleTiles;
        private VertexBuffer _geometryBuffer;
        private Instances[] _instances;
        private Point _camera_point;
        const int _computeGroupSize = 32;
        #endregion

        #region TileMapData

        private Point _size;
        private Vector2 _tileSize = new Vector2(32, 16);
        private Vector2 _tileSizehalf = new Vector2(16, 8);
        private Vector2[] _singleImageDimensions;
        private float _imageCount;

        #endregion


        #region Testing

        private bool _imageWidth32Pixel = false;
        private bool _changeArrayEachFrame = false;
        private int visibleInstances = 0;
        private bool _debug_shader;

        #endregion


        #endregion

        #region PROPERTIES


        private RasterizerState _raster = new RasterizerState();
        private KeyboardState before;


        public int InstanceCount
        {
            get { return this._size.X * this._size.Y; }
        }

        public bool ImageWidth32Pixel { get => _imageWidth32Pixel; set => _imageWidth32Pixel = value; }
        public bool ChangeArrayEachFrame { get => _changeArrayEachFrame; set => _changeArrayEachFrame = value; }

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Creates a new CubeMap.
        /// </summary>
        /// <param name="game">Parent game instance.</param>
        /// <param name="sizeX">Map size on X.</param>
        /// <param name="sizeZ">Map size on Z.</param>
        public TileMap(Game game, int sizeX, int sizeZ, int width, int height) : base(game)
        {
            _size = new Point(sizeX, sizeZ);
            _raster.MultiSampleAntiAlias = false;
            _raster.ScissorTestEnable = true;
            _raster.FillMode = FillMode.Solid;
            _raster.CullMode = CullMode.CullCounterClockwiseFace;
            _raster.DepthClipEnable = true;
            var bounds = new Rectangle(0, 0, width, height);

            _camera = new Camera(new Vector2(0, 0), bounds);
        }

        #endregion

        #region METHODS


        /// <summary>
        /// Initialize all the cube instance. (sizeX * sizeZ)
        /// </summary>
        private void InitializeInstances()
        {

            int size = _size.X * _size.Y;
            // Set the position for each tile.
            for (int y = 0; y < _size.Y; y++)
            {
                for (int x = 0; x < _size.X; x++)
                {
                    var pos = MapToScreenPos(new Point(x, y));
                    _instances[y * _size.X + x].World = new Vector3(pos.X, pos.Y, 1 - (float)(y + x) / size);
                    _instances[y * _size.X + x].AtlasCoordinate = new ImageRenderData((byte)_randomTile.Next(0, 28), (byte)0, 0);
                }
            }

            _allTiles.SetData(this._instances);
        }

        private void ChangeIds()
        {
            for (int i = 0; i < this._instances.Length; i++)
            {
                _instances[i].AtlasCoordinate = new ImageRenderData((byte)_randomTile.Next(0, 28), (byte)0, 0);
            }
            _allTiles.SetData(this._instances);
        }


        /// <summary>
        /// Generate the common Rectangle geometry. (Only one Rectangle)
        /// </summary>
        private void GenerateCommonGeometry()
        {
            int size = _size.X * _size.Y;
            GeometryData[] _vertices = new GeometryData[6 * size];


            #region filling vertices
            for (int i = 0; i < size; i++)
            {

                _vertices[i * 6 + 0].World = new Color((byte)0, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 1].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 2].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 3].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 4].World = new Color((byte)255, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 5].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);

            }
            #endregion

            _geometryBuffer = new VertexBuffer(this.GraphicsDevice, typeof(GeometryData), _vertices.Length, BufferUsage.WriteOnly);
            _geometryBuffer.SetData(_vertices);

        }

        #endregion

        #region OVERRIDE METHODS

        /// <summary>
        /// Load the CubeMap effect and texture.
        /// </summary>
        protected override void LoadContent()
        {
            _effect = this.Game.Content.Load<Effect>("instance_effect");
            var textures = new List<Texture2D>();
            //Image need to be 2048 x 2048
            textures.Add(this.Game.Content.Load<Texture2D>("tiles30x64"));
            textures.Add(this.Game.Content.Load<Texture2D>("tiles32x64"));

            #region Init3DTexture
            //max 2048 otherwise it draws black(hardware limitations)
            //also depth max is 2048 images, so you can stack 2048 images
            _texture = new Texture3D(graphicsDevice: GraphicsDevice, width: 2048, height: 2048, depth: textures.Count, mipMap: false, format: SurfaceFormat.Color);
            int textureSizeInPixels = this._texture.Width * this._texture.Height;
            var color = new Color[textureSizeInPixels * textures.Count];

            int counter = 0;

            foreach (var texture in textures)
            {
                texture.GetData(color, textureSizeInPixels * counter, textureSizeInPixels);
                counter++;
            }
            _imageCount = textures.Count;
            _texture.SetData(color);

            //here somehow load how big a single Image is inside a Texture2D
            _singleImageDimensions = new Vector2[textures.Count];
            _singleImageDimensions[0] = new Vector2(30, 64);
            _singleImageDimensions[1] = new Vector2(32, 64);




            #endregion

            #region Create Buffers

            GenerateCommonGeometry();


            _instances = new Instances[this.InstanceCount];

            //the instances can change so we have a StructuredBuffer buffer
            _allTiles = new StructuredBuffer(GraphicsDevice, typeof(Instances), InstanceCount, BufferUsage.WriteOnly, ShaderAccess.Read);
            _visibleTiles = new StructuredBuffer(GraphicsDevice, typeof(Instances), InstanceCount, BufferUsage.WriteOnly, ShaderAccess.ReadWrite);
            InitializeInstances();
            _allTiles.SetData(_instances);


            #endregion


            _effect.Parameters["ImageSizeArray"].SetValue(_singleImageDimensions);
            _effect.Parameters["NumberOf2DTextures"].SetValue(_imageCount);
            _effect.Parameters["SpriteTexture"].SetValue(_texture);

            base.LoadContent();

        }

        /// <summary>
        /// Update the CubeMap logic.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
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
                _imageWidth32Pixel = !_imageWidth32Pixel;
                for (int i = 0; i < _instances.Length; i++)
                {
                    _instances[i].AtlasCoordinate.Index = (byte)((_imageWidth32Pixel) ? 1 : 0);
                }
            }
            else if (ks.IsKeyDown(Keys.F12) && before.IsKeyUp(Keys.F12))
            {
                _debug_shader = !_debug_shader;
            }
            before = ks;
            if (ChangeArrayEachFrame)
                ChangeIds();

        }

        private Vector2 MapToScreenPos(Point position)
        {
            float postionXCentered = (_tileSizehalf.X * position.X - _tileSizehalf.X * position.Y) - 1;
            float postionYCentered = (_tileSizehalf.Y * position.X + _tileSizehalf.Y * position.Y) + _tileSizehalf.Y * 2;
            var pos = new Vector2(postionXCentered, postionYCentered);
            return pos;
        }


        /// <summary>
        /// Draw the cube map using one single vertexbuffer.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            // Set the effect technique and parameters
            _effect.CurrentTechnique = _effect.Techniques["Instancing"];

            ComputeCulling();

            if (visibleInstances <= 0) return;
            _effect.Parameters["WorldViewProjection"].SetValue(_camera.Transform * _camera.Projection);
            _effect.Parameters["TileBuffer"].SetValue(_visibleTiles);
            _effect.CurrentTechnique.Passes[0].Apply();

            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.RasterizerState = _raster;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SS_PointBorder;

            GraphicsDevice.SetVertexBuffer(_geometryBuffer);

            //dont use DrawInsttanced its to slow for Sprintes insancing see: https://www.slideshare.net/DevCentralAMD/vertex-shader-tricks-bill-bilodeau
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 2 * visibleInstances);

        }

        private void ComputeCulling()
        {
            var drawingArea = _camera.CalculateDrawingArea(_tileSize);
            //calculate how many tiles will be drawn.;
            int tileCountX = drawingArea.Height + (_computeGroupSize - drawingArea.Height % _computeGroupSize);
            int tileCountY = drawingArea.Width + (_computeGroupSize - drawingArea.Width % _computeGroupSize);
            _effect.Parameters["StartPosX"].SetValue(drawingArea.X);
            _effect.Parameters["StartPosY"].SetValue(drawingArea.Y);
            _effect.Parameters["MapSizeX"].SetValue(_size.X);
            _effect.Parameters["MapSizeY"].SetValue(_size.Y);
            _effect.Parameters["AllTiles"].SetValue(_allTiles);
            _effect.Parameters["VisibleTiles"].SetValue(_visibleTiles);
            _effect.Parameters["Columns"].SetValue(tileCountX);
            _effect.Parameters["Rows"].SetValue(tileCountY / 2);

            _effect.CurrentTechnique.Passes[0].ApplyCompute();
            GraphicsDevice.DispatchCompute(tileCountX / _computeGroupSize, tileCountY / _computeGroupSize, 1);

            _camera_point = new Point(drawingArea.X + drawingArea.Width, drawingArea.Y);
            visibleInstances = CalcVisibleTiles(drawingArea.X, drawingArea.Y, tileCountX, tileCountY, _size.X, _size.Y);


            if(_debug_shader)
                ShaderInCSharpImpl.testc(drawingArea.X, drawingArea.Y, tileCountX, tileCountY, _size.X, _size.Y);
        }
        #endregion


        private int CalcVisibleTiles(int StartPosX, int StartPosY, int Columns, int Rows, int MapSizeX, int MapSizeY)
        {
            int visibleIndex = 0;
            Point start = new Point(StartPosX, StartPosY);
            int outside = 1;
            for (int i = 0; i < Columns; i++)
            {
                start.X++;
                start.Y++;
                if (start.X >= 0 && start.Y >= 0 && start.Y < MapSizeY && start.X < MapSizeX)
                {
                    outside = 0;
                    break;
                }
            }

            //calculate the starting point when outside of map.
            if (outside == 1)
            {
                //above map
                if (StartPosX + StartPosY < MapSizeX)
                {
                    Point left = new Point(StartPosX - Rows, StartPosY + Rows);
                    left.X += left.Y;
                    left.Y -= left.Y;
                    start = new Point(MapSizeX - 1, 0);
                    int difference = start.X - left.X;
                    difference += difference % 2;
                    difference /= 2;
                    start.X -= difference;
                    start.Y -= difference;
                }
                else // underneath map
                {
                    int to_the_left = StartPosX - MapSizeX;
                    start = new Point(StartPosX - to_the_left, StartPosY + to_the_left);
                }

            }//inside the map
            else
            {
                start = new Point(StartPosX, StartPosY);
            }


            //We will save this in a array and send to compute shader so we dont need to loop there which is bad.
            for (int i = 0; i < Rows; i++)
            {
                int current_row = i / 2;
                Point pos = new Point(start.X - i % 2 - current_row, start.Y + current_row);
                int vertical_tiles = Columns;
                if (pos.X < 0 || pos.Y < 0)
                {
                    if (pos.X < pos.Y)
                    {
                        vertical_tiles += pos.X;
                        pos.Y -= pos.X;
                        pos.X = 0;
                    }
                    else
                    {
                        vertical_tiles += pos.Y;
                        pos.X -= pos.Y;
                        pos.Y = 0;
                    }
                }

                pos.X += vertical_tiles;
                pos.Y += vertical_tiles;

                if (pos.X >= MapSizeX)
                {
                    int tiles_overflow = pos.X - MapSizeX;
                    vertical_tiles -= tiles_overflow;
                    pos.Y -= tiles_overflow;

                }

                if (pos.Y >= MapSizeY)
                {
                    int tiles_overflow = pos.Y - MapSizeY;
                    vertical_tiles -= tiles_overflow;
                }

                if (vertical_tiles < 0) break;
                visibleIndex += vertical_tiles;

            }
           if (_debug_shader) Debug.WriteLine(visibleIndex + " In For loop calculated");
            return visibleIndex;
        }



    }
}
