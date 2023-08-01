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
        private static int StartPosX, StartPosY, Columns, Rows, MapSizeX, MapSizeY;
        private static int[] RowsIndex;

        internal static void testc(int StartPosX, int StartPosY, int Columns, int Rows, int MapSizeX, int MapSizeY, int[] RowsIndex)
        {
            int dummy123 = 0;
            ShaderInCSharpImpl.StartPosX = StartPosX;
            ShaderInCSharpImpl.StartPosY = StartPosY;
            ShaderInCSharpImpl.Columns = Columns;
            ShaderInCSharpImpl.Rows = Rows;
            ShaderInCSharpImpl.MapSizeX = MapSizeX;
            ShaderInCSharpImpl.MapSizeY = MapSizeY;
            ShaderInCSharpImpl.RowsIndex = RowsIndex;

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


                    visibleIndex = calc_visible_index(index, actual_row_start);
                    if(dummy123 != visibleIndex)
                    {

                    }
                    dummy123++;
                    Debug.WriteLine(visibleIndex + "        " + index);
                }
            }
            Debug.WriteLine(visibleIndex + " In Shader calculated");

        }



        private static int is_in_map_bounds(Point map_position)
        {
            if (map_position.X >= 0 && map_position.Y >= 0 && map_position.Y < MapSizeY && map_position.X < MapSizeX) { return 1; }

            return 0;
        }

        private static int calculate_rows(Point start, int mapSizeX)
        {
            //35 , 1
            int rows = 0;
            if (start.Y < start.X)
            {
                rows = (mapSizeX - 1) - (start.X - start.Y);
            }
            else
            {
                rows = (mapSizeX - 1) + (start.Y - start.X);

            }
            if (rows < 0) return 0;

            return rows;
            
        }

        private static int get_columns_until_border(Point index)
        {
            if (index.X < index.Y)
            {
                return index.X;
            }
            return index.Y;
        }

        private static int is_outside_of_map(Point start_pos)
        {
            Point pos = start_pos;
            for (int i = 0; i < Columns; i += 1)
            {
                pos.X += 1;
                pos.Y += 1;
                if (is_in_map_bounds(pos) == 1)
                {
                    return 0;
                }
            }
            return 1;
        }

        private static Point calc_start_point_outside_map(Point start_pos)
        {
            Point start = start_pos;
            //above right side of map
            if (StartPosX + StartPosY < MapSizeX)
            {
                Point left = new Point(StartPosX - Rows, StartPosY + Rows);
                left.X += left.Y;
                left.Y -= left.Y;

                Point right_bottom_screen = new Point(StartPosX + Columns, StartPosY + Columns);
                //check if we are passed the last Tile for MapSizeX with the Camera
                if (right_bottom_screen.X + right_bottom_screen.Y > MapSizeX)
                {
                    start = new Point(MapSizeX, 0);

                }
                else
                {
                    //we are above the Last Tile so x < MapSizeX for Camera right bottom Position
                    right_bottom_screen.X += right_bottom_screen.Y;
                    right_bottom_screen.Y -= right_bottom_screen.Y;
                    start = right_bottom_screen;
                }

                //difference is all tiles on the x axis and because we calculate here x,y different to Isomectric View we need to divide by 2 and for odd number add 1 so % 2
                int difference = start.X - left.X;
                difference += difference % 2;
                difference /= 2;
                start.X -= difference;
                start.Y -= difference;
                return start;
            }
            //underneath right side of map
            int to_the_left = StartPosX - MapSizeX;
            return new Point(StartPosX - to_the_left, StartPosY + to_the_left);
        }

        private static Point get_start_point(Point start_pos)
        {
            int outside = is_outside_of_map(start_pos);
            if (outside == 1)
            { //calculate the starting point when outside of map on the right.
                return calc_start_point_outside_map(start_pos);
            }
            //inside the map
            return new Point(StartPosX, StartPosY);
        }


        private static int calc_visible_index(Point index, Point actual_row_start)
        {
            int visible_index = 0;

            Point start = get_start_point(new Point(StartPosX, StartPosY));
            int dummy = calculate_rows(index, MapSizeX);
            int dummy1 = calculate_rows(start, MapSizeX);

            int rows_behind = dummy - dummy1;

           
           visible_index = RowsIndex[rows_behind];
            



            //index in current column
            int columns = get_columns_until_border(index);
            if (actual_row_start.X >= 0 && actual_row_start.Y >= 0)
            {
                columns -= get_columns_until_border(actual_row_start);
            }

            visible_index += columns;
            return visible_index;
        }
    }
}
