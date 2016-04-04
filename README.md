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

* On the far left is a window for choosing nodes to place down in the graph.
* On the right is the graph itself.
* On the top-left of the graph window is the specific graph file being edited. This dropdown box lists all the .gpug files in your Unity project.
* Below that is the button for creating a new graph.
* Below the "New Graph" button is a lot of empty space. When you have unsaved changes in your graph, you will instead see "Save Changes" and "Discard Changes" buttons here.
* In the middle-left of the window are the "hash" functions. The basis for all GPU noise is a function for hashing floats into a pseudo-random value between 0 and 1. You may customize the hash for 1D, 2D, and 3D float vectors if you wish.
* Below the hash functions is the preview window, which shows what would happen if you rendered your noise graph to a 2D texture. You can check the "Auto-Update Preview" box to automatically update the preview every time the graph is edited.
* On the right of these UI elements, separated by a solid black bar, is the graph area. This displays all your nodes. The right-most node, "Output", represents the final output of the graph.

In order to add a new node, click the button for the node you want to place, then left-click in the graph to place it. Right-click in the graph to cancel. A node's inputs are on the left side, and its output is on the right side. Note that some nodes have no inputs at all. Each input to a node is either the output of a different node or a constant value entered in a text box.

# Nodes

All the nodes this graph currently contains are listed here. Note that all noise nodes return a value between 0 and 1. See below this list for visual examples of some of the noise functions.

* Noise: Exposes a wide variety of 1D, 2D, or 3D noise with a scale/weight value for convenience when combining multiple octaves of noise.
    * White noise: a pseudo-random value.
    * Grid noise: gets white noise for the `floor`ed seed value. Creates 1x1 square blocks of noise.
    * Linear noise: Like Grid noise but with a linear interpolation between values instead of solid blocks.
    * Smooth noise: Like Linear noise but smoother and a bit more expensive.
    * Smoother noise: Like Smooth noise but smoother and a bit more expensive.
    * Perlin noise: Like Smoother noise but better, with fewer blocky artifacts, and more expensive.
    * Worley noise: Generates random points on a grid and outputs noise based on how far away each pixel is from the nearest point.
* UV: The X or Y coordinate (from 0 to 1) of the pixel in the render texture currently being generated.
* Float Parameter: A shader parameter that can be set when generating the noise.
* Tex2D Parameter: A texture parameter that can be set when generating the noise.
* Sub-graph: Outputs the result of another graph file.
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

**Note**: There is no checking for infinite loops with using graphs as nodes. In other words, if graph A calls into graph B, and graph B calls into graph A, actually using/previewing either graph will likely cause an infinite loop, crashing Unity.

# Applications

Several example applications are built into the editor; they are all accessible through the "GPU Noise" option in the editor's toolbar.

## <a name="GenerateShader"></a>Shader Generator

This tool generates a shader that outputs the graph's noise. You could then create a material that uses this shader (and has custom values for each of the parameter nodes in the graph) and use it in the editor or at runtime to generate noise via the `GPUGraph.GraphUtils` and `GPUGraph.GraphEditorUtils` classes.

## Texture Generator

This tool generates a 2D texture file using a graph.

## Terrain Generator

This tool uses a graph to generate a heightmap for whichever terrain object is currently selected in the scene view.


# Code

Code is organized in the following folder hierarchy:

* **Graph System**: The code for representing a graph.
    * **Nodes**: Specific kinds of nodes that can be placed in a graph.
    * **Editor**: The Unity editor window for creating/modifying graphs.
* **Applications**: The above-metioned sample utilities: texture, shader, and terrain generators.

## Graph System

*namespace: GPUGraph*

The basic system for creating and manipulating graph data. This system uses C#'s built-in serialization system to save/load graphs to/from a file.

A graph node is an instance of a class inheriting from `Node`. class. A node is given a unique UID by the graph it is added to, which is used when serializing/deserializing node references. It also has a rectangle representing its visual position in the graph editor.

A node's inputs are stored as a list of `NodeInput` instances. Each `NodeInput` is either a constant float (in which case `IsAConstant` returns `true` and `ConstantValue` is well-defined) or it is the output of another node (in which case `IsAConstant` returns `false` and `NodeID` contains the UID of the node whose output is being read).

Graphs are represented by the `Graph` class, which has a collection of nodes, the file path of the graph, the 1D/2D/3D hashing functions, and the final output of the graph (a `NodeInput` instance). It exposes `Save()` and `Load()` methods.

The `GraphEditorUtils` class provides various ways to interact with a graph in the editor, including `GetAllGraphsInProject()`, `SaveShader()`, `GenerateToTexture()`, and `GenerateToArray()`. The `GraphEditor` class provides various ways to interact with a graph at *run-time* (after it's already been generated into a shader/material in the editor), including `GenerateToTexture()` and `GenerateToArray()`.

The number of `Node` child classes is actually fairly small:
* `SimpleNode` handles any kind of one-line expression, including all the built-in shader functions like `sin`, `lerp`, `abs`, etc. The vast majority of nodes are instances of `SimpleNode`.
* `NoiseNode` is a noise generation node. It can be "White", "Blocky", "Linear", "Smooth", "Smoother", "Perlin", or "Worley". Note that "Worley" has special editing options that the other noise types don't need. It can also be 1D, 2D, or 3D.
* `TexCoordNode` represents the UV coordinates of the texture the graph noise is being rendered into. This is generally how you get the seed values for a noise function. It can output either the X or the Y coordinate.
* `ParamNode_Float` is a float parameter represented as either a text box or a slider.
* `ParamNode_Texture2D` is a 2D texture parameter.
* `SubGraphNode` allows graphs to be used inside other graphs. 
## Applications

*namespace: GPUGraph.Applications*

The various built-in utilities mentioned above: *ShaderGenerator*, *TextureGenerator*, and *TerrainGenerator*.

## Other

There are also a few general-purpose scripts that exist outside these folders/namespaces:
* `StringUtil`: Helper functions for path/string manipulation.
* `GUIUtil`: Provides primitive-drawing functions for use in the graph editor window.


# License

All code belongs to William Manning. Released under the [Creative Commons 3.0 Attribution License](https://creativecommons.org/licenses/by/3.0/us/).
