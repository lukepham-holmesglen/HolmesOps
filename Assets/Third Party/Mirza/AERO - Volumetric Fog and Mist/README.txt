
AERO - Volumetric Fog & Mist:

A) Set your render pipeline asset to the one included in the package under Settings.
(make sure that asset and the renderer with the volumetric fog is active).

B) Alternatively, add a URP FullScreenPassRendererFeature and set the fog material.

You can use the included volumetric fog material, or copy/modify/create your own.

Quality and performance are determined by raymarch steps and features.

That's it!

Please see the live documentation for more information.

🔗 https://mirzabeig.notion.site/AERO-Volumetric-Fog-Mist-3c0d023ca81842509ad89c749307ac53

-Release Notes-

1.0.0 | February 12, 2026:

- Reboot, first release.

1.1.0 | May 15, 2026:

- Improved setup, hopefully *this* time it won't be deprecated shortly...
- Fixed lighting not working in builds due to pre-processor naming/conflicts.

1.2.0 | May 31, 2026:

- Added scale slider for ambient lighting.
- Added support for adaptive probe volumes.

1.3.0 | June 02, 2026:

- Added height masking (height fog) + texture.

1.4.0 | June 07, 2026:

- Added fog self-shadowing.

1.5.0 | June 09, 2026:

- Added height fog gradient(s).
- Improved self-shadowing performance.

1.6.0 | June 10, 2026:

- Added custom shadow colour(s).
- Performance optimizations, and bool-gating.

1.6.1 | June 10, 2026:

- Scene and settings tweak.

1.6.2 | June 10, 2026:

- Fixed issue with custom shadow colours' alpha being applied to fog self-shadowing even when the toggle has them disabled. Now they correctly match toggle state.

1.6.3 | June 11, 2026:

- Fixed APV sampling space issue.
- Fixed issue with large scene texture sampling consistency.
- Fixed issue with shadow artifacts at max distance.

- Improved anisotropy.

1.7.0 | June 12, 2026:

- Added support for light cookies.
- Added scrolling noise textures example.

- Fixed flickering NaNs issue with APV lighting.

1.7.1 | June 12, 2026:

- Fixed jittering from precision degradation farther from origin via manual offset calculation.

1.8.0 | June 13, 2026:

- Added orthographic camera/rendering support.