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
            for (int globalIDY = 0; globalIDY < 64; globalIDY++)
            {
                for (int globalIDX = 0; globalIDX < 64; globalIDX++)
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
                    int rows_behind = CalculateRows(index, MapSizeX) - CalculateRows(start, MapSizeX);

                    //this will be a array in the shader
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


                    int columns = GetColumnsUntilBorder(index);

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
