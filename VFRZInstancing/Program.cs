using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
namespace VFRZInstancing
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            //testc();
            using (var game = new Game1())
                game.Run();
        }

        private static void testc()
        {
            int StartPosX = -5;
            int StartPosY = 9;
            int Columns = 64;
            int Rows = 64 / 2;
            int MapSizeX = 70;
            int MapSizeY = 70;
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
                    index.Y += column;
                    index.X += column;


                    if (index.X < 0 || index.Y < 0 || index.Y >= MapSizeY || index.X >= MapSizeX)
                    {
                        continue;
                    }

                    int visibleIndex = 0;
                    int rows_behind = 0;
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
                    if(outside == 1)
                    {
                        Point left = new Point(StartPosX - Rows, StartPosY + Rows);
                        left.X += left.Y;
                        left.Y -= left.Y;

                        Point righ_bottom_screen = new Point(StartPosX + Columns, StartPosY + Columns);
                        if (righ_bottom_screen.X + righ_bottom_screen.Y > MapSizeX)
                        {
                            start = new Point(MapSizeX - 1, 0);
                        }
                        else
                        {
                            righ_bottom_screen.X -= righ_bottom_screen.Y;
                            righ_bottom_screen.Y -= righ_bottom_screen.Y;
                            start = righ_bottom_screen;
                        }
                       


                        int difference = start.X - left.X;
                        difference += difference % 2;
                        difference /= 2;
                        start.X -= difference;
                        start.Y -= difference;


                    }//inside the map
                    else
                    {
                        start = new Point(StartPosX, StartPosY);
                        
                        // welche Reihe bin ich links welche Rechts und subtrahieren  = rows
                    }
                    rows_behind = CalculateRows(index, MapSizeX) - CalculateRows(start, MapSizeX);



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

                    if (index.X < index.Y)
                    {
                        visibleIndex += index.X;
                    }
                    else
                    {
                        visibleIndex += index.Y;
                    }

                    Debug.WriteLine(visibleIndex + "        " + rows_behind + "        " + start);
                }
            }



        }

        private static int CalculateRows(Point start, int mapSizeX)
        {
            int rows;
            if(start.Y < start.X)
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
