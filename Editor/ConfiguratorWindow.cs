using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    public class ConfiguratorWindow : EditorWindow
    {
        private ScrollView _variantSetScrollView;
        private Button _refreshButton;

        private static void NewVariantSetBase<T>() where T : VariantSetBase
        {
            GameObject newVariantSet = new GameObject(typeof(T).Name);
            newVariantSet.transform.SetParent(Selection.activeTransform, true);
            newVariantSet.AddComponent<T>();
            Selection.activeObject = newVariantSet;
        }
        
        [MenuItem("GameObject/Product Configurator/Variant Set/GameObject Variant Set")]
        public static void CreateGameObjectVariantSet()
        {
            NewVariantSetBase<GameObjectVariantSet>();
        }
        
        [MenuItem("GameObject/Product Configurator/Variant Set/Transform Variant Set")]
        public static void CreateTransformVariantSet()
        {
            NewVariantSetBase<TransformVariantSet>();
        }
        
        [MenuItem("GameObject/Product Configurator/Variant Set/Material Variant Set")]
        public static void CreateMaterialVariantSet()
        {
            NewVariantSetBase<MaterialVariantSet>();
        }
        
        [MenuItem("GameObject/Product Configurator/Variant Set/Combination Variant Set")]
        public static void CreateCombinationVariantSet()
        {
            NewVariantSetBase<CombinationVariantSet>();
        }
        
        [MenuItem("GameObject/Product Configurator/Variant Set/Animation Variant Set")]
        public static void CreateAnimationVariantSet()
        {
            NewVariantSetBase<AnimationVariantSet>();
        }
        
        [MenuItem("Window/Product Configurator")]
        public static void ShowWindow()
        {
            EditorWindow wnd = GetWindow<ConfiguratorWindow>();
            wnd.titleContent = new GUIContent("Configurator Window");
        }
        
        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += RefreshVariantSetScrollView;
        }

        public void CreateGUI()
        {
            var windowTitleLabel = new Label
            {
                text = "Variant Sets in scene",
                style =
                {
                    marginTop = new Length(5f, LengthUnit.Pixel),
                    marginBottom = new Length(5f, LengthUnit.Pixel),
                }
            };
            rootVisualElement.Add(windowTitleLabel);
            Button refreshButton = new Button
            {
                text = "Refresh",
                style =
                {
                    marginTop = new Length(5f, LengthUnit.Pixel),
                    marginBottom = new Length(5f, LengthUnit.Pixel)
                }
            };

            refreshButton.clicked += RefreshButtonClicked;
            rootVisualElement.Add(refreshButton);

            _variantSetScrollView = new ScrollView
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible,
                mode = ScrollViewMode.Vertical,
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    marginTop = new Length(5f, LengthUnit.Pixel),
                    marginLeft = new Length(5f, LengthUnit.Pixel),
                    marginRight = new Length(5f, LengthUnit.Pixel),
                    paddingBottom = new Length(5f, LengthUnit.Pixel),
                    paddingTop = new Length(5f, LengthUnit.Pixel),
                    paddingRight = new Length(5f, LengthUnit.Pixel),
                    paddingLeft = new Length(5f, LengthUnit.Pixel),
                    borderBottomLeftRadius = 2.5f,
                    borderBottomRightRadius = 2.5f,
                    borderTopLeftRadius = 2.5f,
                    borderTopRightRadius = 2.5f,
                    borderTopWidth = 1f,
                    borderTopColor = Color.grey,
                    borderBottomColor = Color.grey,
                    borderBottomWidth = 1f,
                    borderLeftColor = Color.grey,
                    borderLeftWidth = 1f,
                    borderRightColor = Color.grey,
                    borderRightWidth = 1f,
                    marginBottom = new Length(5f, LengthUnit.Pixel),
                },
            };
            
            rootVisualElement.Add(_variantSetScrollView);
            
            RefreshVariantSetScrollView();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= RefreshVariantSetScrollView;
            if (_refreshButton != null)
            {
                _refreshButton.clicked -= RefreshButtonClicked;
            }
        }

        private void RefreshButtonClicked()
        {
            RefreshVariantSetScrollView();
        }

        private void RefreshVariantSetScrollView()
        {
            _variantSetScrollView.Clear();
            var variantSets = FindObjectsByType<VariantSetBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < variantSets.Length; i++)
            {
                var variantSetButton = new Button
                {
                    text = variantSets[i].VariantSetAsset == null ? "Unnamed Variant Set" : variantSets[i].VariantSetAsset.VariantSetName
                };

                if (i != variantSets.Length - 1)
                {
                    variantSetButton.style.marginBottom = new Length(1f, LengthUnit.Pixel);
                }
                
                var index = i;
                variantSetButton.clicked += () =>
                {
                    SelectVariantSet(variantSets[index]);
                };

                if (variantSets[i].VariantSetAsset != null)
                {
                    var so = new SerializedObject(variantSets[i].VariantSetAsset);
                    var property = so.FindProperty("variantSetName");
                
                    variantSetButton.TrackPropertyValue(property, serializedProperty =>
                    {
                        variantSetButton.text = serializedProperty.stringValue;
                    });
                }
                
                _variantSetScrollView.Add(variantSetButton);
            }
        }

        private static void SelectVariantSet(Object variantSet)
        {
            Selection.activeObject = variantSet;
        }
    }
}
