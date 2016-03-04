using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using StringBuilder = System.Text.StringBuilder;

namespace GPUGraph
{
	/// <summary>
	/// A shader function that takes floats as input and outputs another float.
	/// </summary>
	public class Func
	{
		public struct Param
		{
			public string Name;
			public float DefaultValue;

			public bool HasDefaultValue { get { return !Single.IsNaN(DefaultValue); } }


			public Param(string name, float defaultValue = Single.NaN)
			{
				Name = name;
				DefaultValue = defaultValue;
			}
		}


		public static bool IsValidHLSLName(string name)
		{
			if (name.Length == 0)
			{
				return false;
			}

			for (int i = 0; i < name.Length; ++i)
			{
				if (name[i] != '_' &&
					(name[i] < 'a' || name[i] > 'z') &&
					(name[i] < 'A' || name[i] > 'Z') &&
					(i == 0 || name[i] < '0' || name[i] > '9'))
				{
					return false;
				}
			}

			return true;
		}
		public static bool IsValidFloat(string fl)
		{
			if (fl.Length == 0)
			{
				return false;
			}

			for (int i = 0; i < fl.Length; ++i)
			{
				if ((fl[i] < '0' || fl[i] > '9') && fl[i] != '.')
				{
					return false;
				}
			}

			return true;
		}


		/// <summary>
		/// The name of this node/function.
		/// </summary>
		public string Name { get; protected set; }
		/// <summary>
		/// The parameters for the function this Node represents (all of type "float").
		/// </summary>
		public List<Param> Params { get; protected set; }
		/// <summary>
		/// The body of the shader function this Func represents, including the opening/closing braces.
		/// The inputs are parameters to this function called "x0", "x1", etc.
		/// A float value should be returned.
		/// </summary>
		public string Body { get; protected set; }


