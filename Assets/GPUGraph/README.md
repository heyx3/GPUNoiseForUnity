# GPUGraph

An open-source Unity plugin for generating coherent noise on the GPU, for both editor and runtime uses. The source is available [here](http://www.github.com/heyx3/GPUNoiseForUnity), on Github. It is also available on [the Asset Store](https://www.assetstore.unity3d.com/#!/content/68911).

Editor tools for this plugin are available in the "Assets/GPU Graph" section on the Unity editor's toolbar. The *.unitypackage* file for this plugin is stored in the root of this repo: *GPUGraph.unitypackage*.


# Overview

This repo contains the GPUGraph plugin for Unity, which provides classes and editors for generating floating-point noise on the GPU with shaders.
This leads to extremely fast noise generation compared to traditional CPU methods.

Note that because Unity no longer supports runtime compilation of shaders, Graphs can only really be used in the editor. However, shaders can be generated from the graph in the editor then used in real-time (graphs can expose float and Tex2D parameters which are changeable at run-time). A serializable class "RuntimeGraph" is provided to greatly simplify use of run-time graphs; see "SampleScene.unity" and "SampleGPUGScript.cs" for an example of how to use it (note that you need to look at the SampleGPUGScript component in the Inspector to regenerate shaders before the scene will work).

The basic structure behind GPUGraph is a "Directed Acyclic Graph" (essentially a tree whose nodes can have multiple parents) of commands that represents shader code. Every node in the graph takes some number of floats as inputs and outputs a single float as a result.

Graphs are created in a custom editor window and saved as ".gpug" files into the "Assets" folder. The editor is accessed by selecting "Assets/GPU Graph/Editor" in the Unity editor's toolbar. Here is an example of a very simple graph that generates pure white noise:

![White Noise](https://github.com/heyx3/GPUNoiseForUnity/blob/master/Readme%20Images/WhiteNoise.png)

A side utility is also provided for easy random number generation on the GPU: "Assets/GPUGraph/GpuRandWithState.cginc".


# Editor

In the above image, note the various aspects of the graph editor:

* On the far left is a section for choosing nodes to place down in the graph.
* To the right of that is a set of options for the graph, and for loading/creating other ones.
* On the top of this "options" section is the specific graph file being edited. This dropdown box lists all the .gpug files in your Unity project.
* Below the "New Graph", "Save Changes", and "Discard Changes" buttons is the preview window, which shows what would happen if you rendered your noise graph to a 2D texture. You can check the "Auto-Update Preview" box to automatically update the preview every time the graph is edited.
* On the right side of the window, is the graph area. This displays all your nodes. The right-most node, "Output", represents the final output of the graph. You can right-click and drag to pan the area.

In order to add a new node, click the button for the node you want to place, then left-click in the graph to place it. Right-click in the graph to cancel. A node's inputs are on the left side, and its output is on the right side. Note that some nodes have no inputs at all. Each input to a node is either the output of a different node or a constant value entered in a text box.

You can add the basic arithmetic nodes by pressing their respective keys (`+`, `-`, `*`, and `/`).


# Nodes

All the nodes this plugin currently offers are listed here:

* Noise: Exposes a variety of 1D, 2D, 3D, or 4D noise algorithms (all of which return a value between 0 and 1) with a scale/weight value for convenience when combining multiple octaves of noise. They can all optionally wrap around their edges so that the resulting noise tiles every 1 units.
    * White noise: a pseudo-random value.
    * Grid noise: Creates 1x1 square blocks of noise by getting White Noise for the `floor` of the seed value.
    * Linear noise: Like Grid noise but with a linear interpolation between values, yielding smooth diamond shapes instead of square blocks.
    * Smooth noise: Like Linear noise but smoother and a bit more expensive.
    * Smoother noise: Like Smooth noise but even smoother and a bit more expensive.
    * Perlin noise: Like Smooth noise but with fewer blocky artifacts, and much more expensive.
    * Worley noise: Generates a random point in every 1x1 block of space and outputs noise based on how far away the given value is from the nearest point. Note that Worley noise only tiles properly if the scale is a power of two.
* UV: The X, Y, or Z coordinate (from 0 to 1) of the pixel in the render texture currently being generated. The Z coordinate is useful when generating 3D textures. It can also be thought of as a seed value for 2D noise.
* Custom Expression: A custom HLSL expression with any number of inputs.
* Float Parameter: A shader parameter that can be easily changed after the graph has been compiled into a Material.
* Tex2D Parameter: A texture parameter that can be easily changed after the graph has been compiled into a Material.
* Sub-graph: Represents the output of another graph file.
* Ceil: rounds the input to the next largest integer.
* Floor: rounds the input to the next smallest integer.
* Truncate: gets the non-fractional part of an input.
* Fract: gets the fractional part of an input.
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
* Remap: Remaps a value from the "source" min and max range to the "destination" min and max range.


# Applications

Several example applications are built into the editor; they are all accessible through the "Assets/GPU Graph" category in the Unity editor's toolbar.

## <a name="GenerateShader"></a>Shader Generator

This tool generates a shader that outputs the graph's noise. You could then create a material that uses this shader and use it in the editor or at runtime to generate noise via the `GPUGraph.GraphUtils` and `GPUGraph.GraphEditorUtils` classes. Fortunately, a helper class `RuntimeGraph` has already been created to make this easier for you, complete with custom Inspector code, but this utility is still useful if you want to further mess with the shader after generating it.

## Texture 2D Generator

This tool generates a 2D texture file using a graph.

## Texture 3D Generator

This tool generates a 3D texture file (and, optionally, an accompanying normal texture) using a graph.

## Texture Generator

This is the abstract base class for Texture2DGenerator and Texture3DGenerator. It provides the bulk of their GUI.


## Terrain Generator

This tool uses a graph to generate a heightmap for whichever terrain object is currently selected in the scene view.


# Code

Code is organized in the following way inside the "GPU Noise" folder:

* Runtime: Scripts for using graphs at run-time. Take a look at "SampleGPUGScript.cs" for an example of how to use these.
* Editor: Scripts for using graphs in the editor.
    * **Graph System**: The code for representing/editing a graph.
        * **Nodes**: Specific kinds of nodes that can be placed in a graph.
        * **Editor**: The Unity editor window for creating/modifying graphs.
    * **Applications**: The above-metioned sample utilities: texture, shader, and terrain generators.

## Graph System

*namespace: GPUGraph*

The basic system for creating and manipulating graph data. This system uses C#'s built-in serialization system to save/load graphs to/from a file.

A graph node is an instance of a class inheriting from `Node`. A node is given a unique UID by the graph it is added to, which is used when serializing/deserializing node references. It also has a rectangle representing its visual position in the graph editor.

A node's inputs are stored as a list of `NodeInput` instances. Each `NodeInput` is either a constant float (in which case `IsAConstant` returns `true` and `ConstantValue` is well-defined) or it is the output of another node (in which case `IsAConstant` returns `false` and `NodeID` contains the UID of the node whose output is being read).

Graphs are represented by the `Graph` class, which has a collection of nodes, the file path of the graph, and the final output of the graph (a `NodeInput` instance). It exposes `Save()` and `Load()` methods, as well as `GenerateShader()` to get shader code, and the more abstract `InsertShaderCode()` to use the graph's noise output as part of a more complex shader.

The `GraphEditorUtils` class provides various ways to interact with a graph in the editor, including `GetAllGraphsInProject()`, `SaveShader()`, `GenerateToTexture()`, and `GenerateToArray()`. There is a similar class `GraphUtils` that can be used at runtime.

The number of `Node` child classes is actually fairly small:
* `SimpleNode` handles any kind of one-line expression, including all the built-in shader functions like `sin`, `lerp`, `abs`, etc. The vast majority of nodes are instances of `SimpleNode`.
* `CustomExprNode` allows the user to type a custom shader expression using any number of inputs (`$1`, `$2`, `$3`, etc).
* `NoiseNode` is a node that generates 1D, 2D, 3D, or 4D noise. The noise can be "White", "Blocky", "Linear", "Smooth", "Smoother", "Perlin", or "Worley". Note that "Worley" has special editing options that the other noise types don't need.
* `TexCoordNode` represents the UV coordinates of the texture the graph noise is being rendered into. This is generally how you get the input values for a noise function. It can output the X, Y, or Z coordinate. Z coordinate is used when you want to generate a 3D texture from the graph.
* `ParamNode_Float` is a float parameter, represented as either a text box or a slider.
* `ParamNode_Texture2D` is a 2D texture parameter.
* `SubGraphNode` allows graphs to be used inside other graphs.

## Applications

*namespace: GPUGraph.Applications*

This folder contains the various built-in utilities mentioned above: *ShaderGenerator*, *Texture2DGenerator*, *Texture3DGenerator*, and *TerrainGenerator*.

# Known Issues

If anybody wants to help out with these issues (or contribute to the codebase in any other way), feel free to send a pull request or email me at manning.w27@gmail.com.

* When you first click on the editor after creating a new graph, it goes back to a state of not editing anything for some reason.

* The editor window's UI is pretty rough; you get what you pay for :P. You can pan the view, but you can't zoom in or out. Adding zoom functionality can most likely be done somehow with the use of the GUIUtility.ScaleAroundPivot() method.
* Additionally, the lines connecting nodes don't get drawn if they go off the right side of the graph area, and they get drawn over the other panes if they go off the left side.
* Features like multi-node selection/movement, comments, and pressing Ctrl+S to save would be nice.

* Terrain Generator should show a preview of the generated image.

* UnityEditor's `Undo` class just didn't seem to work at all with the graph editor window, so I don't use it at all (although I at least got it to work with RuntimeGraph's custom inspector). However, the editor closely tracks any unsaved changes and makes sure to tell you if you're about to discard them.

* There are some bugs with drawing textures in RuntimeGraph's custom Inspector code; sometimes the textures aren't visible. As far as I can tell, this is Unity's problem.

* It seems impossible to use one of Unity's built-in textures as a "default" value for a Texture2D parameter because I can't serialize its asset path (they all have the same file path, "Resources/unity_buitin_extra"). Currently I log a warning if you try to use one of them.

# License

All code belongs to William Manning. Released under the MIT License (TL;DR: use it for literally any purpose, including commercially!)

````Copyright (c) 2016 William Manning

````Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

````The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

````THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
