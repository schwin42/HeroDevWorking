VoxelMax For Unity v1.51011
ReadMe:
VoxelMax is a Unity plugin. It’s main purpose is to provide a well
balanced fast tool for game developers to build their own voxel
based models inside the Unity environment.

Startup:
Our tool has three main ways to begin a new voxel structure.

New Structure:
With the help of this tool, you can start building a model from scratch.
You only have to set the starting dimension for the structure.
Texture to Voxels:
This way you can use a 2d texture to start your work. Please
be advised that you have to use relatively low resolution pictures! 
Model to Voxels:
By using this tool, you can convert a gameobject(3d model) to
voxels, but it is an important a requirement to already have 
a collider on your source object.

Editor:
It does not matter which method you choose, you can use the editor
functionality to edit the resulting voxel structure.
The editor has eight different modes.

Cursor Mode(F5):
In this mode you can select/deselect you whole object. Also there
are two options for you here.
-Explode the structure into standalone cubes. You can set their
size and some space between them. This space can be important for
the physics engine in Unity.
-Build optimized mesh. The purpose is of this function is to
provide better mesh with much less polygons. The current algorithm
would probably take hours to run on a complicated or large
structure. But you can set a limit for it. The smaller the limit
 the fastest it gets, but the provided mesh much more likely to have 
larger number of voxels. If you set this limit to 0 then it will
genereate the best possible sollution for the optimization.

Selector Mode(F6):
In this mode you can select voxels inside your current structure.
There is a switch for this mode. If you use the selector in
surface mode you can select only the visible voxels in front of you,
but without the surface selector modifier, you select all the
voxels under the selected area.

Select by color Mode(F7):
With this feature, you can select voxels by their colors.
The surface selector mode also works in this case.


Extrude mode(F8):
To use this mode, you already have to have selected voxels. If you
have some, then there will be three arrows to drag in the editor,
and by doing that, you can extrude voxels by any of the axes.

Drawing mode(F9):
By using this tool, you can simply add voxels next to any existing one. 

Erasing mode(F10):
With this tool, you can remove voxels from the structure.

Paint brush mode(F11):
This tool colorizes any voxels it touches.

Paint bucket mode(F12):
This tool colorizes your previously selected voxels. 



Contact:
If you have any problems concerning our tool or you have new feature
ideas, please feel free to contact us by any of the following means.
E-mail: nanoidgames@gmail.com
Facebook: https://www.facebook.com/NanoidGames
Twitter: https://twitter.com/NanoidGames


