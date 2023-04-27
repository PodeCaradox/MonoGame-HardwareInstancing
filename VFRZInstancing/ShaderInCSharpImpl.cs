using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VFRZInstancing
{
    internal static class ShaderInCSharpImpl
    {
        internal static void testc(int StartPosX, int StartPosY, int Columns, int Rows, int MapSizeX, int MapSizeY)
        {
            int visibleIndex = 0;
            for (int globalIDY = 0; globalIDY < Rows; globalIDY++)
            {
                for (int globalIDX = 0; globalIDX < Columns; globalIDX++)
                {
                    Point index = new Point(StartPosX, StartPosY);
                    int column = globalIDX;
                    int row = globalIDY;

                    index.X -= row % 2;
                    row /= 2;
                    index.Y += row;
                    index.X -= row;
                    Point actual_row_start = index;
                    index.Y += column;
                    index.X += column;


                    if (index.X < 0 || index.Y < 0 || index.Y >= MapSizeY || index.X >= MapSizeX)
                    {
                        continue;
                    }
                    visibleIndex = 0;

                    //check if we are outside with the Top Right Point of the camera
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

                    //calculate the starting point when outside of map on the right.
                    if (outside == 1)
                    {
                        //above map on the righ side
                        if (StartPosX + StartPosY < MapSizeX)
                        {
                            Point left = new Point(StartPosX - Rows, StartPosY + Rows);
                            left.X += left.Y;
                            left.Y -= left.Y;

                            Point righ_bottom_screen = new Point(StartPosX + Columns, StartPosY + Columns);
                            //check if we are passed the last Tile for MapSizeX with the Camera
                            if(righ_bottom_screen.X + righ_bottom_screen.Y > MapSizeX)
                            {
                                start = new Point(MapSizeX - 1, 0);
                            }
                            else
                            {
                                //we are above the Last Tile so x < MapSizeX for Camera right bottom Position
                                righ_bottom_screen.X += righ_bottom_screen.Y;
                                righ_bottom_screen.Y -= righ_bottom_screen.Y;
                                start = righ_bottom_screen;
                            }

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

                    //Calc how many rows are allready drawn behind us, until camera view end on the right side
                    int rows_behind = CalculateRows(index, MapSizeX) - CalculateRows(start, MapSizeX);

                    //this will be a array in the shader
                    //calculate how many tiles are in each Row will be drawn
                    for (int i = 0; i < rows_behind; i++)
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
                        visibleIndex += vertical_tiles;

                    }

                    //get all Colums to the actual Index
                    int columns = GetColumnsUntilBorder(index);

                    //get correct Index if the Camera is inside of the Map so we subtract all Colums above of the camera view
                    if (actual_row_start.X >= 0 && actual_row_start.Y >= 0)
                    {
                        columns -= GetColumnsUntilBorder(actual_row_start);
                    }

                    visibleIndex += columns;
                    //Debug.WriteLine(visibleIndex + "        " + rows_behind + "        " + start);
                }
            }
            Debug.WriteLine(visibleIndex + " In Shader calculated");

        }

        private static int GetColumnsUntilBorder(Point index)
        {
            if (index.X < index.Y)
            {
                return index.X;
            }

            return index.Y;

        }

        private static int CalculateRows(Point start, int mapSizeX)
        {
            int rows;
            //Check if we are near y axis or x axis he lower one is the number of rows. and for y > x we add mapSizeX
            if (start.Y < start.X)
            {
                start.X -= start.Y;
                start.Y -= start.Y;
                rows = mapSizeX - start.X;
            }
            else
            {
                start.Y -= start.X;
                start.X -= start.X;
                rows = mapSizeX + start.Y;
            }


            return rows;
        }
    }
}
