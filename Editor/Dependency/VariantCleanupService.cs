using System.Collections.Generic;
using System.IO;
using UnityEditor;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    /// <summary>
    /// Shared, UI-free deletion helpers used by the Dependencies Cleaner. Callers are responsible
    /// for user confirmation dialogs; this class only performs the work after being told to.
    /// </summary>
    public static class VariantCleanupService
    {
        /// <summary>
        /// Deletes the given assets (by Unity asset GUID). When <paramref name="alsoDeleteOwnIcons"/>
        /// is true, each asset's own captured icon (a PNG named "&lt;uniqueId&gt;.png", where uniqueId
        /// is that asset's UniqueIdString) is deleted too. A texture is only removed when it belongs to
        /// one of the assets being deleted, so a shared or unrelated texture is never touched.
        /// Returns the number of assets deleted.
        /// </summary>
        public static int DeleteAssets(IEnumerable<string> assetGuids, bool alsoDeleteOwnIcons)
        {
            AssetDatabase.SaveAssets(); // flush pending edits so paths/dependencies are current

            var guidsToDelete = new HashSet<string>(assetGuids);

            if (alsoDeleteOwnIcons)
            {
                // UniqueIds (canonical form) of the assets being deleted; their icons are "<uniqueId>.png".
                var deletedIds = new HashSet<string>();
                foreach (var guid in guidsToDelete)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<AssetBase>(assetPath);
                    if (asset != null && System.Guid.TryParse(asset.UniqueIdString, out var g)) deletedIds.Add(g.ToString());
                }

                var iconGuids = new HashSet<string>();
                foreach (var guid in guidsToDelete)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    foreach (var dependencyPath in AssetDatabase.GetDependencies(assetPath, false))
                    {
                        if (dependencyPath == assetPath) continue;
                        if (!dependencyPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;

                        var stem = Path.GetFileNameWithoutExtension(dependencyPath);
                        if (System.Guid.TryParse(stem, out var sg) && deletedIds.Contains(sg.ToString()))
                            iconGuids.Add(AssetDatabase.AssetPathToGUID(dependencyPath));
                    }
                }
                guidsToDelete.UnionWith(iconGuids);
            }

            int deleted = 0;
            foreach (var guid in guidsToDelete)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;
                if (AssetDatabase.DeleteAsset(assetPath)) deleted++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return deleted;
        }

        /// <summary>Deletes orphaned icon files by asset path. Returns the number deleted.</summary>
        public static int DeleteOrphanIcons(IEnumerable<string> iconPaths)
        {
            int deleted = 0;
            foreach (var path in iconPaths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetDatabase.DeleteAsset(path)) deleted++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return deleted;
        }
    }
}
