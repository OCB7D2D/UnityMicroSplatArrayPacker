using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace OcbMicroSplat
{

    [ExecuteInEditMode]
    [CustomEditor(typeof(OcbMicroSplatArray))]
    public class OcbMicroSplatArrayEditor : Editor
    {

        private static string[] compressions = new string[]
        {
            "LZMA (default, best size, but slow load)", // 0
            "LZ4 (recommended, small and still fast)", // 1
            "None (largest size and fastest to load)", // 2
        };

        // Collect assets from folders (optional recursive)
        private void CollectAssets(UnityEngine.Object root,
            ref List<UnityEngine.Object> exports,
            bool recursive = false)
        {
            var path = AssetDatabase.GetAssetPath(root);
            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (var guid in AssetDatabase.FindAssets("", new[] { path }))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath
                        <UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guid));
                    string fname = AssetDatabase.GetAssetPath(asset);
                    if (recursive) CollectAssets(asset, ref exports, recursive);
                    else if (!AssetDatabase.IsValidFolder(fname)) exports.Add(asset);
                }
            }
            // Just add as is to export
            else exports.Add(root);
        }

        List<int> clears = new List<int>();
        List<int> removes = new List<int>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            clears.Clear();
            removes.Clear();

            EditorGUI.BeginChangeCheck();
            
            var script = (OcbMicroSplatArray)target;

            if (script.Textures?.Length != script.Size)
                Array.Resize(ref script.Textures, script.Size);

            // int clear = -1;
            // int remove = -1

            // GUILayout.Box(Texture2D.blackTexture, GUILayout.Height(3), GUILayout.ExpandWidth(true));

            for (int i = 0; i < script.Size; i += 1)
            {

                if (script.Textures[i] == null) script.Textures[i]
                        = new OcbMicroSplatArrayEntry();

                using (new GUILayout.VerticalScope(GUI.skin.box))
                {

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space(); EditorGUILayout.Space();
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(20));

                    var item = script.Textures[i];

                    if (item.Albedo == null) item.Albedo = null;
                    if (item.Normal == null) item.Normal = null;
                    if (item.Height == null) item.Height = null;
                    if (item.Smoothness == null) item.Smoothness = null;
                    if (item.Occlusion == null) item.Occlusion = null;

                    if (item.HasTextures())
                    {
                        EditorGUILayout.LabelField(item.Albedo?.name ?? "empty");
                        if (GUILayout.Button("Clear Entry")) clears.Add(i);
                    }
                    else
                    {
                        // EditorGUILayout.HelpBox("Removing an entry completely can cause texture choices to change on existing terrains. " +
                        //     "You can leave it blank to preserve the texture order and MicroSplat will put a dummy texture into the array.", MessageType.Warning);
                        if (GUILayout.Button("Delete Entry")) removes.Add(i);
                    }

                    GUI.enabled = i != 0;
                    if (GUILayout.Button("↑ Up ↑", GUILayout.Width(72)))
                    {
                        (script.Textures[i], script.Textures[i - 1]) =
                            (script.Textures[i - 1], script.Textures[i]);
                    }
                    GUI.enabled = i != script.Textures.Length - 1;
                    if (GUILayout.Button("↓ Down ↓", GUILayout.Width(72)))
                    {
                        (script.Textures[i], script.Textures[i + 1]) =
                            (script.Textures[i + 1], script.Textures[i]);
                        // SwapEntry(cfg, i, i + 1);
                    }
                    GUI.enabled = true;

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();

                    DrawTextureCell(TexTitle("Diffuse", item.Albedo), ref item.Albedo, null, changed => {
                        FillMissingTexturesHeuristically(changed, item, AlbedoPathTails);
                    });
                    DrawTextureCell(TexTitle("Normal", item.Normal), ref item.Normal, () =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Switch", GUILayout.Width(44));
                        item.IsNormalSwitched = EditorGUILayout.Toggle(item.IsNormalSwitched, GUILayout.Width(20));
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Is DX", GUILayout.Width(44));
                        item.IsNormalInverted = EditorGUILayout.Toggle(item.IsNormalInverted, GUILayout.Width(20));
                        EditorGUILayout.EndHorizontal();
                    }, changed => {
                        FillMissingTexturesHeuristically(changed, item, NormalPathTails);
                    });
                    DrawTextureCell(TexTitle("Height", item.Height), ref item.Height, () =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Center", GUILayout.Width(44));
                        bool wasToggled = item.IsHeightCentered;
                        item.IsHeightCentered = EditorGUILayout.Toggle(item.IsHeightCentered, GUILayout.Width(20));
                        if (!wasToggled && item.IsHeightCentered) item.IsHeightNormalized = false;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Stretch", GUILayout.Width(44));
                        wasToggled = item.IsHeightNormalized;
                        item.IsHeightNormalized = EditorGUILayout.Toggle(item.IsHeightNormalized, GUILayout.Width(20));
                        if (!wasToggled && item.IsHeightNormalized) item.IsHeightCentered = false;
                        EditorGUILayout.EndHorizontal();
                        
                    }, changed => {
                        FillMissingTexturesHeuristically(changed, item, HeightPathTails);
                    });
                    DrawTextureCell(TexTitle("Smooth", item.Smoothness), ref item.Smoothness, () => {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Invert", GUILayout.Width(44));
                        item.IsRoughness = EditorGUILayout.Toggle(item.IsRoughness, GUILayout.Width(20));
                        EditorGUILayout.EndHorizontal();
                    }, changed => {
                        FillMissingTexturesHeuristically(changed, item, SmoothnessPathTails);
                        item.IsRoughness = changed.name.ToLower().Contains("rough");
                    });
                    DrawTextureCell(TexTitle("AO", item.Occlusion), ref item.Occlusion, null, changed => {
                        FillMissingTexturesHeuristically(changed, item, OcclusionPathTails);
                    });

                    EditorGUILayout.EndHorizontal();
                }


            }

            // Clear all textures that requested it
            for (int i = clears.Count - 1; i != -1; i -= 1)
                script.Textures[clears[i]].Clear();
            // Move all textures one down when one is removed
            for (int i = removes.Count - 1; i != -1; i -= 1)
                for (int n = removes[i] + 1; n < script.Textures.Length; n += 1)
                    script.Textures[n - 1] = script.Textures[n];
            // Reset the size for the textures
            script.Size -= removes.Count;

            // Add a new texture entiry at the bottom of the list
            if (GUILayout.Button("Add Texture Entry", GUILayout.Height(24)))
            {
                Array.Resize(ref script.Textures, script.Size + 1);
                script.Textures[script.Size] = new OcbMicroSplatArrayEntry();
                script.Size += 1;
            }

            GUI.enabled = script.Textures.All(t => t.Albedo != null); // && t.Normal != null
            if (GUILayout.Button("Generate Texture2D Array", GUILayout.Height(24)))
            {
                string path = AssetDatabase.GetAssetPath(target).Replace("\\", "/");
                OcbMicroSplatArrayCreator.CreateTextureArrays(script.Textures, path);
            }
            GUI.enabled = true;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

        }

        // Recognized path tails for different textures
        static string[] AlbedoPathTails = new string[]
        { "basecolor", "col", "color", "albedo",
          "diff", "diffuse", "base" };
        static string[] NormalPathTails = new string[]
        { "nrm", "norm", "normal" };
        static string[] HeightPathTails = new string[]
        { "height", "displacement" };
        static string[] SmoothnessPathTails = new string[]
        { "smooth", "smoothness", "rough", "roughness" };
        static string[] OcclusionPathTails = new string[]
        { "ao", "ambient", "occlusion", "ambientocclusion" };

        private void FillMissingTexturesHeuristically(Texture2D tex,
            OcbMicroSplatArrayEntry item, string[] tails)
        {
            if (tex == null) return;
            var fname = AssetDatabase.GetAssetPath(tex);
            var name = Path.GetFileNameWithoutExtension(fname);
            var path = Path.GetDirectoryName(fname);
            name = name.ToLower(); // compare lower case
            foreach (var tail in tails)
            {
                if (name.EndsWith(tail) == false) continue;
                var pre = tex.name.Remove(tex.name.Length - tail.Length);
                FillMissingTextureHeuristically(item, path, pre.ToLower());
                break;
            }
        }

        private static void FillMissingTextureHeuristically(OcbMicroSplatArrayEntry item, string folder, string prefix)
        {
            foreach (var guid in AssetDatabase.FindAssets("", new[] { folder }))
            {
                var fname = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(fname);
                name = name.ToLower(); // compare lower case
                if (item.Normal == null && IsValidTextureFile(
                    name, prefix, NormalPathTails))
                        LoadTextureAsset(fname, ref item.Normal);
                if (item.Height == null && IsValidTextureFile(
                    name, prefix, HeightPathTails))
                    LoadTextureAsset(fname, ref item.Height);
                if (item.Smoothness == null && IsValidTextureFile(
                    name, prefix, SmoothnessPathTails))
                {
                    LoadTextureAsset(fname, ref item.Smoothness);
                    item.IsRoughness = fname.ToLower().Contains("rough"); 
                }
                if (item.Occlusion == null && IsValidTextureFile(
                    name, prefix, OcclusionPathTails))
                        LoadTextureAsset(fname, ref item.Occlusion);
            }
        }

        private static void LoadTextureAsset(
            string path, ref Texture2D texture)
        {
            // if (texture != null) return; // Only overwrite empty ones?
            var loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (loaded != null) texture = loaded; // only assign if valid
        }

        private static bool IsValidTextureFile(
            string name, string prefix, string[] tails)
        {
            Debug.Log($" test {name} starts {prefix}");
            if (name.StartsWith(prefix))
            {
                Debug.Log($" test {name} end {tails}");
                foreach (var tail in tails)
                    if (name.EndsWith(tail))
                        return true;
            }
            return false;
        }

        private string TexTitle(string title, Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            bool onDisk = string.IsNullOrEmpty(path);
            return onDisk ? $"{title}[C]" : title;
        }

        // Make sure we can read the texture on the GPU
        private static void CheckReadableTexture(Texture2D texture)
        {
            if (texture == null) return;
            if (texture.isReadable) return;
            if (GUILayout.Button("Fix: Mark readable", GUILayout.Height(24)))
            {
                string path = AssetDatabase.GetAssetPath(texture);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    if (importer.isReadable) return;
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void DrawTextureCell(string name, ref Texture2D tex,
            Action renderer = null, Action<Texture2D> changed = null)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(name, GUILayout.Width(64));
            var color = GUI.backgroundColor;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                GUI.backgroundColor = Color.red;
            CheckReadableTexture(tex);
            CheckUncompressedTexture(tex);
            var previous = tex;
            tex = EditorGUILayout.ObjectField(tex, typeof(Texture2D), false,
                GUILayout.Width(64), GUILayout.Height(64)) as Texture2D;
            if (tex != previous) changed(tex);
            GUI.backgroundColor = color;
            if (renderer != null) renderer();
            EditorGUILayout.EndVertical();
        }

        // Make sure we do not compress source textures
        // We only want to compress the final result
        // Otherwise we may do two lossy compressions
        private static void CheckUncompressedTexture(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    if (GUILayout.Button("Fix: Mark uncompressed", GUILayout.Height(24)))
                    {
                        importer.textureCompression =
                            TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        private static void DrawTextureInv(string name, ref Texture2D tex, ref bool invert)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(name, GUILayout.Width(64));
            var color = GUI.backgroundColor;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                GUI.backgroundColor = Color.red;
            tex = EditorGUILayout.ObjectField(tex, typeof(Texture2D), false,
                GUILayout.Width(64), GUILayout.Height(64)) as Texture2D;
            GUI.backgroundColor = color;
            EditorGUILayout.EndVertical();
        }

    }


    // Polyfill Implementation for `Path.GetRelativePath`
    // From https://stackoverflow.com/a/74747405/1550314
    static class PathUtil
    {
        public static string GetRelativePath(string relativeTo, string path)
        {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return Path.GetRelativePath(relativeTo, path);
#else
            return GetRelativePathPolyfill(relativeTo, path);
#endif
        }

#if !(NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        static string GetRelativePathPolyfill(string relativeTo, string path)
        {
            path = Path.GetFullPath(path);
            relativeTo = Path.GetFullPath(relativeTo);

            var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            IReadOnlyList<string> p1 = path.Split(separators);
            IReadOnlyList<string> p2 = relativeTo.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var sc = StringComparison;

            int i;
            int n = Math.Min(p1.Count, p2.Count);
            for (i = 0; i < n; i++)
                if (!string.Equals(p1[i], p2[i], sc))
                    break;

            if (i == 0)
            {
                // Cannot make a relative path, for example if the path resides on another drive.
                return path;
            }

            p1 = p1.Skip(i).Take(p1.Count - i).ToList();

            if (p1.Count == 1 && p1[0].Length == 0)
                p1 = Array.Empty<string>();

            string relativePath = string.Join(
                new string(Path.DirectorySeparatorChar, 1),
                Enumerable.Repeat("..", p2.Count - i).Concat(p1));

            if (relativePath.Length == 0)
                relativePath = ".";

            return relativePath;
        }

        static StringComparison StringComparison =>
            IsCaseSensitive ?
                StringComparison.Ordinal :
                StringComparison.OrdinalIgnoreCase;

        static bool IsCaseSensitive =>
            !(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
#endif
    }

}