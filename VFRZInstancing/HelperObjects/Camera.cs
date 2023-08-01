using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace VFRZInstancing.HelperObjects;

internal class Camera
{
    private static float MaxZoomOut = 0.125f;
    private float CameraSpeed = 0.5f;
    private Vector2 CameraPosition;
    private float PreviousMouseWheelValue;
    private float Zoom = 1;
    private Rectangle _bounds;
    private Rectangle VisibleArea;
    private Matrix _transform;
    private Matrix _projection;
    public Matrix Transform { get => _transform; }
    public Matrix Projection { get => _projection; }

    public Camera(Vector2 CameraPosition, Rectangle viewport)
    {
        this.CameraPosition = CameraPosition;
        this.PreviousMouseWheelValue = Mouse.GetState().ScrollWheelValue;
        this.Zoom = 1.0f;
        this._bounds = viewport;
        Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, -1, out _projection);
        UpdateMatrix();
    }

    internal void UpdateInput(float elaspedTime)
    {
        KeyboardState keyboard = Keyboard.GetState();
        bool changed = false;

        //Keyboard
        if (keyboard.IsKeyDown(Keys.W))
        {
            changed = true;
            CameraPosition.Y -= CameraSpeed * elaspedTime;
        }

        if (keyboard.IsKeyDown(Keys.S))
        {
            changed = true;
            CameraPosition.Y += CameraSpeed * elaspedTime;
        }

        if (keyboard.IsKeyDown(Keys.D))
        {
            changed = true;
            CameraPosition.X += CameraSpeed * elaspedTime;
        }

        if (keyboard.IsKeyDown(Keys.A))
        {
            changed = true;
            CameraPosition.X -= CameraSpeed * elaspedTime;
        }

        //Zoom
        float currentMouseWheelValue = Mouse.GetState().ScrollWheelValue;
        if (currentMouseWheelValue > PreviousMouseWheelValue)
        {
            changed = true;
            ZoomIn();
        }

        if (currentMouseWheelValue < PreviousMouseWheelValue)
        {
            changed = true;
            ZoomOut();
        }

        PreviousMouseWheelValue = currentMouseWheelValue;

        if (changed)
        {
            UpdateMatrix();
        }

    }

    private void UpdateMatrix()
    {
        var positionRounded = CameraPosition;
        positionRounded.Round();
        _transform = Matrix.CreateTranslation(new Vector3(-positionRounded.X, -positionRounded.Y, 0)) *
                Matrix.CreateScale(Zoom, Zoom, 1) *
                Matrix.CreateTranslation(new Vector3(_bounds.Width * 0.5f, _bounds.Height * 0.5f, 0));
        UpdateVisibleArea();
    }

    private void UpdateVisibleArea()
    {
        var inverseViewMatrix = Matrix.Invert(_transform);

        var tl = Vector2.Transform(Vector2.Zero, inverseViewMatrix);
        var tr = Vector2.Transform(new Vector2(_bounds.X, 0), inverseViewMatrix);
        var bl = Vector2.Transform(new Vector2(0, _bounds.Y), inverseViewMatrix);
        var br = Vector2.Transform(new Vector2(_bounds.Width, _bounds.Height), inverseViewMatrix);

        var min = new Vector2(
            MathHelper.Min(tl.X, MathHelper.Min(tr.X, MathHelper.Min(bl.X, br.X))),
            MathHelper.Min(tl.Y, MathHelper.Min(tr.Y, MathHelper.Min(bl.Y, br.Y))));
        var max = new Vector2(
            MathHelper.Max(tl.X, MathHelper.Max(tr.X, MathHelper.Max(bl.X, br.X))),
            MathHelper.Max(tl.Y, MathHelper.Max(tr.Y, MathHelper.Max(bl.Y, br.Y))));
        VisibleArea = new Rectangle((int)min.X, (int)min.Y, (int)Math.Round(max.X - min.X, 0), (int)Math.Round(max.Y - min.Y, 0));
        
    }


    internal Rectangle CalculateDrawingArea(in Vector2 tileSize)
    {
        var pos = ScreenPointToMapPoint(tileSize, new Point(VisibleArea.X + VisibleArea.Width, VisibleArea.Y));
        int widht = (int)(VisibleArea.Width / tileSize.X) * 2;// *2 because rows differnce
        int height = (int)(VisibleArea.Height / tileSize.Y);
        return new Rectangle(pos.X, pos.Y - 2, widht, height);
    }

    private Point ScreenPointToMapPoint(in Vector2 tileSize, in Point position)
    {
        float x = (position.Y / tileSize.Y) + (position.X / tileSize.X);
        float y = (position.Y / tileSize.Y) - (position.X / tileSize.X);
        Point point = new Point((int)x, (int)y);
        return point;
    }

    private void ZoomIn()
    {
        Zoom *= 2;
        AdjustZoom();
    }

    private void ZoomOut()
    {
        Zoom /= 2;
        AdjustZoom();
    }

    private void AdjustZoom()
    {
        if (Zoom < MaxZoomOut)
        {
            Zoom = MaxZoomOut;
        }
        if (Zoom > 6.4f)
        {
            Zoom = 6.4f;
        }
    }
}

