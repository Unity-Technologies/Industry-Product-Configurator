using System;
using System.IO;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using UnityEngine;
using UnityEditor;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    public class IconPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                if (!asset.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                var fullIconPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                    EditorCore.VariantSetIconPath);
                
                if (!string.Equals(Directory.GetParent(asset).FullName, fullIconPath)) continue;
                string id = Path.GetFileNameWithoutExtension(asset);
                foreach (var configurationAsset in AssetDatabase.FindAssets($"t:{nameof(VariantAsset)}"))
                {
                    var configurationOption = AssetDatabase.LoadAssetAtPath<VariantAsset>(
                        AssetDatabase.GUIDToAssetPath(configurationAsset));
                    if (!string.Equals(configurationOption.UniqueIdString, id)) continue;
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
                    configurationOption.icon = texture;
                    EditorUtility.SetDirty(configurationOption);
                }
            }
        }
    }
}
