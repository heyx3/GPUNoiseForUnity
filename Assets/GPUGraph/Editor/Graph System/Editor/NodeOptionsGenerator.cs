using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using P = GPUGraph.SimpleNode.Param;
using Category = GPUGraph.Editor.NodeTree_Element_Category;
using Option = GPUGraph.Editor.NodeTree_Element_Option;


namespace GPUGraph.Editor
{
	//TODO: ParamNode_Texture3D and Cube (http://docs.unity3d.com/432/Documentation/Components/SL-Properties.html).
	//TODO: CustomFunction node, like a fuller version of CustomExpression.
	//TODO: Add buttons to addition, subtraction, min/max, etc. nodes for an arbitrary number of inputs.
	//TODO: Optionally have more than one output. Use for noise, texture nodes, and TexCoordNode.


	/// <summary>
	/// A tree of node types to choose from.
	/// Each element is either an option or a branch leading to more elements of the same category.
	/// </summary>
	public abstract class NodeTree_Element
	{
		/// <summary>
		/// Draws this element as a selectable option in an editor window.
		/// Returns the element that was selected, or "null" if nothing was selected.
		/// </summary>
		public abstract NodeTree_Element_Option OnGUI();
	}


	public class NodeTree_Element_Category : NodeTree_Element
	{
		public string Title, Tooltip;
		public NodeTree_Element[] SubItems;

		public NodeTree_Element_Category(string title, params NodeTree_Element[] subItems)
		{
			Title = title;
			SubItems = subItems;
		}
		public NodeTree_Element_Category(string title, string tooltip, params NodeTree_Element[] subItems)
		{
			Title = title;
			Tooltip = tooltip;
			SubItems = subItems;
		}

		private bool foldout = false;
		public override NodeTree_Element_Option OnGUI()
		{
			NodeTree_Element_Option option = null;

			foldout = EditorGUILayout.Foldout(foldout, Title);
			if (foldout)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(25.0f);
				GUILayout.BeginVertical();

				foreach (NodeTree_Element el in SubItems)
				{
					NodeTree_Element_Option temp = el.OnGUI();
					if (temp != null)
						option = temp;
				}

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}

