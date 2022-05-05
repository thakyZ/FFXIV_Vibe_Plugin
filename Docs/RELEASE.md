# Internal release process reminder
1. Bump the C# assembly project version (double click on FFXIV\_Vibe\_Plugin in Visual Studio).
2. Build in release mode (ignore the error saying that the manifest file could not be found)
3. Go to the %appdata%/XIVLauncher/devPlugins/FFXIV\_Vibe\_Plugin folder and again in FFXIV\_Vibe\_Plugin folder.
4. Rename the ZIP to the corresponding version (eg: FFXIV\_Vibe\_Plugin\_v1.4.0.0.zip)
5. Update the repo.json version, timestamp, downloadCount and download links.
6. Tag the project version (eg: `git tag v1.4.0.0`)
7. Create a release on github, upload the zip and publish
8. Remove the FFXIV\_Vibe\_Plugin in devPlugins/FFXIV\_Vibe\_Plugin
