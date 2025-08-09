# Client Setup Guide

This guide helps you set up the development environment for the SF_Lidgren client plugin.

## Prerequisites

1. **Stick Fight: The Game** - Installed via Steam
2. **BepInEx 5.x** - Game modding framework
3. **.NET 3.5 SDK** - For building the client plugin

## Required Game Dependencies

The client plugin requires several game assemblies that must be copied from your Stick Fight installation.

### Location of Game Files

**Windows (Steam):**
```
C:\Program Files (x86)\Steam\steamapps\common\StickFight\StickFight_Data\Managed\
```

**Linux (Steam):**
```
~/.steam/steam/steamapps/common/StickFight/StickFight_Data/Managed/
```

### Required Files

Copy these files from the game's `Managed` folder to the `SF_Lidgren` project directory:

1. **Assembly-CSharp.dll** - Main game code
2. **Assembly-CSharp-firstpass.dll** - Game framework code
3. **UnityEngine.dll** - Unity engine
4. **Lidgren.Network.dll** - Networking library (if present in game)

### BepInEx Dependencies

The client also requires BepInEx assemblies. You can either:

#### Option A: Manual Installation
Copy from your BepInEx installation:
- `BepInEx.dll`
- `0Harmony.dll`

#### Option B: NuGet Packages (Recommended)
The project will automatically download BepInEx packages when the NuGet source is available.

## Setting Up the Development Environment

### 1. Install BepInEx in Game

1. Download BepInEx 5.x from [GitHub](https://github.com/BepInEx/BepInEx/releases)
2. Extract to your Stick Fight game directory
3. Run the game once to generate BepInEx folders

### 2. Copy Game Dependencies

```bash
# Example for Windows
copy "C:\Program Files (x86)\Steam\steamapps\common\StickFight\StickFight_Data\Managed\Assembly-CSharp.dll" "SF_Lidgren\"
copy "C:\Program Files (x86)\Steam\steamapps\common\StickFight\StickFight_Data\Managed\Assembly-CSharp-firstpass.dll" "SF_Lidgren\"
copy "C:\Program Files (x86)\Steam\steamapps\common\StickFight\StickFight_Data\Managed\UnityEngine.dll" "SF_Lidgren\"
```

### 3. Update Project File

Uncomment the reference sections in `SF_Lidgren.csproj`:

```xml
<ItemGroup>
  <Reference Include="Assembly-CSharp">
    <HintPath>Assembly-CSharp.dll</HintPath>
  </Reference>
  <Reference Include="Assembly-CSharp-firstpass">
    <HintPath>Assembly-CSharp-firstpass.dll</HintPath>
  </Reference>
  <Reference Include="Lidgren.Network">
    <HintPath>Lidgren.Network.dll</HintPath>
  </Reference>
  <Reference Include="UnityEngine">
    <HintPath>UnityEngine.dll</HintPath>
  </Reference>
</ItemGroup>
```

### 4. Build the Client

```bash
cd SF_Lidgren
dotnet restore
dotnet build
```

## Installing the Built Plugin

1. Build the client plugin (see above)
2. Copy `SF_Lidgren.dll` from `bin/Debug/net35/` or `bin/Release/net35/`
3. Place it in `BepInEx/plugins/` in your game directory
4. Start the game

## Troubleshooting

### Build Errors

**"Assembly-CSharp could not be found"**
- Ensure you've copied the game assemblies to the project directory
- Check the file paths in the project file

**"BepInEx packages not found"**
- Try building without BepInEx packages first
- Manually copy BepInEx assemblies if needed

**"Target framework not supported"**
- Ensure you have .NET Framework 3.5 installed
- On Linux, ensure Mono is properly configured

### Runtime Issues

**Plugin not loading**
- Check BepInEx console for error messages
- Ensure BepInEx is properly installed
- Verify plugin is in the correct directory

**Game crashes**
- Check for assembly version mismatches
- Ensure all dependencies are present
- Review BepInEx logs for errors

## Development Tips

1. **Keep game files separate** - Don't commit game assemblies to version control
2. **Use symbolic links** - Link to game assemblies instead of copying
3. **Test incrementally** - Build and test small changes frequently
4. **Monitor logs** - BepInEx provides detailed logging for debugging

## Legal Notice

Game assemblies are copyrighted by Landfall Games. Only use them for development purposes and do not redistribute them.