			return option;
		}
	}
	public class NodeTree_Element_Option : NodeTree_Element
	{
		public static NodeTree_Element_Option OneVarFunc(string func, string title, string tooltip,
														 string var = "f", float defVal = float.NaN)
		{
			return new Option((g, r) => new SimpleNode(r, func + "('" + var + "')", title, new P(var, defVal)),
							  title, tooltip);
		}
		public static NodeTree_Element_Option TwoVarFunc(string func, string title, string tooltip,
														 string var1 = "x", float defVal1 = float.NaN,
														 string var2 = "y", float defVal2 = float.NaN)
		{
			return new Option((g, r) => new SimpleNode(r, func + "('" + var1 + "', '" + var2 + "')", title,
													   new P(var1, defVal1), new P(var2, defVal2)),
							  title, tooltip);
		}
		public static NodeTree_Element_Option ThreeVarFunc(string func, string title, string tooltip,
														   string var1 = "x", float defVal1 = float.NaN,
														   string var2 = "y", float defVal2 = float.NaN,
														   string var3 = "t", float defVal3 = float.NaN)
		{
			return new Option((g, r) => new SimpleNode(r, func + "('" + var1 + "', '" + var2 + "', '" + var3 + "')", title,
													   new P(var1, defVal1), new P(var2, defVal2), new P(var3, defVal3)),
							  title, tooltip);
		}

		public Func<Graph, Rect, Node> NodeFactory;
		public string Name, Tooltip;

		public NodeTree_Element_Option(Func<Graph, Rect, Node> nodeFactory,
									   string name, string tooltip = "")
		{
			NodeFactory = nodeFactory;
			Name = name;
			Tooltip = tooltip;
		}

		public override NodeTree_Element_Option OnGUI()
		{
			GUILayout.BeginHorizontal();

			bool pressed = GUILayout.Button(new GUIContent(Name, Tooltip));
			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();

			return (pressed ? this : null);
		}
	}


	public static class NodeOptionsGenerator
	{
		/// <summary>
		/// Returns the root of the option list.
		/// </summary>
		public static List<NodeTree_Element> GenerateList()
		{
			return new List<NodeTree_Element>() {
				new Category("Noise", "Noise-generation functions",
					new Option((g, r) => new NoiseNode(r, NoiseNode.NoiseTypes.White, 3),
							   "White Noise", "Fast, completely chaotic noise"),
					new Option((g, r) => new NoiseNode(r, NoiseNode.NoiseTypes.Blocky, 3),
							   "Grid Noise", "White noise that's broken up into square blocks"),
					new Option((g, r) => new NoiseNode(r, NoiseNode.NoiseTypes.Linear, 3),
							   "Linear Noise", "Low-quality but fast coherent noise"),
					new Option((g, r) => new NoiseNode(r, NoiseNode.NoiseTypes.Smooth, 3),
							   "Smooth Noise", "Medium-quality, fairly fast coherent noise"),
					new Option((g, r) => new NoiseNode(r, NoiseNode.NoiseTypes.Smoother, 3),
							   "Smoother Noise", "High-quality but slow coherent noise"),
					new Option((g, r) => new NoiseNode(r, NoiseNode.NoiseTypes.Perlin, 3),
							   "Perlin Noise", "Beautiful but very slow coherent noise"),
					new Option((g, r) => new NoiseNode(r, NoiseNode.NoiseTypes.Worley, 2),
							   "Worley Noise", "Generates noise that looks like Voroni diagrams")),
				new Category("Interpolation", "Ways of transitioning from one value to another",
					Option.TwoVarFunc("step", "Step", "Returns 0 if X is less than Y and 1 if X is more than Y",
									  "y", 0.5f, "x"),
					Option.ThreeVarFunc("lerp", "Lerp", "Linearly interpolates between a and b based on t",
										"a", float.NaN, "b", float.NaN, "t"),
					Option.ThreeVarFunc("smoothstep", "Smoothstep", "Like \"Lerp\" but pushed out to the edges of the range",
										"a", 0.0f, "b", 1.0f, "t"),
					new Option((g, r) => new SimpleNode(r, "smoothstep('x', 'y', smoothstep(0.0, 1.0, 't'))",
														"Smoothstep",
														new P("x", 0.0f), new P("y", 0.0f), new P("t")),
							   "Smootherstep", "Like \"Smoothstep\" but even more pushed outwards"),
					new Option((g, r) => new SimpleNode(r, "lerp('destMin', 'destMax', " +
																 "('srcVal' - 'srcMin') / ('srcMax' - 'srcMin'))",
														"Remap",
														new P("destMin", 0.0f), new P("destMax", 1.0f),
														new P("srcMin", -1.0f), new P("srcMax", 1.0f),
														new P("srcVal")),
							   "Remap", "Remaps a value from a source range to a destination range")),
				new Category("Basic Math", "Add/subtract/multiply/divide",
					new Option((g, r) => new SimpleNode(r, "'f1' + 'f2'", "Add", new P("f1"), new P("f2")),
							   "Add", "Adds two values together"),
					new Option((g, r) => new SimpleNode(r, "'f1' - 'f2'", "Subtract", new P("f1"), new P("f2")),
							   "Subtract", "Subtracts the second value from the first"),
					new Option((g, r) => new SimpleNode(r, "'f1' * 'f2'", "Multiply", new P("f1"), new P("f2")),
							   "Multiply", "Multiplies two values together"),
					new Option((g, r) => new SimpleNode(r, "'f1' / 'f2'", "Divide", new P("f1"), new P("f2")),
							   "Divide", "Divides the first value by the second"),
					Option.TwoVarFunc("pow", "Pow", "Raises a value to an exponent",
									  "value", float.NaN, "exponent", 1.0f),
					Option.OneVarFunc("sqrt", "Square Root", "Square root"),
					Option.OneVarFunc("log", "Logarithm", "Logarithm base e")),
				new Category("Trig",
					Option.OneVarFunc("sin", "Sin", "A sine wave"),
					Option.OneVarFunc("cos", "Cos", "A cosine wave"),
					Option.OneVarFunc("tan", "Tan", "Tangent"),
					Option.OneVarFunc("acos", "Inverse Cos", "Inverse of the cosine wave"),
					Option.OneVarFunc("asin", "Inverse Sin", "Inverse of the sine wave"),
					Option.OneVarFunc("atan", "Inverse Tan", "Inverse of \"tan\""),
					Option.TwoVarFunc("atan2", "Inverse Tan 2", "Inverse of \"tan\" that takes individual x and y",
									  "y", float.NaN, "x")),
				new Category("Numeric",
					Option.OneVarFunc("frac", "Fractional Part", "The fractional part of a value"),
					Option.OneVarFunc("trunc", "Integer Part", "The integer part of a value"),
					Option.OneVarFunc("ceil", "Ceiling", "Rounds a value up towards positive infinity"),
					Option.OneVarFunc("floor", "Floor", "Rounds a value down towards negative infinity"),
					Option.OneVarFunc("round", "Round to Integer", "Rounds a value to the nearest integer"),
					Option.OneVarFunc("sign", "Sign", "Returns -1, 0, or 1 depending on the value's sign"),
					Option.OneVarFunc("abs", "Abs", "Absolute value"),
					Option.TwoVarFunc("max", "Max", "Gets the largest of two values"),
					Option.TwoVarFunc("min", "Min", "Gets the smallest of two values"),
					Option.ThreeVarFunc("clamp", "Clamp", "Keeps a value between a min and a max",
										"f", float.NaN, "low", 0.0f, "high", 1.0f)),
				new Option((g, r) => new CustomExprNode(r, "$1"), "Custom Expression"),
				new Option((g, r) => new TexCoordNode(r, 0), "Tex Coord", "UV x/y/z"),
				new Option((g, r) => new ParamNode_Float(r, new FloatParamInfo("MyVar")), "Scalar Parameter"),
				new Option((g, r) => new ParamNode_Texture2D(r, new Texture2DParamInfo("MyTex")),
						   "Tex2D Parameter", "Gets the Red value of a texture"),
				new Option((g, r) => new SubGraphNode(r), "Sub-graph", "Get the output of another graph"),
			};
		}
	}
}