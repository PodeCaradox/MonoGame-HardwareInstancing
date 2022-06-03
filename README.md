"MonoGame-HardwareInstancing" 

this project shows how MonoGame.Framework.Compute.DesktopGL 3.8.1.3, can draw many isometric tiles with more gpu usage than spritebatch and still be dynamic(normaly you only would update tileIds and not all tiles, or animate the tiles in the Compute Shader).

Can be used on Linux/Windows/Mac.

(the array filling is culled and filled in the compute shader) ;)
![Preview](https://github.com/PodeCaradox/MonoGame-HardwareInstancing/blob/master/Images/Preview.JPG)


It was just a sideproject for my game, so i could test it, and like the image shows it just works perfect :)
![Project](https://github.com/PodeCaradox/MonoGame-HardwareInstancing/blob/master/Images/Project.JPG)
(its a 1000 x 1000 tile map and only camera view will be drawn)
