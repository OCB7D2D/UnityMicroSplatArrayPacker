using UnityEngine;

namespace OcbMicroSplat
{

    [CreateAssetMenu(fileName = "MicroSplatArray", menuName = "MicroSplat Texture Array", order = 2)]
    [System.Serializable]
    public class OcbMicroSplatArray : ScriptableObject
    {

        [HideInInspector]
        public int Size = 0;

        [HideInInspector]
        public string Path = "";

        [HideInInspector]
        public TextureWrapMode WrapMode = TextureWrapMode.Repeat;

        [HideInInspector]
        public FilterMode FilterMode = FilterMode.Trilinear;

        [HideInInspector]
        public int AnisoLevel = 8;

        [HideInInspector]
        public OcbMicroSplatArrayEntry[] Textures = null;

    }

    [System.Serializable]
    public class OcbMicroSplatArrayEntry
    {

        public Texture2D Albedo;
        public Texture2D Height;
        public Texture2D Normal;
        public bool IsNormalSwitched = true;
        public bool IsNormalInverted = false;
        public bool IsHeightCentered = false;
        public bool IsHeightNormalized = true;
        public Texture2D Smoothness;
        public bool IsRoughness = false;
        public Texture2D Metallic;
        public Texture2D Occlusion;
        public Texture2D Emission;

        public bool HasTextures()
        {
            return Albedo != null
                || Height != null
                || Normal != null
                || Smoothness != null
                || Metallic != null
                || Occlusion != null
                || Emission != null;
        }

        public void Clear()
        {
            Albedo = null;
            Height = null;
            Normal = null;
            Smoothness = null;
            IsRoughness = false;
            Metallic = null;
            Occlusion = null;
            Emission = null;
        }
    }

}