		/// <summary>
		/// Takes in the full HLSL function declaration and parses information out of it.
		/// There are some differences between this declaration and normal HLSL:
		/// 1) The first line must contain the full function declaration and nothing else
		///		(so no opening brace).
		/// 2) Default values can be set for any parameter.
		/// 3) All inputs must be "float", and the output must be "float" as well.
		/// An example of the first line of a function:
		///	"float Lerp(float start = 0, float end = 1, float t)".
		/// </summary>
		public Func(string shaderFunc)
		{
			//Find the end of the first line (a.k.a. the function declaration) to get the body.
			int endDecl = 0;
			while (shaderFunc[endDecl] != '\n' && shaderFunc[endDecl] != '\r')
				endDecl += 1;
			Body = shaderFunc.Substring(endDecl);
			 
			//Get the first actual line, containing the function declaration.
			shaderFunc = shaderFunc.Replace('\r', '\n');
			string decl = shaderFunc.Split('\n')[0].Trim();
		
			//Split the declaration into individual tokens.
			List<string> tokens = decl.Split(' ', '(', ',', ')').ToList();
			tokens.RemoveAll(s => s.Trim().Length == 0);

			//Check the function name/return type.
			if (tokens[0] != "float")
			{
				throw new ArgumentException("Function must return float, not '" + tokens[0] + "'");
			}
			Name = tokens[1];
			if (!IsValidHLSLName(Name))
			{
				throw new ArgumentException("Function name '" + Name + "' isn't valid!");
			}

			//Check out each of the parameters.
			Params = new List<Param>();
			//Iterate over the tokens and use a FSM to keep track of what kind of token is expected next.
			int state = 0;
			string varName = null;
			for (int i = 2; i < tokens.Count; ++i)
			{
				string str = tokens[i];
				switch (state)
				{
					//Expecting new param declaration.
					case 0:
						if (str != "float")
						{
							throw new ArgumentException("All function params must be of type 'float'");
						}
						else
						{
							state = 1;
						}
					break;

					//Expecting variable name.
					case 1:
						if (!IsValidHLSLName(str))
						{
							throw new ArgumentException("Function param '" + str +
														"' isn't a valid HLSL name");
						}
						else
						{
							varName = str;

							state = 2;
						}
					break;

					//Expecting either a default value declaration or the next variable.
					case 2:
						if (str == "=")
						{
							state = 3;
						}
						else
						{
							Params.Add(new Param(varName));

							//Iterate over this token again but in the "expecting new variable" state.
							state = 0;
							i -= 1;
						}
					break;

					//Expecting the value of the default value for a variable.
					case 3:
						if (!IsValidFloat(str))
						{
							throw new ArgumentException("Default value for param '" + varName +
														"' isn't a valid float");
						}
						else
						{
							Params.Add(new Param(varName, float.Parse(str)));
							state = 0;
						}
					break;

					default:
						throw new InvalidOperationException("Unknown state " + state);
				}
			}
			if (state == 2)
			{
				Params.Add(new Param(varName));
			}
			//Make sure the param declarations didn't end prematurely.
			if (state == 1)
			{
				throw new InvalidOperationException("Function declaration ended before " +
													"providing name for last parameter. Line: " +
													decl);
			}
			else if (state == 3)
			{
				throw new InvalidOperationException("Function declaration ended before " +
													"providing default value for parameter '" +
													varName + "'");
			}
		}
		/// <summary>
		/// Creates a new Func with the given fields.
		/// </summary>
		/// <param name="funcName">The name of the function. Must be a valid HLSL name.</param>
		/// <param name="funcParams">
		/// The parameters to this function. Each must be a valid HLSL name.
		/// </param>
		/// <param name="funcBody">
		/// The body of the function, including the opening and closing braces. Must return a float.
		/// </param>
		public Func(string funcName, List<Param> funcParams, string funcBody)
		{
			if (!IsValidHLSLName(funcName))
			{
				throw new ArgumentException("Function name '" + funcName + "' isn't valid HLSL");
			}
			Name = funcName;

			foreach (Param p in funcParams)
			{
				if (!IsValidHLSLName(p.Name))
				{
					throw new ArgumentException("Function param '" + p.Name + "' isn't valid HLSL");
				}
			}
			Params = funcParams;

			funcBody = funcBody.Trim();
			if (funcBody[0] != '{')
			{
				throw new ArgumentException("Function body doesn't start with an opening brace!");
			}
			if (funcBody[funcBody.Length - 1] != '}')
			{
				throw new ArgumentException("Function body doesn't end with a closing brace!");
			}
			Body = funcBody;
		}
		

		//Some nodes need to offer custom UI behavior.
		[Serializable]
		public abstract class ExtraData { }
		public virtual ExtraData InitCustomGUI() { return null; }
		//Should return whether any values have changed.
		public virtual bool CustomGUI(ExtraData myData) { return false; }

		/// <summary>
		/// Gets the full shader function this Func represents.
		/// </summary>
		public virtual string GetFunctionDecl()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			sb.Append("float ");
			sb.Append(Name);

			sb.Append("(");
			for (int i = 0; i < Params.Count; ++i)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}
				sb.Append("float ");
				sb.Append(Params[i].Name);
			}
			sb.Append(")");

			sb.Append(Body);

			return sb.ToString();
		}
		/// <summary>
		/// Outputs an expression that has the expected output for this Func.
		/// Will not need to be overridden except for special nodes like Param ones.
		/// </summary>
		public virtual string GetInvocation(ExtraData customDat, string paramList)
		{
			return Name + "(" + paramList + ")";
		}

		/// <summary>
		/// Gets any custom parameter declarations for the shader's "Properties" block.
		/// </summary>
		public virtual void GetPropertyDeclarations(ExtraData customDat, StringBuilder shaderText) { }
		/// <summary>
		/// Gets any custom parameter declarations for the Cg part of the shader.
		/// </summary>
		public virtual void GetParamDeclarations(ExtraData customDat, StringBuilder shaderText) { }
	}
}