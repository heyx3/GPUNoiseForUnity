using System;
using UnityEngine;

namespace GPUGraph.Editor
{
	public static class GUIUtil
	{
		private static Texture2D whitePixelTex = null;

		public static void DrawLine(Vector2 a, Vector2 b, float thickness, Color col)
		{
			//Taken from: http://wiki.unity3d.com/index.php?title=DrawLine


			// Save the current GUI matrix, since we're going to make changes to it.
			Matrix4x4 matrix = GUI.matrix;

			// Generate a single pixel texture if it doesn't exist
			if (whitePixelTex == null)
			{
				whitePixelTex = new Texture2D(1, 1);
				whitePixelTex.SetPixel(0, 0, Color.white);
				whitePixelTex.Apply();
			}

			// Store current GUI color, so we can switch it back later,
			// and set the GUI color to the color parameter
			Color savedColor = GUI.color;
			GUI.color = col;

			// Determine the angle of the line.
			float angle = Vector3.Angle(b - a, Vector2.right);

			// Vector3.Angle always returns a positive number.
			// If pointB is above pointA, then angle needs to be negative.
			if (a.y > b.y)
			{
				angle = -angle;
			}

			// Use ScaleAroundPivot to adjust the size of the line.
			// We could do this when we draw the texture, but by scaling it here we can use
			//  non-integer values for the width and length (such as sub 1 pixel widths).
			// Note that the pivot point is at +.5 from a.y, this is so that the width of the line
			//  is centered on the origin at point a.
			GUIUtility.ScaleAroundPivot(new Vector2((b - a).magnitude, thickness),
										new Vector2(a.x, a.y + 0.5f));

			// Set the rotation for the line.
			//  The angle was calculated with point a as the origin.
			GUIUtility.RotateAroundPivot(angle, a);

			// Finally, draw the actual line.
			// We're really only drawing a 1x1 texture from point a.
			// The matrix operations done with ScaleAroundPivot and RotateAroundPivot will make this
			//  render with the proper width, length, and angle.
			GUI.DrawTexture(new Rect(a.x, a.y, 1.0f, 1.0f), whitePixelTex);

			// We're done.  Restore the GUI matrix and GUI color to whatever they were before.
			GUI.matrix = matrix;
			GUI.color = savedColor;
		}
	}
}