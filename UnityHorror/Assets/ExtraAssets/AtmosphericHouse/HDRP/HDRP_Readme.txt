//*****  (©) Finward Studios 2021. All rights reserved. *****\\

HDRP Instructions:
1. Import asset to an existing HDRP project.
2. Import AtmosphericHouseHDRP.unitypackage file from Assets -> Import Package -> Custom Package.
3. Replace all files.
4. Shaders and scene should update to HDRP.

HDRP changes for demomaps to work correctly:
1. Enable Shadowmask from HDRP asset. Lighting -> Shadows -> Shadowmask.

HDRP issues: 
Some materials look strange, such as plants and curtains?
This is due to missing Diffusion profiles from your HDRP asset file. Package has two Diffusion profiles that needs to be added to your HDRP asset.
Select Curtains_clean material and click fix in Diffusion Profile. Do the same for Interior_plants_clean material.

Exterior feels too bright?
Select PostProcessing game object and set Exposure to Automatic.
