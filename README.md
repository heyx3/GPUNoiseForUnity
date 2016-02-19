# GPUGraph

A Unity plugin for generating coherent noise on the GPU, mainly useful for in-editor generation.

The newest Unitypackage file for this plugin is stored in the root of this repo: *GPUGraph.unitypackage*.

# Overview

This repo contains the GPUGraph plugin for Unity, which provides classes and editors for generating floating-point noise via the graphics card.
This is useful because the graphics card can generate noise almost instantly, compared to the CPU which could take a long time.

This system is primarily designed for use in the editor and not at run-time -- Unity no longer supports rum-time compilation of shader code.
However, you can [generate a shader from the graph](#GenerateShader) in the editor and then use that shader at run-time with the help of the `GPUGraph.GraphUtils` class.

The basic functionality behind GPUGraph is a "Directed Acyclic Graph" of commands that represents shader code.
This graph is created by a programmer or artist and used for various purposes in the editor.
Every node in the graph takes some number of floats as inputs and outputs a single float as a result.
Here is an example of a graph that generates pure white noise:

![White Noise](https://github.com/heyx3/GPUNoiseForUnity/blob/master/Readme%20Images/WhiteNoise.png)

# Editor

In the above image, note the various aspects of the graph editor:

* On the top-left is the graph beind edited. This dropdown box lists all the .gpug files in your Unity project.
* Below that is the button for creating a new graph.
* Below the "New Graph" button is a lot of empty space. When you have unsaved changes in your graph, you will instead see "Save Changes" and "Discard Changes" buttons here.
* In the middle-left of the window are the "hash" functions. The basis for all GPU noise is a function for hashing floats into a pseudo-random value between 0 and 1. You may customize the hash for 1D, 2D, and 3D float vectors if you wish.
* Below the hash functions is the preview window, which shows what would happen if you rendered your noise graph to a 2D texture. You can check the "Auto-Update Preview" box to automatically update the preview every time the graph is edited.
* On the right of these UI elements, separated by a solid black bar, is the graph area. This displays all your nodes. The right-most node, "Output", represents the final output of the graph.

In order to add a new node, right-click in the graph area and select the node you want to create. A node's inputs are on the left side, and its output is on the right side. Note that some nodes have no inputs at all. Each input is either the output of a different node or a constant value entered in a text box.

# Nodes

All the nodes this graph currently contains are listed here. Note that all noise nodes return a value between 0 and 1. See below this list for visual examples of some of the noise functions:

* WhiteNoise1: takes a single float and hashes it to get a pseudo-random value.
* WhiteNoise2: the two-dimensional version of WhiteNoise1 (takes two floats instead of one).
* WhiteNoise3: the three-dimensional version of WhiteNoise1 (takes three floats instead of one).
* GridNoise1: takes a single float, `floor`s it, and hashes the floored value. This creates a blocky noise value.
* GridNoise2: the two-dimensional version of GridNoise1.
* GridNoise3: the three-dimensional version of GridNoise1.
* LinearNoise1: takes a single float, `floor`s and `ceil`s it, and `lerp`s between the hashes of those two values. This creates a smoother version of GridNoise at a higher performance cost.
* LinearNoise2: the two-dimensional version of LinearNoise1.
* LinearNoise3: the three-dimensional version of LinearNoise1.
* SmoothNoise1: a smoother version of LinearNoise1, at a higher performance cost.
* SmoothNoise2: the two-dimensional version of SmoothNoise1.
* SmoothNoise3: the three-dimensional version of SmoothNoise1.
* SmootherNoise1: a smoother version of SmoothNoise1, at a higher performance cost.
* SmootherNoise2: the two-dimensional version of SmootherNoise1.
* SmootherNoise3: the three-dimensional version of SmootherNoise1.
* PerlinNoise1: a high-quality alternative to SmootherNoise1, with a higher performance cost.
* PerlinNoise2: the two-dimensional version of PerlinNoise1.
* PerlinNoise3: the three-dimensional version of PerlinNoise1.

The rest of the nodes are not noise functions themselves, but can be very useful.
* Fract: gets the fractional part of an input.
* Ceil: gets the next integer value after the input.
* Floor: gets the integer value just before the input.
* Truncate: gets the non-fractional part of an input.
* RoundToInt: rounds the input to the nearest integer.
* Sign: Returns -1 if the input is negative, 0 if it's zero, and +1 if it's positive.
* Abs: Returns the absolute value of the input.
* Cos: Returns the cosine of the input.
* Sin: Returns the sine of the input.
* Tan: Returns the tangent of the input.
* Acos: Returns the inverse cosine of the input.
* ASin: Returns the inverse sine of the input.
* Atan: Returns the inverse tangent of the input.
* Sqrt: Returns the square root of the input.
* Log: Returns the logarithm base *e* of the input.
* Add: Adds two inputs together.
* Subtract: Subtracts the second input from the first.
* Multiply: Multiplies two inputs together.
* Divide: Divides the first input by the second.
* Max: Gets the largest of two inputs.
* Min: Gets the smallest of two inputs.
* Pow: Raises the first input to the power of the second input.
* Step: Returns 0 if the second input is less than the first one, or 1 if it is larger.
* Atan2: A version of atan that takes the individual X and Y components.
* Clamp: Forces the third input to be no smaller than the first one and no larger than the second.
* Lerp: Linearly interpolates between the first and second inputs using the third input.
* Smoothstep: Like `Lerp` but with a smooth, third-order curve instead of a line.
* Remap: Remaps a value from the "source" min and max to the "destination" min and max.
* UV_X: The X coordinate (from 0 to 1) of the pixel in the render texturecurrently being generated.
* UV_Y: The Y coordinate (from 0 to 1) of the pixel in the render texture currently being generated.
* FloatParam: A shader parameter that can be set when generating the noise.
* SliderParam: Another kind of shader parameter.

Here is an example of how to use the various noise nodes in a simple graph:

![Grid Noise](https://raw.githubusercontent.com/heyx3/GPUNoiseForUnity/master/Readme%20Images/GridNoise.png)
![Linear Noise](https://raw.githubusercontent.com/heyx3/GPUNoiseForUnity/master/Readme%20Images/LinearNoise.png)
![Smooth Noise](https://raw.githubusercontent.com/heyx3/GPUNoiseForUnity/master/Readme%20Images/SmoothNoise.png)
![Perlin Noise](https://raw.githubusercontent.com/heyx3/GPUNoiseForUnity/master/Readme%20Images/PerlinNoise.png)


# Applications

Several example applications are built into the editor; they are all accessible through the "GPU Noise" option in the editor's toolbar.

## <a name="GenerateShader"></a>Shader Generator

This tool generates a shader that outputs the graph's noise. You could then create a material that uses this shader (and has custom values for each of the parameter nodes in the graph) and use it to generate noise via the `GPUGraph.GraphUtils` class.

## Texture Generator

This tool generates a 2D texture file using a graph's noise.

## Terrain Generator

This tool uses a graph's noise to generate a heightmap for whatever terrain object is currently selected in the scene view.


# Code

There are three categories of code in the project:

## Graph System

*namespace: GPUGraph*

The basic system for creating and manipulating graph data.

A graph node is an instance of the `Func` class. Simple nodes (e.x. *WhiteNoise1*, *Lerp*, *Sin*) are actual `Func` instances, while more complex nodes (*UV_x*, *UV_y*, *FloatParam*, and *SliderParam*) are child classes that inherit from `Func`. All node types are defined in *FuncDefinitions.cs*.

The `GraphEditorUtils` class provides various ways to interact with a graph, including `GetAllGraphsInProject()`, `LoadGraph()`, `SaveGraph()`, `SaveShader()`, `GenerateToTexture()`, and `GenerateToArray()`.

If you want to add a new simple node, just go to the declaration of the "Functions" array in `FuncDefinitions` and add your Func to the end of it in the form of actual shader code (take a look at the other nodes for examples), or if it's *really* simple you can use one of the the `MakeSimpleX` functions to generate it. Note that you may add default values for each input, and the function must return a `float`.

If you want to create a more complex node, refer to the bottom of *FuncDefinitions.cs* to see how *UV_x*, *UV_y*, *FloatParam*, and *SliderParam* were implemented.

## Graph Editor

*namespace: GPUGraph.Editor*

The editor for the graph is split into two classes:

* `EditorGraph`: The graph itself, plus the file-name. When an instance is created, the positions of each node are automatically generated.
* `EditorGraphWindow`: The `EditorWindow` class that contains all the Unity GUI code for rendering/modifying the graph.

## Applications

*namespace: GPUGraph.Applications*

The various built-in utilities mentioned above: *ShaderGenerator*, *TextureGenerator*, and *TerrainGenerator*.

## Other

There are also a few general-purpose scripts that exist outside these folders/namespaces:
* `GraphUtils`: As mentioned before, this class lets you generate noise data into a buffer on the GPU (i.e. a texture) or the CPU (i.e. an array).
* `PathUtils`: Helper functions for file paths.
* `GUIUtil`: Provides primitive-drawing functions for use in the Unity editor GUI system.


# License

All code belongs to William Manning. Released under the [Creative Commons 3.0 Attribution License](https://creativecommons.org/licenses/by/3.0/us/).
