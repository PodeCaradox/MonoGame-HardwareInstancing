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
            using (var game = new Game1())
                game.Run();
        }

      
    }
}
