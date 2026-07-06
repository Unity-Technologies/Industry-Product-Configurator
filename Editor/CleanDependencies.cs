using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    public class CleanDependencies : EditorWindow
    {
        // Unused assets are keyed by Unity asset GUID; orphan icons by asset path.
        private List<string> _unusedVariantAssets = new();
        private List<string> _unusedVariantSetAssets = new();
        private List<string> _orphanIconPaths = new();

        // Persistent selection that survives ListView row recycling (row toggles are just a view of it).
        private readonly HashSet<string> _selected = new();
        // Cache of icon-tab thumbnails so scrolling doesn't reload textures every rebind.
        private readonly Dictionary<string, Texture2D> _thumbCache = new();

        private VariantDependencyScanner.ScanResult _scan;
        private Dictionary<string, DepNode> _nodeByGuid = new();
        private Dictionary<string, string> _uidToName = new();
        private List<string> _newlyUnusedVariants = new();

        private TabView _tabView;
        private Tab _variantAssetsTab;
        private Tab _variantSetAssetsTab;
        private Tab _orphanIconsTab;
        private Button _searchButton;
        private Label _danglingWarning;
        private VisualElement _orphanHint;
        private Label _orphanHintLabel;
        private Button _orphanHintButton;
        private ListView _listView;
        private Button _selectAllButton;
        private Button _deselectAllButton;
        private Button _deleteButton;

        private bool IsIconTab => _tabView != null && _tabView.activeTab == _orphanIconsTab;

        [MenuItem("Window/Product Configurator/Dependencies Cleaner")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<CleanDependencies>();
            wnd.titleContent = new GUIContent("Dependencies Cleaner");
        }

        public void CreateGUI()
        {
            _searchButton = new Button(Search) { text = "Search Unused Assets" };
            rootVisualElement.Add(_searchButton);

            _danglingWarning = new Label
            {
                style =
                {
                    marginLeft = 2, marginTop = 2, marginBottom = 2,
                    whiteSpace = WhiteSpace.Normal,
                    color = new Color(0.95f, 0.6f, 0.2f),
                    display = DisplayStyle.None
                }
            };
            rootVisualElement.Add(_danglingWarning);

            _orphanHint = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = Align.Center,
                    marginLeft = 2, marginTop = 2, marginBottom = 2,
                    display = DisplayStyle.None
                }
            };
            _orphanHintLabel = new Label
            {
                style = { whiteSpace = WhiteSpace.Normal, flexGrow = 1, color = new Color(0.55f, 0.8f, 1f) }
            };
            _orphanHintButton = new Button(OnDeleteNewlyUnusedClicked) { text = "Delete them" };
            _orphanHint.Add(_orphanHintLabel);
            _orphanHint.Add(_orphanHintButton);
            rootVisualElement.Add(_orphanHint);

            _tabView = new TabView();
            _variantAssetsTab = new Tab("Variant Assets") { name = "Variant Assets" };
            _variantSetAssetsTab = new Tab("Variant Set Assets") { name = "Variant Set Assets" };
            _orphanIconsTab = new Tab("Orphan Icons") { name = "Orphan Icons" };
            _tabView.Add(_variantAssetsTab);
            _tabView.Add(_variantSetAssetsTab);
            _tabView.Add(_orphanIconsTab);
            _tabView.activeTab = _variantAssetsTab;
            _tabView.activeTabChanged += OnActiveTabChanged;
            rootVisualElement.Add(_tabView);

            _listView = new ListView
            {
                makeItem = MakeItem,
                bindItem = BindItem,
                unbindItem = UnBindItem,
                style = { flexGrow = 1 }
            };
            rootVisualElement.Add(_listView);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            _selectAllButton = new Button(SelectAll) { text = "Select All" };
            _deselectAllButton = new Button(DeselectAll) { text = "Deselect All" };
            buttonRow.Add(_selectAllButton);
            buttonRow.Add(_deselectAllButton);
            rootVisualElement.Add(buttonRow);
            _selectAllButton.SetEnabled(false);
            _deselectAllButton.SetEnabled(false);

            _deleteButton = new Button(DeleteSelected) { text = "Delete Selected" };
            _deleteButton.SetEnabled(false);
            rootVisualElement.Add(_deleteButton);
        }

        private void OnDestroy()
        {
            if (_tabView != null) _tabView.activeTabChanged -= OnActiveTabChanged;
        }

        private void Search()
        {
            if (_orphanHint != null) _orphanHint.style.display = DisplayStyle.None;

            // Detection is delegated to the shared analyzer: unlike a plain GetDependencies scan it also
            // counts references living only in combination string-GUID maps, so those are never flagged
            // unused. Combination maps are read from prefabs and currently-open scenes.
            _scan = VariantDependencyScanner.Scan();

            _nodeByGuid = _scan.Nodes.ToDictionary(n => n.AssetGuid, n => n);
            _uidToName = _scan.Nodes
                .GroupBy(n => n.UniqueId)
                .ToDictionary(g => g.Key, g => g.First().DisplayName);

            _unusedVariantAssets = _scan.Nodes.Where(x => x.Kind == NodeKind.Variant && x.Unused).Select(x => x.AssetGuid).ToList();
            _unusedVariantSetAssets = _scan.Nodes.Where(x => x.Kind == NodeKind.VariantSet && x.Unused).Select(x => x.AssetGuid).ToList();
            _orphanIconPaths = new List<string>(_scan.Usage.OrphanIconPaths);

            _selected.Clear();
            _thumbCache.Clear();

            UpdateDanglingWarning();
            DrawLists();
        }

        private List<string> CurrentItems()
        {
            if (_tabView == null) return _unusedVariantAssets;
            if (_tabView.activeTab == _variantSetAssetsTab) return _unusedVariantSetAssets;
            if (_tabView.activeTab == _orphanIconsTab) return _orphanIconPaths;
            return _unusedVariantAssets;
        }

        private void DrawLists()
        {
            if (_listView == null) return;
            var items = CurrentItems();
            _listView.itemsSource = items;
            _listView.Rebuild();

            bool hasItems = items.Count > 0;
            _selectAllButton?.SetEnabled(hasItems);
            _deselectAllButton?.SetEnabled(hasItems);
            _deleteButton?.SetEnabled(_selected.Count > 0);
        }

        private void UpdateDanglingWarning()
        {
            if (_danglingWarning == null) return;
            var dangling = _scan?.Usage.DanglingCombos;
            if (dangling == null || dangling.Count == 0)
            {
                _danglingWarning.style.display = DisplayStyle.None;
                return;
            }

            var lines = new List<string> { $"⚠ {dangling.Count} dangling combination reference(s) — a combination points to a deleted set/variant and will silently do nothing at runtime:" };
            foreach (var d in dangling.Take(6))
            {
                var comboName = _uidToName.TryGetValue(d.ComboSetId, out var cn) ? cn : Short(d.ComboSetId);
                var optionName = _uidToName.TryGetValue(d.ComboVariantId, out var on) ? on : Short(d.ComboVariantId);
                var missing = d.SetMissing && d.VariantMissing ? $"set {Short(d.TargetSetId)} + variant {Short(d.TargetVariantId)}"
                    : d.SetMissing ? $"set {Short(d.TargetSetId)}"
                    : $"variant {Short(d.TargetVariantId)}";
                lines.Add($"   • '{comboName}' → option '{optionName}' points to missing {missing}");
            }
            if (dangling.Count > 6) lines.Add($"   …and {dangling.Count - 6} more");
            lines.Add("   (combinations in unopened scenes aren't checked — open a scene and search again)");

            _danglingWarning.text = string.Join("\n", lines);
            _danglingWarning.style.display = DisplayStyle.Flex;
        }

        private static string Short(string id) =>
            string.IsNullOrEmpty(id) ? "(none)" : (id.Length > 8 ? id.Substring(0, 8) : id);

        private VisualElement MakeItem()
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, justifyContent = Justify.SpaceBetween }
            };
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };
            left.Add(new Toggle());
            left.Add(new Image { name = "thumb", scaleMode = ScaleMode.ScaleToFit, style = { width = 22, height = 22, marginLeft = 4, marginRight = 4 } });
            row.Add(left);
            row.Add(new Button { text = "View" });
            return row;
        }

        private void BindItem(VisualElement row, int index)
        {
            var items = _listView.itemsSource as List<string>;
            if (items == null || index < 0 || index >= items.Count) return;
            var item = items[index];

            var toggle = row.Q<Toggle>();
            var button = row.Q<Button>();
            var thumb = row.Q<Image>("thumb");
            thumb.image = null;
            thumb.tooltip = string.Empty;

            toggle.userData = item;
            button.userData = item;

            if (IsIconTab)
            {
                // item is an asset path to an orphaned icon PNG.
                toggle.text = Path.GetFileName(item);
                toggle.tooltip = item;
                thumb.image = GetOrLoadIconThumb(item);
            }
            else
            {
                // item is a Unity asset GUID; all display data comes from the scan (no per-row asset loads).
                var node = _nodeByGuid != null && _nodeByGuid.TryGetValue(item, out var n) ? n : null;
                var name = node != null ? node.DisplayName : item;
                // "which set it belonged to" isn't recoverable for an unused variant, so we disambiguate
                // identically-named entries (e.g. several "Black") with a short id + path tooltip + icon.
                var shortId = node != null ? Short(node.UniqueId) : Short(item);
                toggle.text = $"{name}  ·  {shortId}";
                toggle.tooltip = node != null ? node.AssetPath : string.Empty;
                if (_scan != null && _scan.IconByGuid.TryGetValue(item, out var tex)) thumb.image = tex;
            }

            button.RegisterCallback<ClickEvent>(OnViewButtonClicked);
            toggle.RegisterValueChangedCallback(OnToggleValueChanged);
            toggle.SetValueWithoutNotify(_selected.Contains(item));
        }

        private void UnBindItem(VisualElement row, int index)
        {
            row.Q<Button>().UnregisterCallback<ClickEvent>(OnViewButtonClicked);
            row.Q<Toggle>().UnregisterValueChangedCallback(OnToggleValueChanged);
        }

        private Texture2D GetOrLoadIconThumb(string path)
        {
            if (_thumbCache.TryGetValue(path, out var tex)) return tex;
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            _thumbCache[path] = tex;
            return tex;
        }

        private void OnToggleValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.currentTarget is not Toggle toggle || toggle.userData is not string key) return;
            if (evt.newValue) _selected.Add(key);
            else _selected.Remove(key);
            _deleteButton?.SetEnabled(_selected.Count > 0);
        }

        private void OnViewButtonClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button || button.userData is not string data || string.IsNullOrEmpty(data)) return;

            // Icon rows store a path; asset rows store a GUID (GUIDs never contain '/').
            string assetPath = data.Contains('/')
                ? data
                : (_nodeByGuid != null && _nodeByGuid.TryGetValue(data, out var node) ? node.AssetPath : AssetDatabase.GUIDToAssetPath(data));
            if (string.IsNullOrEmpty(assetPath)) return;

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null) return;
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private void SelectAll()
        {
            _selected.Clear();
            _selected.UnionWith(CurrentItems());
            _listView.Rebuild();
            _deleteButton?.SetEnabled(_selected.Count > 0);
        }

        private void DeselectAll()
        {
            _selected.Clear();
            _listView.Rebuild();
            _deleteButton?.SetEnabled(false);
        }

        private void OnActiveTabChanged(Tab prevTab, Tab newTab)
        {
            // Different tab = different item set; selection doesn't carry over.
            _selected.Clear();
            DrawLists();
        }

        private void DeleteSelected()
        {
            var items = CurrentItems();
            var selected = _selected.Where(items.Contains).ToList();
            if (selected.Count == 0) return;

            if (IsIconTab)
            {
                if (!EditorUtility.DisplayDialog("Delete Confirmation",
                        $"Delete {selected.Count} orphan icon file(s)? This cannot be undone.", "Delete", "Cancel"))
                    return;
                VariantCleanupService.DeleteOrphanIcons(selected);
                Search();
                return;
            }

            // Remember what was already unused, so we can tell which variants the deletion orphaned.
            var beforeUnusedVariants = new HashSet<string>(_unusedVariantAssets);

            bool alsoDeleteIcons;
            if (_tabView.activeTab == _variantSetAssetsTab)
            {
                // A variant set references no icons or other assets, so there is nothing extra to delete.
                if (!EditorUtility.DisplayDialog("Delete Confirmation",
                        $"Delete {selected.Count} variant set(s)?", "Delete Selected", "Cancel"))
                    return;
                alsoDeleteIcons = false;
            }
            else
            {
                // A variant references its icon texture, so offer to delete that alongside it.
                int option = EditorUtility.DisplayDialogComplex(
                    "Delete Confirmation",
                    "Do you want to delete the selected variant(s)?",
                    "Delete Selected + icons", // 0
                    "Delete Selected only",    // 1
                    "Cancel");                 // 2
                if (option == 2) return;
                alsoDeleteIcons = option == 0;
            }

            VariantCleanupService.DeleteAssets(selected, alsoDeleteIcons);
            Search();

            // Variants that weren't unused before but are now (e.g. their only variant set was deleted).
            var newlyUnused = _unusedVariantAssets.Where(g => !beforeUnusedVariants.Contains(g)).ToList();
            ShowOrphanHint(newlyUnused);
        }

        private void ShowOrphanHint(List<string> newlyUnusedVariantGuids)
        {
            _newlyUnusedVariants = newlyUnusedVariantGuids ?? new List<string>();
            if (_orphanHint == null) return;

            if (_newlyUnusedVariants.Count == 0)
            {
                _orphanHint.style.display = DisplayStyle.None;
                return;
            }

            _orphanHintLabel.text =
                $"{_newlyUnusedVariants.Count} variant(s) that belonged to the deleted set(s) are now unused — see the Variant Assets tab.";
            _orphanHintButton.text = $"Delete {_newlyUnusedVariants.Count} now";
            _orphanHint.style.display = DisplayStyle.Flex;
        }

        private void OnDeleteNewlyUnusedClicked()
        {
            if (_newlyUnusedVariants == null || _newlyUnusedVariants.Count == 0) return;
            if (!EditorUtility.DisplayDialog("Delete Confirmation",
                    $"Delete {_newlyUnusedVariants.Count} newly-unused variant(s)? This cannot be undone.", "Delete", "Cancel"))
                return;

            VariantCleanupService.DeleteAssets(_newlyUnusedVariants, false);
            Search(); // refreshes lists and hides the hint
        }
    }
}
