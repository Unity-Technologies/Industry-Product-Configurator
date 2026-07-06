# Changelog

## [1.9.0] - TBC
###Changed
- Raised the minimum supported Unity version to 6.3 (LTS).
- Dependencies Cleaner now recognises variants and variant sets referenced through Combination Variant Set maps, so combination-only references are no longer reported as unused (and cannot be deleted by accident).
- Dependencies Cleaner selection is preserved while scrolling, and deleting a variant only removes that variant's own icon.

###Added
- Compatibility rules: a `CompatibilityRuleSet` asset + `CompatibilityController` to declare cross-set restriction ("A makes B unselectable") and requirement ("A needs B") rules. Exposes a query API (`IsRestricted` / `IsRequired` / `IsConfigurationValid` / restricted & required variant lists) so a developer's own UI can reflect availability, and auto-switches a set whose selection becomes restricted. No UI is imposed.
- Dependencies Cleaner: an "Orphan Icons" tab to find and remove icon files whose variant no longer exists.
- Dependencies Cleaner: warnings for dangling combination references that point at a deleted set or variant.
- Dependencies Cleaner: a prompt to remove variants that became unused after deleting their variant set.

###Removed
- Removed unused variant and variant set assets from the SkidLoader Configurator sample.

## [1.8.0] - 2026-05-19
###Changed
- Various bug fixes across runtime and editor
- Improved event subscription lifecycle to prevent stale listeners on scene unload
- Added configurable animator layer index to Animation Variant Set
- Added configurable camera priority fields to Variant Select
- Various code improvements and dead code removal

## [1.7.6] - 2025-08-14
- Fixing App UI Style for sample scenes

## [1.7.5] - 2025-07-16
- A small improvement App UI installer

## [1.7.4] - 2025-07-03
- A small improvement on Transform Variant Set Component

## [1.7.3] - 2025-07-03
- Fixed issue the free look camera prefab has missing Input references from the sample
- Fixed issue missing reference on the MiainUIController from the sample

## [1.7.2] - 2025-07-03
###Changed
- Fixed issue Transform Variant Set might return error when missing variable
- Fixed issue that "+" appear under Variant Set even not in advance mode
- Fixed issue that Combination Variant will not trigger animation from Transform Variant Set

## [1.7.1] - 2025-07-03
###Changed
- Fixed issue that not asking user to install the AppUI package when importing samples

## [1.7.0] - 2025-06-26
###Changed
- Updated new Ski Loader Sample

###Added
- Added Talia Chair Sample
- Added a new shared folder to share resources between samples

## [1.6.0] - 2025-06-03
###Changed
- Upgrade using Cinemachine to v3.x
- Upgrade the sample scene to use Cinemachine v3.x

###Added
- Dependencies cleaner editor window

###Removed
- The pop-up that ask users to remove dependencies

## [1.5.11] - 2025-01-28
###Changed
- Change to support Cinemachine below version 3.

###Added
- VariantSetBase class to support Cinemachine type for Focus Camera field when using Cinemachine 3 or above.
- VariantSetBase class now has an event which will trigger when a variant change.

## [1.5.10] - 2025-01-10
###Changed
- Fixed issue that variant set UI misbehaviour.
- Some UI improvement.

## [1.5.9] - 2025-01-10
###Changed
- Fixed issue that variant set UI misbehaves when there is no renders in the Combination variant set component.

## [1.5.8] - 2025-01-09
###Changed
- Fixed issue that variant set UI misbehaves when there is no renders in the Material variant set component in advance mode.

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
