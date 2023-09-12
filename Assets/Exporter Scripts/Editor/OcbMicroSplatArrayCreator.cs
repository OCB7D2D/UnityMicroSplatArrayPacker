using UnityEditor;
using UnityEngine;

namespace OcbMicroSplat
{

    public static class OcbMicroSplatArrayCreator
    {

        static int Width = 2048;
        static int Height = 2048;

        public static void CopyTextureAlbedo(OcbMicroSplatArrayEntry cfg,
            Texture2DArray dst, int i, bool linear = true)
        {
            Texture2D tmp = new Texture2D(Width, Height,
                TextureFormat.RGBA32, true, false);
            float NormFactor = 1; float NormOffset = 0;
            // Optimize height map
            if (cfg.Height != null)
            {
                // Get stats about heights
                float min = 1; float max = 0;
                float sum = 0; float avg = 0;
                // Calculate some info for normalization
                // Helps with very sharp blend borders
                if (cfg.IsHeightCentered || cfg.IsHeightNormalized)
                {
                    var heights = cfg.Height?.GetPixels(0);
                    for (int p = 0; p < heights.Length; p++)
                    {
                        min = Mathf.Min(min, heights[p].g);
                        max = Mathf.Max(max, heights[p].g);
                        sum += heights[p].g;
                    }
                    avg = sum / heights.Length;
                }
                // Move average to center (0.5)
                if (cfg.IsHeightCentered == true)
                {
                    NormOffset = 0.5f - avg;
                    if (min + NormOffset < 0) NormOffset += min + NormOffset;
                    if (max + NormOffset > 1) NormOffset -= max + NormOffset - 1;
                    // min += NormOffset; max += NormOffset; avg += NormOffset;
                }
                // Stretch min/max into range 0-1
                if (cfg.IsHeightNormalized == true)
                {
                    // Avoid division by zero
                    if (max > min)
                    {
                        NormFactor = 1f / (max - min);
                        NormOffset = -min * NormFactor;
                    }
                }
            }
            for (int m = 0; m < tmp.mipmapCount; m++)
            {
                Color[] pixels = new Color[Width * Height];
                var albedo = cfg.Albedo?.GetPixels(m);
                var heights = cfg.Height?.GetPixels(m);
                for (int p = 0; p < albedo.Length; p++)
                {
                    Color color = new Color(0, 0, 0, 0.5f);
                    var diff = albedo?[p] ?? Color.clear;
                    color.a = heights?[p].g ?? 0.5f;
                    color.a *= NormFactor + NormOffset;
                    color.r = diff.r;
                    color.g = diff.g;
                    color.b = diff.b;
                    pixels[p] = color;
                }
                tmp.SetPixels(pixels, m);
            }
            EditorUtility.CompressTexture(tmp, TextureFormat.DXT5, 100);
            for (int m = 0; m < tmp.mipmapCount; m++)
                Graphics.CopyTexture(tmp, 0, m, dst, i, m);
        }

        public static void CopyTextureNormal(OcbMicroSplatArrayEntry cfg, Texture2DArray dst, int i)
        {
            Texture2D tmp = new Texture2D(Width, Height,
                TextureFormat.RGBA32, true, true);
            for (int m = 0; m < cfg.Normal.mipmapCount; m++)
            {
                var pixels = cfg.Normal.GetPixels(m);
                for (int p = 0; p < pixels.Length; p++)
                {
                    var pixel = pixels[p];
                    if (cfg.IsNormalInverted) pixel.g = 1f - pixel.g;
                    if (cfg.IsNormalSwitched) (pixel.g, pixel.a) = (pixel.a, pixel.g);
                    pixel.r = pixel.b = 0f; // Channels are unused
                    pixels[p] = pixel;

                }
                // pixels[p] = apply(pixels[p]);
                // c => new Color(0, c.a, c.b, c.g), 
                
                tmp.SetPixels(pixels, m);
            }
            EditorUtility.CompressTexture(tmp, TextureFormat.DXT5, 100);
            for (int m = 0; m < cfg.Normal.mipmapCount; m++)
                Graphics.CopyTexture(tmp, 0, m, dst, i, m);
        }

        public static void CopyTextureSHAO(OcbMicroSplatArrayEntry cfg,
            Texture2DArray dst, int i, bool linear = true)
        {
            Texture2D tmp = new Texture2D(Width, Height,
                TextureFormat.RGBA32, true, linear);
            for (int m = 0; m < tmp.mipmapCount; m++)
            {
                Color[] pixels = new Color[Width * Height];
                var heights = cfg.Height?.GetPixels(m);
                var smoothness = cfg.Smoothness?.GetPixels(m);
                var occlusion = cfg.Occlusion?.GetPixels(m);
                for (int p = 0; p < heights.Length; p++)
                {
                    var color = pixels[p];
                    if (smoothness != null) color.g = smoothness[p][1];
                    // Height is stored in albedo alpha channel
                    // if (heights != null) color.a = heights[p][1];
                    if (occlusion != null) color.a = occlusion[p][1];
                    // Invert the roughness if configured to do so
                    if (cfg.IsRoughness) color.g = 1f - color.g;
                    pixels[p] = color;
                }
                tmp.SetPixels(pixels, m);
            }
            EditorUtility.CompressTexture(tmp, TextureFormat.DXT5, 100);
            for (int m = 0; m < tmp.mipmapCount; m++)
                Graphics.CopyTexture(tmp, 0, m, dst, i, m);
        }

