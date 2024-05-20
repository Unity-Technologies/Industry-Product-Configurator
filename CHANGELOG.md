# Changelog

## [1.5.0] - 2024-05-17
###Added
- Package Settings page, you can modify some settings under "Edit" > "Project Settings" > "Product Configurator"
- Introduced Basic Mode, it will simplify some interface on the inspector panel when modifying variant set
- EditorCode will now handle Asset Creation
- Introduced "VariantSelect" component for non-developer to pick variant set and variant from dropdown menus, and can invoke variant selection using UnityEvent UI (useful for uGUI)
- Add dialogue message to confirm removal of assets from project, when variant get removed or variant set get removed from scene.

###Changed
- VariantSetBase and VariantBase now has a newer Namespace, just added ".runtime" at the end of your existing namespace
- Some UX Update
- Fixed Restore Camera position for icon capture

## [1.4.0] - 2024-05-08
###Added
- Animation Variant Set

###Changed
- Move Variant Set Asset and Variant Asset creation to variant set base class.

## [1.2.0] - 2024-02-20
###Added
- Environment selection sample, camera control sample. Also conditional variant.

## [1.1.9] - 2024-02-15
###Added
- A new button to capture option icons in the inspector.

## [1.1.1] - 2024-02-13
###Added
- A new custom inspector of each configuration to preview each option in inspector.

## [1.0.0] - 2024-01-29
###Added
- This is the first release of Product Configurator, as a Package
