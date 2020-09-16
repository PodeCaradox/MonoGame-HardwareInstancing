"MonoGame-HardwareInstancing" 

this project shows how monogame 3.8.0.1641 can draw many isometric tiles with more gpu usage than spritebatch and still be dynamic.

Can be used on Linux/Windows/Mac.

(the array filling can be multithreaded if you fill it up like same column size and rows/threads number, so you need in each thread the row index and column size to fill the array correctly without racecondition) thats the way i do it in my project, so only the DrawInstancedPrimitives is singlethreaded because opengl ;)
![img1](https://github.com/PodeCaradox/MonoGame-HardwareInstancing/blob/master/Images/Preview.gif)


It was just a sideproject for my game, so i could test it, and like the image shows it just works perfect :)
![preview](https://github.com/PodeCaradox/MonoGame-HardwareInstancing/blob/master/Images/Project.JPG)
(its a 1000 x 1000 tile map and only camera view will be drawn)
