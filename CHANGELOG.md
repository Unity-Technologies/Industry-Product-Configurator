# Changelog

## [1.5.7] - 2025-01-09
###Changed
- Fixed issue that variant set UI misbehaves when there is no renders in the Material variant set component.

## [1.5.6] - 2025-01-09
###Changed
- Fixed issue that variant set UI misbehaves when there is no renders in the Material variant set component.

## [1.5.5] - 2025-01-09
###Changed
- Fixed issue that variant set UI misbehaves when there is no renders in the Material variant set component.


## [1.5.4] - 2025-01-06
###Added
- Added variant description field to the variant inspector.
- Added support on Cinemachine version 3 and upward.

###Changed
- Fixed issue that variant set UI misbehaves when there is no variant in the variant set.

## [1.5.3] - 2024-09-17
###Added
- Added a Variant Drop-down to the inspector to allow users to preview a variant from the drop-down list.

## [1.5.2] - 2024-09-03
###Changed
- Fixed issue that adding a new variant on combination variant set causes unexpected behavior on inspector.


## [1.5.1] - 2024-07-15
###Changed
- Update Sample to have a zoom in and out camera action
- Update TransformVariantSet to have an option to lerp to variant transform position, rotation and scale
- Update to be able to hit "Enter" to confirm variant name
- Update CombinationVariantSet to allow user to pick different variant in a combination.


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
