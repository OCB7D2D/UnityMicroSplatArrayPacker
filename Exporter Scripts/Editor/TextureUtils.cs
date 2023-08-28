using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OcbMicroSplat
{
	public static class TextureUtils
	{

		public static float AdjustChannel(float colour,
				float brightness, float contrast, float gamma)
		{
			return Mathf.Pow(colour, gamma) * contrast + brightness;
		}

		public static void BrightnessContrast(Texture2D tex,
				float brightness = 1f, float contrast = 1f, float gamma = 1f)
		{
			float adjustedBrightness = brightness - 1.0f;

			Color[] pixels = tex.GetPixels();

			for (int i = 0; i < pixels.Length; i++)
			{
				var p = pixels[i];
				p.r = AdjustChannel(p.r, adjustedBrightness, contrast, gamma);
				p.g = AdjustChannel(p.g, adjustedBrightness, contrast, gamma);
				p.b = AdjustChannel(p.b, adjustedBrightness, contrast, gamma);
				pixels[i] = p;
			}

			tex.SetPixels(pixels);
			tex.Apply();

		}

		public class PixelData
		{

			public Color[] Pixels;
			public int Width = 2048;
			public int Height = 2048;

			public PixelData(Texture2D texture)
			{
				Pixels = texture.GetPixels(0);
				Width = texture.width;
				Height = texture.height;
			}

			public Color GetPixel(int x, int y)
			{
				return Pixels[PixelAt(x, y)];
			}

			public void SetPixel(int x, int y, Color col)
			{
				Pixels[PixelAt(x, y)] = col;
			}

			public int PixelAt(int x, int y)
			{
				x = Math.Max(0, Math.Min(Width - 1, x));
				y = Math.Max(0, Math.Min(Height - 1, y));
				return x + y * Width;
			}

		}

		// ####################################################################
		// ####################################################################

		public static float PixelIntensity(Color pixel)
			=> (pixel.r + pixel.g + pixel.b) / 3.0f;

		public static Texture2D CreateNormalFromHeight(Texture2D height, float bias = 8f)
		{
			var src = new PixelData(height);
			for (int x = 0; x < src.Width; x++)
			{
				for (int y = 0; y < src.Height; y++)
				{
					// surrounding pixels
					Color TL = src.GetPixel(x - 1, y - 1);
					Color TT = src.GetPixel(x + 0, y - 1);
					Color TR = src.GetPixel(x + 1, y - 1);
					Color LL = src.GetPixel(x - 1, y + 0);
					// Color ME = src.GetPixel(x + 0, y + 0);
					Color RR = src.GetPixel(x + 1, y + 0);
					Color BL = src.GetPixel(x - 1, y + 1);
					Color BB = src.GetPixel(x + 0, y + 1);
					Color BR = src.GetPixel(x + 1, y + 1);

					float ITL = PixelIntensity(TL);
					float ITT = PixelIntensity(TT);
					float ITR = PixelIntensity(TR);
					float ILL = PixelIntensity(LL);
					// float IME = PixelIntensity(ME);
					float IRR = PixelIntensity(RR);
					float IBL = PixelIntensity(BL);
					float IBB = PixelIntensity(BB);
					float IBR = PixelIntensity(BR);

					// sobel filter
					float dX = (ITR + 2.0f * IRR + IBR) - (ITL + 2.0f * ILL + IBL);
					float dY = (IBL + 2.0f * IBB + IBR) - (ITL + 2.0f * ITT + ITR);
					float dZ = bias;

					var v = new Vector3(dX, dY, dZ).normalized;
					// transform -1 - 1 to 0 - 1
					v = (v + Vector3.one) / 2.0f;
					// src.SetPixel(x, y, new Color(v.x, v.y, v.z));
					src.SetPixel(x, y, new Color(v.z, v.y, v.z, v.x));
				}
			}
			var cpy = new Texture2D(height.width, height.height);
			cpy.SetPixels(src.Pixels, 0);
			cpy.Apply(true, false);
			return cpy;
		}

		// ####################################################################
		// ####################################################################

		public static Texture2D CreateNormalFromAlbedo(Texture2D albedo,
			float normalStrength = 0.5f, float factor = 1, bool compressed = false)
		{

			Color[] pixels = new Color[albedo.width * albedo.height];
			Texture2D texNormal = new Texture2D(albedo.width, albedo.height, TextureFormat.RGBA32, true, true);
			Vector3 vScale = new Vector3(0.3333f, 0.3333f, 0.3333f);

			// TODO: would be faster using pixel array, instead of getpixel
			for (int y = 0; y < albedo.height; y++)
			{
				for (int x = 0; x < albedo.width; x++)
				{
					Color tc = albedo.GetPixel(x - 1, y - 1);
					Vector3 cSampleNegXNegY = new Vector3(tc.r, tc.g, tc.g);
					tc = albedo.GetPixel(x, y - 1);
					Vector3 cSampleZerXNegY = new Vector3(tc.r, tc.g, tc.g);
					tc = albedo.GetPixel(x + 1, y - 1);
					Vector3 cSamplePosXNegY = new Vector3(tc.r, tc.g, tc.g);
					tc = albedo.GetPixel(x - 1, y);
					Vector3 cSampleNegXZerY = new Vector3(tc.r, tc.g, tc.g);
					tc = albedo.GetPixel(x + 1, y);
					Vector3 cSamplePosXZerY = new Vector3(tc.r, tc.g, tc.g);
					tc = albedo.GetPixel(x - 1, y + 1);
					Vector3 cSampleNegXPosY = new Vector3(tc.r, tc.g, tc.g);
					tc = albedo.GetPixel(x, y + 1);
					Vector3 cSampleZerXPosY = new Vector3(tc.r, tc.g, tc.g);
					tc = albedo.GetPixel(x + 1, y + 1);
					Vector3 cSamplePosXPosY = new Vector3(tc.r, tc.g, tc.g);
					float fSampleNegXNegY = Vector3.Dot(cSampleNegXNegY, vScale);
					float fSampleZerXNegY = Vector3.Dot(cSampleZerXNegY, vScale);
					float fSamplePosXNegY = Vector3.Dot(cSamplePosXNegY, vScale);
					float fSampleNegXZerY = Vector3.Dot(cSampleNegXZerY, vScale);
					float fSamplePosXZerY = Vector3.Dot(cSamplePosXZerY, vScale);
					float fSampleNegXPosY = Vector3.Dot(cSampleNegXPosY, vScale);
					float fSampleZerXPosY = Vector3.Dot(cSampleZerXPosY, vScale);
					float fSamplePosXPosY = Vector3.Dot(cSamplePosXPosY, vScale);
					float edgeX = (fSampleNegXNegY - fSamplePosXNegY) * 0.25f + (fSampleNegXZerY - fSamplePosXZerY) * 0.5f + (fSampleNegXPosY - fSamplePosXPosY) * 0.25f;
					float edgeY = (fSampleNegXNegY - fSampleNegXPosY) * 0.25f + (fSampleZerXNegY - fSampleZerXPosY) * 0.5f + (fSamplePosXNegY - fSamplePosXPosY) * 0.25f;
					Vector2 vEdge = new Vector2(edgeX, edgeY) * normalStrength;
					Vector3 norm = new Vector3(vEdge.x, vEdge.y, 1.0f).normalized;

					if (compressed)
					{
						var r = norm.x * 0.5f + 0.5f;
						var g = norm.y * 0.5f + 0.5f;
						g += factor;
						r = Mathf.Clamp01(r);
						g = Mathf.Clamp01(g);
						pixels[x + y * albedo.width] = new Color(1f, g, g, r);
					}
					else
					{
						pixels[x + y * albedo.width].a = norm.x * 0.5f + 0.5f;
						pixels[x + y * albedo.width].g = norm.y * 0.5f + 0.5f;
						pixels[x + y * albedo.width].r = norm.z * 0.5f + 0.5f;
						pixels[x + y * albedo.width].b = norm.z * 0.5f + 0.5f;
						pixels[x + y * albedo.width].r *= factor;
						pixels[x + y * albedo.width].g *= factor;
						pixels[x + y * albedo.width].b *= factor;
						pixels[x + y * albedo.width].a *= factor;
					}
				} // for x
			} // for y

			texNormal.SetPixels(pixels);
			texNormal.Apply();

			return texNormal;


			// var cpy = new Texture2D(albedo.width, albedo.height);
			// for (int m = 0; m < albedo.mipmapCount; m++)
			// {
			//     var pixels = albedo.GetPixels(m);
			//     cpy.SetPixels(pixels, m);
			// }
			// return cpy;
		}

		// ####################################################################
		// ####################################################################

		public static Texture2D CreateHeightFromNormal(Texture2D normal)
		{
			var cpy = new Texture2D(normal.width, normal.height);
			for (int m = 0; m < normal.mipmapCount; m++)
			{
				var pixels = normal.GetPixels(m);
				for (int i = 0; i < pixels.Length; i++)
				{
					float x = pixels[i].a * 2f - 1f;
					float y = pixels[i].g * 2f - 1f;
					var ao = 1 - Mathf.Sqrt(x * x + y * y);
					pixels[i] = new Color(ao, ao, ao, 1);

				}
				cpy.SetPixels(pixels, m);
			}
			cpy.Apply(false, false);
			return cpy;
		}

		public static Texture2D CreateOcclusionFromNormal(Texture2D normal)
		{
			var cpy = new Texture2D(normal.width, normal.height);
			for (int m = 0; m < normal.mipmapCount; m++)
			{
				var pixels = normal.GetPixels(m);
				for (int i = 0; i < pixels.Length; i++)
				{
					float x = pixels[i].a * 2f - 1f;
					float y = pixels[i].g * 2f - 1f;
					var ao = 1 - Mathf.Sqrt(x * x + y * y);
					// if (i % 935000 == 0) Debug.Log($"{ao} vs {pixels[i].r}, {pixels[i].g}, {pixels[i].b}, {pixels[i].a} => {ao}");
					pixels[i] = new Color(ao, ao, ao, 1);

				}
				cpy.SetPixels(pixels, m);
			}
			cpy.Apply(false, false);
			return cpy;
		}

		public static Texture2D CreateSmoothnessFromNormal(Texture2D normal)
		{
			var cpy = new Texture2D(normal.width, normal.height);
			for (int m = 0; m < normal.mipmapCount; m++)
			{
				var pixels = normal.GetPixels(m);
				for (int i = 0; i < pixels.Length; i++)
				{
					var col = pixels[i];
					float x = pixels[i].a * 2f - 1f;
					float y = pixels[i].g * 2f - 1f;
					col.r = col.g = col.b = Mathf.Max(
						Mathf.Abs(x), Mathf.Abs(y));
					col.a = 1f;
					pixels[i] = col;
				}
				cpy.SetPixels(pixels, m);
			}
			cpy.Apply(false, false);
			return cpy;
		}

	}

}
