# Broadcast Jellyfin Plugin

> Continuous TV-style channel generator for Jellyfin with optional commercials and IPTV (M3U + XMLTV) integration.

[![License: GPLv3](https://img.shields.io/badge/License-GPLv3-blue.svg)](#license) [![Jellyfin ABI](https://img.shields.io/badge/ABI-10.9.0.0-orange.svg)](#plugin-manifest)

## Quick Links
- Live TV Setup: see section 13 below
- Diagnostics Endpoints: `/Broadcast/Diagnostics/*`
- IPTV Playlist: `/Broadcast/iptv/playlist.m3u`
- XMLTV EPG: `/Broadcast/iptv/epg.xml`

## Features
- Generate configurable schedule (hours + optional hour alignment)
- Insert commercials from a folder at configurable frequency
- Uses poster thumbnails in schedule + EPG
- Exposes IPTV playlist and XMLTV for Live TV channel
- Now/Next endpoint for fast UI status
- Rich diagnostics endpoints for libraries, schedule and commercials

## Installation (Release Binary)
1. Download latest release asset `Jellyfin.Plugin.Broadcast.dll` from GitHub Releases.
2. Place into: `~/.local/share/jellyfin/plugins/Broadcast/` (create folder if needed).
3. Restart Jellyfin server.
4. Configure under Dashboard -> Plugins -> Broadcast.

## Building From Source
```bash
# Clone
git clone https://github.com/satincake/jellyfin-plugin-broadcast.git
cd jellyfin-plugin-broadcast/Jellyfin.Plugin.Broadcast
# Build
dotnet build -c Release
# Copy to plugins directory
mkdir -p ~/.local/share/jellyfin/plugins/Broadcast
cp bin/Release/net8.0/Jellyfin.Plugin.Broadcast.* ~/.local/share/jellyfin/plugins/Broadcast/
```

## Plugin Manifest (build.yaml)
The `build.yaml` file is used by Jellyfin's plugin loader and distribution tooling.
Current values:
- name: Broadcast
- guid: eb5d7894-8eef-4b36-aa6f-5d124e828ce1
- version: 0.1.0.0
- targetAbi: 10.9.0.0
- artifacts: Jellyfin.Plugin.Broadcast.dll

Update `version` and `changelog` when cutting a new release tag.

## Configuration Overview
See in-app page for full detail.
- Enabled Libraries: source media (auto-fallback to all top-level if empty)
- Episode Duration: segment length (minutes)
- Commercial Frequency: segments between ad breaks
- Commercials Folder: ads pool; optional
- ChannelName / ChannelGroup / ChannelId: IPTV identifiers
- ChannelLogoUrl or UseFirstItemAsLogo
- ScheduleHours / AlignToHour

## Endpoints Summary
| Endpoint | Purpose |
|----------|---------|
| GET /Broadcast/Schedule | Full current schedule entries |
| POST /Broadcast/Schedule/Regenerate | Force rebuild schedule |
| GET /Broadcast/NowNext | Current & next item summary |
| GET /Broadcast/Channels | Channel metadata (logo & URLs) |
| GET /Broadcast/iptv/playlist.m3u | IPTV playlist |
| GET /Broadcast/iptv/epg.xml | XMLTV EPG |
| GET /Broadcast/iptv/stream?channelId=... | Stream current item |
| GET /Broadcast/Diagnostics/Config | Config snapshot |
| GET /Broadcast/Diagnostics/Libraries | Library list |
| GET /Broadcast/Diagnostics/LibraryItems | Sample items from library |
| GET /Broadcast/Diagnostics/Schedule | Coverage stats |
| GET /Broadcast/Diagnostics/Commercials | Commercial inventory |

## Release Process
1. Bump `version` + `changelog` in `build.yaml`.
2. Commit and tag: `git tag v0.1.1 && git push --tags`.
3. Create GitHub Release; attach built DLL.
4. Update README if feature changes.

## Contributing
See `CONTRIBUTING.md` for development workflow, guidelines, and testing checklist.

## Code of Conduct
See `CODE_OF_CONDUCT.md`.

## License
GPLv3. By using or contributing you agree to the terms in `LICENSE`.

---

# So you want to make a Jellyfin plugin

Awesome! This guide is for you. Jellyfin plugins are written using the dotnet standard framework. What that means is you can write them in any language that implements the CLI or the DLI and can compile to net8.0. The examples on this page are in C# because that is what most of Jellyfin is written in, but F#, Visual Basic, and IronPython should all be compatible once compiled.

## 0. Things you need to get started

- [Dotnet SDK 8.0](https://dotnet.microsoft.com/download)

- An editor of your choice. Some free choices are:

   [Visual Studio Code](https://code.visualstudio.com)

   [Visual Studio Community Edition](https://visualstudio.microsoft.com/downloads)

   [Mono Develop](https://www.monodevelop.com)

## 0.5. Quickstarts

We have a number of quickstart options available to speed you along the way.

- [Download the Example Plugin Project](https://github.com/jellyfin/jellyfin-plugin-template/tree/master/Jellyfin.Plugin.Template) from this repository, open it in your IDE and go to [step 3](https://github.com/jellyfin/jellyfin-plugin-template#3-customize-plugin-information)

- Install our dotnet template by [downloading the dotnet-template/content folder from this repo](https://github.com/jellyfin/jellyfin-plugin-template/tree/master/dotnet-template/content) or off of Nuget (Coming soon)

   ```
   dotnet new -i /path/to/templatefolder
   ```

- Run this command then skip to step 4

   ```
      dotnet new Jellyfin-plugin -name MyPlugin
   ```

If you'd rather start from scratch keep going on to step one. This assumes no specific editor or IDE and requires only the command line with dotnet in the path.

## 1. Initialize Your Project

Make a new dotnet standard project with the following command, it will make a directory for itself.

```
dotnet new classlib -f net8.0 -n MyJellyfinPlugin
```

Now add the Jellyfin shared libraries.

```
dotnet add package Jellyfin.Model
dotnet add package Jellyfin.Controller
```

You have an autogenerated Class1.cs file. You won't be needing this, so go ahead and delete it.

## 2. Set Up the Basics

There are a few mandatory classes you'll need for a plugin so we need to make them.

### PluginConfiguration

You can call it whatever you'd like really. This class is used to hold settings your plugin might need. We can leave it empty for now. This class should inherit from `MediaBrowser.Model.Plugins.BasePluginConfiguration`

### Plugin

This is the main class for your plugin. It will define your name, version and Id. It should inherit from `MediaBrowser.Common.Plugins.BasePlugin<PluginConfiguration>`

Note: If you called your PluginConfiguration class something different, you need to put that between the <>

### Implement Required Properties

The Plugin class needs a few properties implemented before it can work correctly.

It needs an override on ID, an override on Name, and a constructor that follows a specific model. To get started you can use the following section.

```c#
public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer){}
public override string Name => throw new System.NotImplementedException();
public override Guid Id => Guid.Parse("");
```

## 3. Customize Plugin Information

You need to populate some of your plugin's information. Go ahead a put in a string of the Name you've overridden name, and generate a GUID

- **Windows Users**: you can use the Powershell command `New-Guid`, `[guid]::NewGuid()` or the Visual Studio GUID generator

- **Linux and OS X Users**: you can use the Powershell Core command `New-Guid` or this command from your shell of choice:

   ```bash
   od -x /dev/urandom | head -n1 | awk '{OFS="-"; srand($6); sub(/./,"4",$5); sub(/./,substr("89ab",1+rand()*4,1),$6); print $2$3,$4,$5,$6,$7$8$9}'
   ```

or

   ```bash
   uuidgen
   ```

- Place that guid inside the `Guid.Parse("")` quotes to define your plugin's ID.

## 4. Adding Functionality

Congratulations, you now have everything you need for a perfectly functional functionless Jellyfin plugin! You can try it out right now if you'd like by compiling it, then placing the dll you generate in a subfolder (named after your plugin for example) within the plugins folder under your Jellyfin config directory. If you want to try and hook it up to a debugger make sure you copy the generated PDB file alongside it.

Most people aren't satisfied with just having an entry in a menu for their plugin, most people want to have some functionality, so lets look at how to add it.

### 4a. Implement Interfaces

If the functionality you are trying to add is functionality related to something that Jellyfin has an interface for you're in luck. Jellyfin uses some automatic discovery and injection to allow any interfaces you implement in your plugin to be available in Jellyfin.

Here's some interfaces you could implement for common use cases:

- **IAuthenticationProvider** - Allows you to add an authentication provider that can authenticate a user based on a name and a password, but that doesn't expect to deal with local users.
- **IBaseItemComparer** - Allows you to add sorting rules for dealing with media that will show up in sort menus
- **IIntroProvider** - Allows you to play a piece of media before another piece of media (i.e. a trailer before a movie, or a network bumper before an episode of a show)
- **IItemResolver** - Allows you to define custom media types
- **ILibraryPostScanTask** - Allows you to define a task that fires after scanning a library
- **IMetadataSaver** - Allows you to define a metadata standard that Jellyfin can use to write metadata
- **IResolverIgnoreRule** - Allows you to define subpaths that are ignored by media resolvers for use with another function (i.e. you wanted to have a theme song for each tv series stored in a subfolder that could be accessed by your plugin for playback in a menu).
- **IScheduledTask** - Allows you to create a scheduled task that will appear in the scheduled task lists on the dashboard.

There are loads of other interfaces that can be used, but you'll need to poke around the API to get some info. If you're an expert on a particular interface, you should help [contribute some documentation](https://docs.jellyfin.org/general/contributing/index.html)!

### 4b. Use plugin aimed interfaces to add custom functionality

If your plugin doesn't fit perfectly neatly into a predefined interface, never fear, there are a set of interfaces and classes that allow your plugin to extend Jellyfin any which way you please. Here's a quick overview on how to use them

- **IPluginConfigurationPage** - Allows you to have a plugin config page on the dashboard. If you used one of the quickstart example projects, a premade page with some useful components to work with has been created for you! If not you can check out this guide here for how to whip one up.

 **IPluginServiceRegistrator** - Will be located by Jellyfin at server startup and allows you to add services to the DI container to allow for injection in your plugin's classes later.

- **IHostedService** - Allows you to run code as a background task that will be started at program startup and will remain in memory. See [Microsoft's documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio#ihostedservice-interface) for more information. You can make as many of these as you need; make Jellyfin aware of them with an `IPluginServiceRegistrator`. It is wildly useful for loading configs or persisting state. **Be aware that your main plugin class (IBasePlugin) cannot also be a IHostedService.**

- **ControllerBase** - Allows you to define custom REST-API endpoints. This is the default ASP.NET Web-API controller. You can use it exactly as you would in a normal Web-API project. Learn more about it [here](https://docs.microsoft.com/aspnet/core/web-api/?view=aspnetcore-5.0).

Likewise you might need to get data and services from the Jellyfin core, Jellyfin provides a number of interfaces you can add as parameters to your plugin constructor which are then made available in your project (you can see the 2 mandatory ones that are needed by the plugin system in the constructor as is).

- **IBlurayExaminer** - Allows you to examine blu-ray folders
- **IDtoService** - Allows you to create data transport objects, presumably to send to other plugins or to the core
- **ILibraryManager** - Allows you to directly access the media libraries without hopping through the API
- **ILocalizationManager** - Allows you tap into the main localization engine which governs translations, rating systems, units etc...
- **INetworkManager** - Allows you to get information about the server's networking status
- **IServerApplicationPaths** - Allows you to get the running server's paths
- **IServerConfigurationManager** - Allows you to write or read server configuration data into the application paths
- **ITaskManager** - Allows you to execute and manipulate scheduled tasks
- **IUserManager** - Allows you to retrieve user info and user library related info
- **IXmlSerializer** - Allows you to use the main xml serializer
- **IZipClient** - Allows you to use the core zip client for compressing and decompressing data

## 5. Create a Repository

- [See blog post](https://jellyfin.org/posts/plugin-updates/)

## 6. Set Up Debugging

Debugging can be set up by creating tasks which will be executed when running the plugin project. The specifics on setting up these tasks are not included as they may differ from IDE to IDE. The following list describes the general process:

- Compile the plugin in debug mode.
- Create the plugin directory if it doesn't exist.
- Copy the plugin into your server's plugin directory. The server will then execute it.
- Make sure to set the working directory of the program being debugged to the working directory of the Jellyfin Server.
- Start the server.

Some IDEs like Visual Studio Code may need the following compile flags to compile the plugin:

```shell
dotnet build Your-Plugin.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

These flags generate the full paths for file names and **do not** generate a summary during the build process as this may lead to duplicate errors in the problem panel of your IDE.

### 6.a Set Up Debugging on Visual Studio

Visual Studio allows developers to connect to other processes and debug them, setting breakpoints and inspecting the variables of the program. We can set this up following this steps:
On this section we will explain how to set up our solution to enable debugging before the server starts.

1. Right-click on the solution, And click on Add -> Existing Project...
2. Locate Jellyfin executable in your installation folder and click on 'Open'. It is called `Jellyfin.exe`. Now The solution will have a new "Project" called Jellyfin. This is the executable, not the source code of Jellyfin.
3. Right-click on this new project and click on 'Set up as Startup Project'
4. Right-click on this new project and click on 'Properties'
5. Make sure that the 'Attach' parameter is set to 'No'

From now on, everytime you click on start from Visual Studio, it will start Jellyfin attached to the debugger!

The only thing left to do is to compile the project as it is specified a few lines above and you are done.

### 6.b Automate the Setup on Visual Studio Code

Visual Studio Code allows developers to automate the process of starting all necessary dependencies to start debugging the plugin. This guide assumes the reader is familiar with the [documentation on debugging in Visual Studio Code](https://code.visualstudio.com/docs/editor/debugging) and has read the documentation in this file. It is assumed that the Jellyfin Server has already been compiled once. However, should one desire to automatically compile the server before the start of the debugging session, this can be easily implemented, but is not further discussed here.

A full example, which aims to be portable may be found in this repo's `.vscode` folder.

This example expects you to clone `jellyfin`, `jellyfin-web` and `jellyfin-plugin-template` under the same parent directory, though you can customize this in `settings.json`

1. Create a `settings.json` file inside your `.vscode` folder, to specify common options specific to your local setup.
   ```jsonc
    {
        // jellyfinDir : The directory of the cloned jellyfin server project
        // This needs to be built once before it can be used
        "jellyfinDir"     : "${workspaceFolder}/../jellyfin/Jellyfin.Server",
        // jellyfinWebDir : The directory of the cloned jellyfin-web project
        // This needs to be built once before it can be used
        "jellyfinWebDir"  : "${workspaceFolder}/../jellyfin-web",
        // jellyfinDataDir : the root data directory for a running jellyfin instance
        // This is where jellyfin stores its configs, plugins, metadata etc
        // This is platform specific by default, but on Windows defaults to
        // ${env:LOCALAPPDATA}/jellyfin
        "jellyfinDataDir" : "${env:LOCALAPPDATA}/jellyfin",
        // The name of the plugin
        "pluginName" : "Jellyfin.Plugin.Template",
    }
   ```

1. To automate the launch process, create a new `launch.json` file for C# projects inside the `.vscode` folder. The example below shows only the relevant parts of the file. Adjustments to your specific setup and operating system may be required.

   ```jsonc
    {
        // Paths and plugin names are configured in settings.json
        "version": "0.2.0",
        "configurations": [
            {
                "type": "coreclr",
                "name": "Launch",
                "request": "launch",
                "preLaunchTask": "build-and-copy",
                "program": "${config:jellyfinDir}/bin/Debug/net8.0/jellyfin.dll",
                "args": [
                //"--nowebclient"
                "--webdir",
                "${config:jellyfinWebDir}/dist/"
                ],
                "cwd": "${config:jellyfinDir}",
            }
        ]
    }

   ```

   The `request` type is specified as `launch`, as this `launch.json` file will start the Jellyfin Server process. The `preLaunchTask` defines a task that will run before the Jellyfin Server starts. More on this later. It is important to set the `program` path to the Jellyin Server program and set the current working directory (`cwd`) to the working directory of the Jellyfin Server.
   The `args` option allows to specify arguments to be passed to the server, e.g. whether Jellyfin should start with the web-client or without it.

2. Create a `tasks.json` file inside your `.vscode` folder and specify a `build-and-copy` task that will run in `sequence` order. This tasks depends on multiple other tasks and all of those other tasks can be defined as simple `shell` tasks that run commands like the `cp` command to copy a file. The sequence to run those tasks in is given below. Please note that it might be necessary to adjust the examples for your specific setup and operating system.

   The full file is shown here - Specific sections will be discussed in depth
    ```jsonc
    {
        // Paths and plugin name are configured in settings.json
        "version": "2.0.0",
        "tasks": [
            {
            // A chain task - build the plugin, then copy it to your
            // jellyfin server's plugin directory
            "label": "build-and-copy",
            "dependsOrder": "sequence",
            "dependsOn": ["build", "make-plugin-dir", "copy-dll"]
            },
            {
            // Build the plugin
            "label": "build",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "publish",
                "${workspaceFolder}/${config:pluginName}.sln",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "group": "build",
            "presentation": {
                "reveal": "silent"
            },
            "problemMatcher": "$msCompile"
            },
            {
                // Ensure the plugin directory exists before trying to use it
                "label": "make-plugin-dir",
                "type": "shell",
                "command": "mkdir",
                "args": [
                "-Force",
                "-Path",
                "${config:jellyfinDataDir}/plugins/${config:pluginName}/"
                ]
            },
            {
                // Copy the plugin dll to the jellyfin plugin install path
                // This command copies every .dll from the build directory to the plugin dir
                // Usually, you probablly only need ${config:pluginName}.dll
                // But some plugins may bundle extra requirements
                "label": "copy-dll",
                "type": "shell",
                "command": "cp",
                "args": [
                "./${config:pluginName}/bin/Debug/net8.0/publish/*",
                "${config:jellyfinDataDir}/plugins/${config:pluginName}/"
                ]

            },
        ]
    }

    ```
    1.  The "build-and-copy" task which triggers all of the other tasks
    ```jsonc
        {
        // A chain task - build the plugin, then copy it to your
        // jellyfin server's plugin directory
        "label": "build-and-copy",
        "dependsOrder": "sequence",
        "dependsOn": ["build", "make-plugin-dir", "copy-dll"]
        },
    ```
    2.  A build task. This task builds the plugin without generating summary, but with full paths for file names enabled.

        ```jsonc
            {
            // Build the plugin
            "label": "build",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "publish",
                "${workspaceFolder}/${config:pluginName}.sln",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "group": "build",
            "presentation": {
                "reveal": "silent"
            },
            "problemMatcher": "$msCompile"
            },
        ```

    3.  A tasks which creates the necessary plugin directory and a sub-folder for the specific plugin. The plugin directory is located below the [data directory](https://jellyfin.org/docs/general/administration/configuration.html) of the Jellyfin Server. As an example, the following path can be used for the bookshelf plugin: `$HOME/.local/share/jellyfin/plugins/Bookshelf/`
        ```jsonc
            {
                // Ensure the plugin directory exists before trying to use it
                "label": "make-plugin-dir",
                "type": "shell",
                "command": "mkdir",
                "args": [
                "-Force",
                "-Path",
                "${config:jellyfinDataDir}/plugins/${config:pluginName}/"
                ]
            },
        ```

    4.  A tasks which copies the plugin dll which has been built in step 2.1. The file is copied into it's specific plugin directory within the server's plugin directory.

        ```jsonc
            {
                // Copy the plugin dll to the jellyfin plugin install path
                // This command copies every .dll from the build directory to the plugin dir
                // Usually, you probablly only need ${config:pluginName}.dll
                // But some plugins may bundle extra requirements
                "label": "copy-dll",
                "type": "shell",
                "command": "cp",
                "args": [
                "./${config:pluginName}/bin/Debug/net8.0/publish/*",
                "${config:jellyfinDataDir}/plugins/${config:pluginName}/"
                ]
            },
        ```

## Debugging the Broadcast Plugin Locally (Full Environment Setup)

The following steps set up a complete Jellyfin development environment so you can live‑debug and iterate on the `Broadcast` plugin.

### 1. Clone Repositories Side by Side
Assuming a parent workspace directory (e.g. `~/dev`):

```bash
mkdir -p ./dev && cd ./dev
# Jellyfin server source
git clone https://github.com/jellyfin/jellyfin.git
# Jellyfin web client (optional if you want the latest dev web UI)
git clone https://github.com/jellyfin/jellyfin-web.git
```

Directory layout should now look like:
```
~/dev/
  jellyfin/
  jellyfin-web/
  broadcast-plugin/   (this repo)
```

### 2. Build Jellyfin Server Once
```bash
cd ./dev/jellyfin
dotnet build Jellyfin.Server -c Debug
```
This restores packages and produces `Jellyfin.Server/bin/Debug/net8.0/jellyfin.dll` (the server entry assembly).

### 3. (Optional) Build jellyfin-web
If you want a local web client:
```bash
cd ./dev/jellyfin-web
# Install dependencies (choose one depending on what the repo uses)
npm install
# or: yarn install
# Build distribution assets
npm run build
# or: yarn build
```
The build outputs a `dist/` folder used as `--webdir` when launching the server.

### 4. Build the Broadcast Plugin
From the plugin repo root:
```bash
cd ./Jellyfin.Plugin.Broadcast
# Regular build
dotnet build -c Debug
# Or publish (produces trimmed output folder)
dotnet publish Jellyfin.Plugin.Broadcast.sln -c Debug -o ./publish
```
Artifacts of interest:
```
Jellyfin.Plugin.Broadcast/bin/Debug/net8.0/Jellyfin.Plugin.Broadcast.dll
Jellyfin.Plugin.Broadcast/bin/Debug/net8.0/Jellyfin.Plugin.Broadcast.pdb
```

### 5. Determine Jellyfin Data Directory
On Linux the default data directory (after first server run) is:
```
~/.local/share/jellyfin
```
If it does not yet exist, start the server once (step 7) then stop it.

Plugin install path pattern:
```
~/.local/share/jellyfin/plugins/Broadcast/
```

### 6. Copy Plugin Files
```bash
#mkdir -p ~/.local/share/jellyfin/plugins/Broadcast
cp Jellyfin.Plugin.Broadcast/bin/Debug/net8.0/Jellyfin.Plugin.Broadcast.* ~/.local/share/jellyfin/plugins/Broadcast/
```
Repeat this copy step after each rebuild (or use the automated tasks below).

### 7. Launch Jellyfin Server for Debugging
Minimal launch (server uses packaged web UI):
```bash
cd ./dev/jellyfin/Jellyfin.Server
dotnet ./bin/Debug/net8.0/jellyfin.dll
```
With custom web client:
```bash
cd ./dev/jellyfin/Jellyfin.Server
dotnet ./bin/Debug/net9.0/jellyfin.dll --webdir ../../jellyfin-web/dist/
```
After startup, access the UI (default): http://localhost:8096

### 8. Verify Plugin Loaded
In the Jellyfin dashboard navigate to: Admin Dashboard -> Plugins. The `Broadcast` plugin should appear. If not, check:
- DLL present at `~/.local/share/jellyfin/plugins/Broadcast/Jellyfin.Plugin.Broadcast.dll`
- Correct target framework (`net8.0`).
- Server restarted after copy.

### 9. Test Plugin API Endpoints
The plugin exposes two endpoints:
- GET schedule: `GET /Broadcast/Schedule`
- Regenerate schedule: `POST /Broadcast/Schedule/Regenerate`

Using `curl` (adjust port if different):
```bash
# Fetch (auto-generates if empty)
curl -s http://localhost:8096/Broadcast/Schedule | jq .
# Force regenerate
curl -X POST -s http://localhost:8096/Broadcast/Schedule/Regenerate -o /dev/null -w '%{http_code}\n'
# Fetch again
curl -s http://localhost:8096/Broadcast/Schedule | jq .
```
If you get an empty schedule ensure you configured libraries and saved settings in the plugin configuration page.

### 10. Live Debugging in VS Code
A ready-made `.vscode` setup can automate build + copy + launch. Create the files as shown below (already provided in this template if you generated them).

#### `.vscode/settings.json`
```jsonc
{
  // Adjust paths if you renamed directories
  "jellyfinDir": "${workspaceFolder}/../jellyfin/Jellyfin.Server",
  "jellyfinWebDir": "${workspaceFolder}/../jellyfin-web",
  "jellyfinDataDir": "${env:HOME}/.local/share/jellyfin",
  "pluginName": "Jellyfin.Plugin.Broadcast"
}
```

#### `.vscode/tasks.json`
```jsonc
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build-and-copy",
      "dependsOrder": "sequence",
      "dependsOn": ["build-plugin", "ensure-plugin-dir", "copy-plugin"]
    },
    {
      "label": "build-plugin",
      "type": "shell",
      "command": "dotnet",
      "args": [
        "build",
        "${workspaceFolder}/Jellyfin.Plugin.Broadcast.sln",
        "-c",
        "Debug",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "group": "build",
      "problemMatcher": "$msCompile",
      "presentation": { "reveal": "silent" }
    },
    {
      "label": "ensure-plugin-dir",
      "type": "shell",
      "command": "bash",
      "args": ["-c", "mkdir -p \"${config:jellyfinDataDir}/plugins/Broadcast\""]
    },
    {
      "label": "copy-plugin",
      "type": "shell",
      "command": "bash",
      "args": [
        "-c",
        "cp \"${workspaceFolder}/Jellyfin.Plugin.Broadcast/bin/Debug/net8.0/Jellyfin.Plugin.Broadcast.*\" \"${config:jellyfinDataDir}/plugins/Broadcast/\""
      ]
    }
  ]
}
```

#### `.vscode/launch.json`
```jsonc
{
  "version": "0.2.0",
  "configurations": [
    {
      "type": "coreclr",
      "name": "Launch Jellyfin with Broadcast Plugin",
      "request": "launch",
      "preLaunchTask": "build-and-copy",
      "program": "${config:jellyfinDir}/bin/Debug/net8.0/jellyfin.dll",
      "args": [
        "--webdir",
        "${config:jellyfinWebDir}/dist/"
      ],
      "cwd": "${config:jellyfinDir}",
      "console": "internalConsole"
    }
  ]
}
```

Start debugging (F5): VS Code builds the plugin, copies DLL/PDB, then launches Jellyfin with the local web client. Set breakpoints inside plugin code (e.g. `ScheduleManager.GenerateSchedule`).

### 11. Common Pitfalls
| Issue | Resolution |
|-------|------------|
| Plugin not listed | Confirm DLL path & restart server. Clear old cached plugin DLLs. |
| Schedule always empty | Ensure `EnabledLibraryIds` configured and libraries contain Episodes/Movies. |
| 404 on endpoints | Verify base URL & port, ensure server loaded the plugin (check dashboard). |
| Breakpoints not hit | Copy PDB file alongside DLL; ensure Debug build; restart after changes. |

### 12. Cleanup / Reset
To remove the plugin:
```bash
rm -rf ~/.local/share/jellyfin/plugins/Broadcast
```
Restart Jellyfin.

### 13. Use as IPTV Tuner (Live TV)
This plugin now exposes an IPTV-compatible M3U and XMLTV EPG so you can add it to Jellyfin’s Live TV as a tuner.

- Playlist (M3U):
  - http://localhost:8096/Broadcast/iptv/playlist.m3u
- EPG (XMLTV):
  - http://localhost:8096/Broadcast/iptv/epg.xml
- Stream endpoint (used by the playlist):
  - http://localhost:8096/Broadcast/iptv/stream?channelId=broadcast-1

Steps:
1. Ensure the Broadcast plugin is installed and has some Enabled Libraries configured.
2. Generate the schedule once (Dashboard -> Plugins -> Broadcast -> Regenerate) or it will be auto-generated on first use.
3. In the Jellyfin dashboard, go to Live TV -> Tuners -> Add, select M3U tuner and set the playlist URL above.
4. Add an XMLTV guide provider with the EPG URL above.
5. Finish setup; the single channel “Broadcast Channel” will appear with the schedule generated from your libraries.

Notes:
- The stream endpoint currently serves the scheduled item’s source file with range support; Jellyfin may probe/transcode as needed.
- The EPG covers the current schedule window; regenerate to refresh.
- You can expand to multiple channels by adding additional virtual channels and filtering libraries in future enhancements.

## Licensing

Licensing is a complex topic. This repository features a GPLv3 license template that can be used to provide a good default license for your plugin. You may alter this if you like, but if you do a permissive license must be chosen.

Due to how plugins in Jellyfin work, when your plugin is compiled into a binary, it will link against the various Jellyfin binary NuGet packages. These packages are licensed under the GPLv3. Thus, due to the nature and restrictions of the GPL, the binary plugin you get will also be licensed under the GPLv3.

If you accept the default GPLv3 license from this template, all will be good. However if you choose a different license, please keep this fact in mind, as it might not always be obvious that an, e.g. MIT-licensed plugin would become GPLv3 when compiled.

Please note that this also means making "proprietary", source-unavailable, or otherwise "hidden" plugins for public consumption is not permitted. To build a Jellyfin plugin for distribution to others, it must be under the GPLv3 or a permissive open-source license that can be linked against the GPLv3.
