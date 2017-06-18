using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace GPUGraph
{
	public static class GUIUtil
	{
		private static Texture2D whitePixelTex = null;

		public static Texture2D WhitePixel
		{
			get
			{
				//Generate the texture if it doesn't exist.
				if (whitePixelTex == null)
				{
					whitePixelTex = new Texture2D(1, 1);
					whitePixelTex.SetPixel(0, 0, Color.white);
					whitePixelTex.Apply();
				}

				return whitePixelTex;
			}
		}

		public static void DrawLine(Vector2 a, Vector2 b, float thickness, Color col)
		{
			//Taken from: http://wiki.unity3d.com/index.php?title=DrawLine

			// Save the current GUI matrix, since we're going to make changes to it.
			Matrix4x4 matrix = GUI.matrix;

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
			// Note that the pivot point is at 0.5 ahead of a.y, this is so that the width of the line
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


		/// <summary>
		/// Gets the index of the first element to satisfy the given predicate.
		/// Returns -1 if none were found.
		/// </summary>
		public static int IndexOf<T>(this IEnumerable<T> en, Predicate<T> p)
		{
			int i = 0;
			foreach (T t in en)
			{
				if (p(t))
					return i;
				i += 1;
			}
			return -1;
		}

		public static T Accumulate<ElementType, T>(this IEnumerable<ElementType> en,
												   Func<ElementType, T, T> accumulator,
												   T initial)
		{
			foreach (ElementType el in en)
				initial = accumulator(el, initial);
			return initial;
		}

		public static IEnumerable<DestT> SelectSome<SrcT, DestT>(this IEnumerable<SrcT> en, Func<SrcT, DestT?> converter)
			where DestT : struct
		{
			foreach (SrcT srcT in en)
			{
				DestT? destT = converter(srcT);
				if (destT.HasValue)
					yield return destT.Value;
			}
		}
		public static IEnumerable<DestT> SelectSome<SrcT, DestT>(this IEnumerable<SrcT> en, Func<SrcT, DestT> converter)
			where DestT : class
		{
			foreach (SrcT srcT in en)
			{
				DestT destT = converter(srcT);
				if (destT != null)
					yield return destT;
			}
		}


        public static int WrapNegative(this int i, int maxExclusive)
        {
            return (i % maxExclusive) + maxExclusive;
        }
        public static int Wrap(this int _i, int maxExclusive)
        {
            //http://stackoverflow.com/questions/3417183/modulo-of-negative-numbers
            int i = _i % maxExclusive;
            return (i < 0) ? (i + maxExclusive) : i;
        }

        /// <summary>
        /// Samples 8 nearby values from an array given the UV coordinate.
        /// Nicely handles wrapping or clamping behavior.
        /// Returns the interpolant between the 8 values.
        /// </summary>
        /// <param name="uv">The position in the array to sample from, from 0 to 1.</param>
        /// <param name="wrap">
        /// If true, positions wrap around the ends of the array. If false, positions are clamped to the bounds of the array.
        /// </param>
        public static void Sample<T>(this T[,,] array, int x, int y, int z, bool wrap,
                                     out T out_minXYZ, out T out_maxXminYZ,
                                     out T out_minXmaxYminZ, out T out_maxXYminZ,
                                     out T out_minXYmaxZ, out T out_maxXminYmaxZ,
                                     out T out_minXmaxYZ, out T out_maxXYZ)
        {
            int sizeX = array.GetLength(0),
                sizeY = array.GetLength(1),
                sizeZ = array.GetLength(2);

            //Wrap/clamp the pos.
            if (x < 0)
                x = (wrap ? x.WrapNegative(sizeX) : 0);
            else if (x >= sizeX)
                x = (wrap ? (x % sizeX) : 0);
            if (y < 0)
                y = (wrap ? y.WrapNegative(sizeY) : 0);
            else if (y >= sizeY)
                y = (wrap ? (y % sizeY) : 0);
            if (z < 0)
                z = (wrap ? z.WrapNegative(sizeZ) : 0);
            else if (z >= sizeZ)
                z = (wrap ? (z % sizeZ) : 0);

            //Get the index of the next elements.
            int? moreX = (x < sizeX - 1) ? (x + 1) : (wrap ? 0 : new int?()),
                 moreY = (y < sizeY - 1) ? (y + 1) : (wrap ? 0 : new int?()),
                 moreZ = (z < sizeZ - 1) ? (z + 1) : (wrap ? 0 : new int?());

            //Get the elements.
            out_minXYZ = array[x, y, z];
            out_minXYmaxZ = moreZ.HasValue ?
                                array[x, y, (int)moreZ] :
                                out_minXYZ;
            out_minXmaxYminZ = moreY.HasValue ?
                                   array[x, (int)moreY, z] :
                                   out_minXYZ;
            out_minXmaxYZ = (moreY.HasValue & moreZ.HasValue) ?
                                array[x, (int)moreY, (int)moreZ] :
                                out_minXYZ;
            out_maxXminYZ = moreX.HasValue ?
                                array[(int)moreX, y, z] :
                                out_minXYZ;
            out_maxXminYmaxZ = (moreX.HasValue & moreZ.HasValue) ?
                                   array[(int)moreX, y, (int)moreZ] :
                                   out_minXYZ;
            out_maxXYminZ = (moreX.HasValue & moreY.HasValue) ?
                                   array[(int)moreX, (int)moreY, z] :
                                   out_minXYZ;
            out_maxXYZ = (moreX.HasValue & moreY.HasValue & moreZ.HasValue) ?
                             array[(int)moreX, (int)moreY, (int)moreZ] :
                             out_minXYZ;
        }
        /// <summary>
        /// Samples from a 3D array using trilinear filtering.
        /// </summary>
        public static T Sample<T>(this T[,,] array, Vector3 uv, Func<T, T, float, T> lerp, bool wrap)
        {
            int sizeX = array.GetLength(0),
                sizeY = array.GetLength(1),
                sizeZ = array.GetLength(2);

            uv = new Vector3(uv.x * sizeX, uv.y * sizeY, uv.z * sizeZ);

            int x = (int)uv.x,
                y = (int)uv.y,
                z = (int)uv.z;
            Vector3 t = new Vector3(uv.x - x,
                                    uv.y - y,
                                    uv.z - z);

            //Get the elements to interpolate between.
            T minXYZ, minXYmaxZ, minXmaxYminZ, minXmaxYZ, maxXminYZ, maxXminYmaxZ, maxXYminZ, maxXYZ;
            Sample(array, x, y, z, wrap,
                   out minXYZ, out maxXminYZ, out minXmaxYminZ, out maxXYminZ,
                   out minXYmaxZ, out maxXminYmaxZ, out minXmaxYZ, out maxXYZ);

            //Interpolate.
            return lerp(lerp(lerp(minXYZ, maxXminYZ, t.x),
                             lerp(minXmaxYminZ, maxXYminZ, t.x),
                             t.y),
                        lerp(lerp(minXYmaxZ, maxXminYmaxZ, t.x),
                             lerp(minXmaxYZ, maxXYZ, t.x),
                             t.y),
                        t.z);
        }

        /// <summary>
        /// Up- or down-samples the given 3D array,
        ///     assuming that the change in size isn't more than 2x along each axis.
        /// </summary>
        public static T[,,] Resample<T>(this T[,,] original, Func<T, T, float, T> lerp,
                                         int newSizeX, int newSizeY, int newSizeZ)
        {
            T[,,] newVals = new T[newSizeX, newSizeY, newSizeZ];
            Vector3 scale = new Vector3(1.0f / newSizeX, 1.0f / newSizeY, 1.0f / newSizeZ);
            for (int z = 0; z < newSizeZ; ++z)
            {
                for (int y = 0; y < newSizeY; ++y)
                {
                    for (int x = 0; x < newSizeX; ++x)
                    {
                        Vector3 uv = new Vector3(x * scale.x, y * scale.y, z * scale.z);
                        newVals[x, y, z] = original.Sample(uv, lerp, false);
                    }
                }
            }

            return newVals;
        }
        /// <summary>
        /// Up- or down-samples the given 3D array to any new size.
        /// </summary>
        public static T[,,] ResampleFull<T>(this T[,,] original, Func<T, T, float, T> lerp,
                                            int targetSizeX, int targetSizeY, int targetSizeZ)
        {
            //Keep doubling/halving the array until we get to the target.
            while (original.GetLength(0) != targetSizeX ||
                   original.GetLength(1) != targetSizeY ||
                   original.GetLength(2) != targetSizeZ)
            {
                //If the target size is too big/small, clamp it.
                int newSizeX = Mathf.Clamp(targetSizeX,
                                           original.GetLength(0) / 2,
                                           original.GetLength(0) * 2),
                    newSizeY = Mathf.Clamp(targetSizeY,
                                           original.GetLength(1) / 2,
                                           original.GetLength(1) * 2),
                    newSizeZ = Mathf.Clamp(targetSizeZ,
                                           original.GetLength(2) / 2,
                                           original.GetLength(2) * 2);

                original = Resample(original, lerp, newSizeX, newSizeY, newSizeZ);
            }

            return original;
        }
    }
}