        public static void CreateTextureArrays(OcbMicroSplatArray cfg, string path)
        {

            var textures = cfg.Textures;

            var DiffPath = path.Replace(".asset", "_diff_tarray.asset");
            var NormPath = path.Replace(".asset", "_norm_tarray.asset");
            var ShaoPath = path.Replace(".asset", "_shao_tarray.asset");

            var DiffArray = new Texture2DArray(Width, Height, textures.Length, TextureFormat.DXT5, true, false);
            var NormArray = new Texture2DArray(Width, Height, textures.Length, TextureFormat.DXT5, true, true);
            var ShaoArray = new Texture2DArray(Width, Height, textures.Length, TextureFormat.DXT5, true, true);

            DiffArray.wrapMode = NormArray.wrapMode = ShaoArray.wrapMode = cfg.WrapMode;
            DiffArray.filterMode = NormArray.filterMode = ShaoArray.filterMode = cfg.FilterMode;
            DiffArray.anisoLevel = NormArray.anisoLevel = ShaoArray.anisoLevel = cfg.AnisoLevel;

            for (int i = 0; i < textures.Length; i++)
            {
                OcbMicroSplatArrayEntry texture = textures[i];

                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture.Albedo))) texture.Albedo = null;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture.Normal))) texture.Normal = null;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture.Height))) texture.Height = null;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture.Smoothness))) texture.Smoothness = null;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture.Occlusion))) texture.Occlusion = null;

                // var albedo = texture.Albedo;
                // var normal = texture.Normal;
                // var height = texture.Height;
                // var smoothness = texture.Smoothness;
                // var occlusion = texture.Occlusion;

                if (texture.Normal == null)
                {
                    if (texture.Height != null)
                    {
                        Debug.LogWarning("Create normal from height map");
                        texture.Normal = TextureUtils.CreateNormalFromHeight(texture.Height);
                    }
                    else if (texture.Albedo != null)
                    {
                        Debug.LogWarning("Create normal from albedo map");
                        texture.Normal = TextureUtils.CreateNormalFromAlbedo(texture.Albedo);
                    }
                }

                if (texture.Height == null)
                {
                    if (texture.Normal != null)
                    {
                        Debug.LogWarning("Create height from normal map");
                        texture.Height = TextureUtils.CreateHeightFromNormal(texture.Normal);
                    }
                }

                if (texture.Smoothness == null)
                {
                    if (texture.Normal != null)
                    {
                        Debug.LogWarning("Create smothness from normal map");
                        texture.Smoothness = TextureUtils.CreateSmoothnessFromNormal(texture.Normal);
                    }
                }

                if (texture.Occlusion == null)
                {
                    if (texture.Normal != null)
                    {
                        Debug.LogWarning("Create occlusion from normal map");
                        texture.Occlusion = TextureUtils.CreateOcclusionFromNormal(texture.Normal);
                    }
                }

                // var albedo = texture.Albedo;
                // var normal = texture.Normal;
                // var height = texture.Height;


                CopyTextureAlbedo(texture, DiffArray, i, false);
                CopyTextureNormal(texture, NormArray, i);
                CopyTextureSHAO(texture, ShaoArray, i, true);

                // texture.Albedo = albedo;
                // texture.Normal = normal;
                // texture.Height = height;
                // texture.Smoothness = smoothness;
                // texture.Occlusion = occlusion;

            }

            CreateArrayAsset(DiffArray, DiffPath);
            CreateArrayAsset(NormArray, NormPath);
            CreateArrayAsset(ShaoArray, ShaoPath);
        }

        static Shader mergeInChannelShader;

        static void MergeInChannel(RenderTexture target, int targetChannel,
            Texture merge, int mergeChannel, bool linear, bool invert = false)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Merge");
            RenderTexture resRT = new RenderTexture(target.width, target.height, 0, RenderTextureFormat.ARGB32, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
            resRT.DiscardContents();
            if (mergeInChannelShader == null)
            {
                mergeInChannelShader = Shader.Find("Hidden/MicroSplat/MergeInChannel");
            }
            if (mergeInChannelShader == null)
            {
                Debug.LogError("Could not find shader for merge");
                GameObject.DestroyImmediate(resRT);
                return;
            }
            Material genMat = new Material(mergeInChannelShader);
            genMat.SetInt("_TargetChannel", targetChannel);
            genMat.SetInt("_MergeChannel", mergeChannel);
            genMat.SetInt("_Invert", invert ? 1 : 0);
            genMat.SetTexture("_TargetTex", target);

            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            Graphics.Blit(merge, resRT, genMat);
            Graphics.Blit(resRT, target);
            GL.sRGBWrite = false;

            resRT.Release();
            GameObject.DestroyImmediate(resRT);
            GameObject.DestroyImmediate(genMat);

            UnityEngine.Profiling.Profiler.EndSample();
        }


        static void CreateArrayAsset(Texture2DArray array, string path)
        {

            array.Apply(false, true);


            // On 2020.3LTS, the terrain turns black if you use the existing array,
            // but on previous versions, the reference gets broken on every material
            // if you don't use the existing array. I suspect this is some change to how
            // the asset database works, and suspect this isn't the end of the trouble here.
            //#if !UNITY_2020_3_OR_NEWER
            var existing = AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
            if (existing != null)
            {
                array.name = existing.name;

                EditorUtility.CopySerialized(array, existing);
            }
            else
            //#endif
            {
                AssetDatabase.CreateAsset(array, path);
            }
        }

    }
}
