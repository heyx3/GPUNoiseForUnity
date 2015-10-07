using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace GPUNoise
{
	public static class TextureGenerator
	{
		[MenuItem("GPU Noise/Generate texture with graph")]
		public static void GenerateTexture()
		{
			ScriptableObject.CreateInstance<TextureGeneratorOptionsWindow>().Show();
		}
	}
}