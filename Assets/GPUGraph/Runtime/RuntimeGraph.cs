using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GPUGraph
{
	/// <summary>
	/// Stores a graph at run-time in the form of a Shader/Material.
	/// Note that modifying parameters is best done with the built-in methods,
	///     not by modifying the param lists directly.
	/// </summary>
	[Serializable]
	public class RuntimeGraph
	{
		public Shader GraphShader;
		public List<FloatParamKVP> FloatParams = new List<FloatParamKVP>();
		public List<Tex2DParamKVP> Tex2DParams = new List<Tex2DParamKVP>();
		public float UVz = 0.0f;


		public Material GraphMat
		{
			get
			{
				if (graphMat == null)
				{
					if (GraphShader == null)
					{
						throw new InvalidOperationException("Something happened to a RuntimeGraph's shader! " +
															 "View the RuntimeGraph in the Inspector to regenerate it.");
					}
					else
					{
						graphMat = new Material(GraphShader);
						UpdateAllParams();
					}
				}

				return graphMat;
			}
		}
		private Material graphMat = null;


		/// <summary>
		/// Outputs noise into the screen or whatever RenderTexture is currently active.
		/// </summary>
		public void GenerateToCurrentFramebuffer()
		{
			UpdateAllParams();
			GraphUtils.GenerateToTexture(RenderTexture.active, GraphMat);
		}
		/// <summary>
		/// Outputs noise into the given RenderTexture.
		/// </summary>
		public void GenerateToFramebuffer(RenderTexture outTex)
		{
			UpdateAllParams();
			GraphUtils.GenerateToTexture(outTex, GraphMat);
		}

		/// <summary>
		/// Outputs noise into the given Texture2D.
		/// </summary>
		/// <param name="leaveReadable">
		/// Whether to leave the texture data readable on the CPU after the operation.
		/// </param>
		public void GenerateToTexture(Texture2D outTex, bool leaveReadable = false)
		{
			UpdateAllParams();
			RenderTexture tempTex = RenderTexture.GetTemporary(outTex.width, outTex.height);
			GraphUtils.GenerateToTexture(tempTex, GraphMat, outTex, leaveReadable);
			RenderTexture.ReleaseTemporary(tempTex);
		}
		/// <summary>
		/// Generates to a texture of the given size.
		/// </summary>
		/// <param name="leaveReadable">
		/// Whether to leave the texture data readable on the CPU after the operation.
		/// </param>
		public Texture2D GenerateToTexture(int width, int height,
										   TextureFormat fmt = TextureFormat.RGBAFloat,
										   FilterMode filtering = FilterMode.Bilinear,
										   bool mipmaps = true,
										   bool leaveReadable = false)
		{
			UpdateAllParams();
			Texture2D t = new Texture2D(width, height, fmt, mipmaps);
			t.filterMode = filtering;
			GenerateToTexture(t, leaveReadable);
			return t;
		}

		/// <summary>
		/// Outputs noise into the given array.
		/// </summary>
		public void GenerateToArray(float[,] outData)
		{
			UpdateAllParams();
			GraphUtils.GenerateToArray(outData, GraphMat);
		}


		#region Param getters/setters

		/// <summary>
		/// Returns -1 if the param doesn't exist.
		/// </summary>
		public int IndexOfParam_Float(string name)
		{
			for (int i = 0; i < FloatParams.Count; ++i)
				if (FloatParams[i].Key == name)
					return i;
			return -1;
		}
		/// <summary>
		/// Returns -1 if the param doesn't exist.
		/// </summary>
		public int IndexOfParam_Tex2D(string name)
		{
			for (int i = 0; i < Tex2DParams.Count; ++i)
				if (Tex2DParams[i].Key == name)
					return i;
			return -1;
		}

		public float GetParam_Float(string name)
		{
			return FloatParams[IndexOfParam_Float(name)].Value;
		}
		public Texture2D GetParam_Tex2D(string name)
		{
			for (int i = 0; i < Tex2DParams.Count; ++i)
				if (Tex2DParams[i].Key == name)
					return Tex2DParams[i].Value;

			UnityEngine.Assertions.Assert.IsTrue(false);
			return null;
		}

		public bool SetParam_Float(string name, float val)
		{
			int i = IndexOfParam_Float(name);
			if (i >= 0)
				SetParam_Float(i, val);
			return i >= 0;
		}
		public bool SetParam_Tex2D(string name, Texture2D val)
		{
			int i = IndexOfParam_Tex2D(name);
			if (i >= 0)
				SetParam_Tex2D(i, val);
			return i >= 0;
		}

		public void SetParam_Float(int index, float val)
		{
			FloatParams[index].Value = val;
			graphMat.SetFloat(FloatParams[index].Key, val);
		}
		public void SetParam_Tex2D(int index, Texture2D val)
		{
			Tex2DParams[index].Value = val;
			graphMat.SetTexture(Tex2DParams[index].Key, val);
		}

		/// <summary>
		/// Should be called if changes are made to this instance's parameter lists by external code.
		/// Otherwise, those new parameter values won't actually be used when generating noise.
		/// </summary>
		private void UpdateAllParams()
		{
			Material m = GraphMat;
			foreach (FloatParamKVP floatParam in FloatParams)
				m.SetFloat(floatParam.Key, floatParam.Value);
			foreach (Tex2DParamKVP texParam in Tex2DParams)
				m.SetTexture(texParam.Key, texParam.Value);
			m.SetFloat(GraphUtils.Param_UVz, UVz);
		}

		#endregion


		#region Editor-only fields
		#if UNITY_EDITOR

		//The following should all be ignored; it's only used by the Inspector window.

		#region Helper classes

		[Serializable]
		public class _SerializableFloatParamInfo
		{
			public bool IsSlider;
			public float SliderMin, SliderMax;
			public float DefaultValue;
		}
		[Serializable]
		public class _SerializableTex2DParamInfo
		{
			public Texture2D DefaultValue;
		}

		[Serializable]
		public class _SerializableFloatParamKVP
		{
			public string Key;
			public _SerializableFloatParamInfo Value;

			public _SerializableFloatParamKVP() { }
			public _SerializableFloatParamKVP(string key, _SerializableFloatParamInfo value)
				{ Key = key; Value = value; }
		}
		[Serializable]
		public class _SerializableTex2DParamKVP
		{
			public string Key;
			public _SerializableTex2DParamInfo Value;

			public _SerializableTex2DParamKVP() { }
			public _SerializableTex2DParamKVP(string key, _SerializableTex2DParamInfo value)
				{ Key = key; Value = value; }
		}

		#endregion

		public string _ShaderFile = null;
		public string _GraphFile = null;
		public Material _PreviewMat = null;
		public Texture2D _PreviewTex = null;
		public float _PreviewTexScale = 1.0f;
		public int _PreviewTexWidth = 256,
				   _PreviewTexHeight = 256;
		public List<_SerializableFloatParamKVP> _FloatParams = new List<_SerializableFloatParamKVP>();
		public List<_SerializableTex2DParamKVP> _Tex2DParams = new List<_SerializableTex2DParamKVP>();

		#endif
		#endregion
	}


	/// <summary>
	/// A float parameter's name and value.
	/// </summary>
	[Serializable]
	public class FloatParamKVP
	{
		public string Key;
		public float Value;

		public FloatParamKVP() { }
		public FloatParamKVP(string key, float value) { Key = key; Value = value; }
	}
	/// <summary>
	/// A 2D texture parameter's name and value.
	/// </summary>
	[Serializable]
	public class Tex2DParamKVP
	{
		public string Key;
		public Texture2D Value;

		public Tex2DParamKVP() { }
		public Tex2DParamKVP(string key, Texture2D value) { Key = key; Value = value; }
	}
}