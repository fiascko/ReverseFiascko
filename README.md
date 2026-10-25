# ReverseFiascko

**Lightweight. No external dependencies. No runtime overhead. No false promises.**

ReverseFiascko is a lightweight Unity Editor toolkit designed to help developers harden their builds and make reverse engineering more difficult.

It analyzes Unity project settings, applies recommended protection configurations, validates builds and scans build outputs for potentially sensitive files.

ReverseFiascko does not prevent reverse engineering. Its goal is to raise the difficulty, especially against casual attackers and script kiddies, while encouraging good Unity build security practices.

**Make reverse engineering harder. Keep your builds cleaner.**

## The Philosophy

ReverseFiascko is built around two ideas: **make Unity build hardening simple and promote good security practices without unnecessary complexity.**

- No external dependencies
- No third-party packages
- No external frameworks
- No runtime components
- No runtime performance overhead
- No accounts
- No cloud services required by ReverseFiascko
- No tracking
- No unnecessary complexity

ReverseFiascko uses Unity's built-in Editor APIs and build settings.

Everything runs locally inside the Unity Editor.

**Your project. Your build. Your control.**

## Features

- Scan Unity project protection settings
- Check IL2CPP availability, scripting backend and compiler configuration
- Check Managed Stripping Level
- Check Development Build and Script Debugging settings
- Check Stack Trace settings
- Apply recommended protection settings automatically
- Validate project settings before building
- Off, Warning and Strict validation modes
- Scan completed builds for potentially sensitive files
- Detect debug symbols (`.pdb`, `.mdb`, `.dSYM`)
- Detect source and project files (`.cs`, `.csproj`, `.sln`)
- Identify Unity diagnostic and backup directories
- Display scan results and security recommendations
- Generate an optional AI Analysis Notice for release builds
- Standard and custom AI Analysis Notice content
- macOS and Windows build scanning
- Unity Editor-only, with no external dependencies or runtime overhead

## How It Works

ReverseFiascko focuses on three areas.

### Project Protection

Scan your Unity project to identify settings that may make reverse engineering easier.

ReverseFiascko checks the scripting backend, IL2CPP compiler, Managed Stripping Level, development settings and stack traces.

Use **Apply Protection** to configure the recommended settings automatically.

Settings that cannot be verified are reported rather than assumed to be secure.

### Build Validation

Choose how ReverseFiascko handles protection warnings before building.

- **Off** — Disable automatic build validation.
- **Warning** — Report protection issues without blocking the build.
- **Strict** — Block release builds when protection warnings are detected.

Development builds are excluded from automatic validation.

You can also scan completed builds to identify potentially sensitive files, including debug symbols, C# source files and project files.

### AI Analysis Notice

Optionally generate an `AI_ANALYSIS_NOTICE.txt` file alongside release builds.

The notice communicates that the developer does not authorize unauthorized reverse engineering or analysis of the software.

It may also help certain AI-powered analysis tools recognize that reverse engineering is not authorized, potentially triggering warnings, restrictions or additional safeguards.

Depending on how an AI system is configured, the notice may discourage or interrupt automated analysis, making the process more difficult.

You can use the standard notice or provide custom text.

This feature is informational and acts as an additional deterrent. It does not guarantee that AI tools will recognize, respect or enforce the notice, nor does it technically prevent reverse engineering or provide guaranteed legal protection.

## Security Limitations

**ReverseFiascko does not make Unity games impossible to reverse engineer.**

No build configuration can guarantee complete protection against reverse engineering, decompilation, memory inspection or determined attackers.

IL2CPP, Managed Stripping and other recommended settings can increase the effort required to analyze a Unity game, but they do not eliminate the possibility.

ReverseFiascko is primarily intended to:

- Make casual reverse engineering more difficult
- Discourage basic script kiddie techniques
- Reduce unnecessary exposure of source-related files and debug symbols
- Encourage safer release build configurations
- Help developers identify common build security mistakes

ReverseFiascko is not an anti-cheat system, code obfuscator, encryption tool or comprehensive security solution.

A successful scan means that the checked conditions passed. It does not mean that the build is completely secure.

**Security is a process, not a checkbox.**

## Development Status

ReverseFiascko is an ongoing project and will continue to evolve over time.

New features, improvements and additional security checks are planned for future updates.

The goal is to keep ReverseFiascko lightweight, simple and dependency-free while continuously improving its capabilities.

## Privacy by Design

ReverseFiascko is designed to work locally inside the Unity Editor.

There is no account to create, no remote database to connect to and no cloud service required by the toolkit.

ReverseFiascko does not upload your source code, project settings or build files to external services.

It does not collect analytics or track usage.

Unity itself may provide separate online services depending on your project configuration. Those services are independent of ReverseFiascko.

## Technologies

ReverseFiascko intentionally keeps its technology stack small:

- C#
- Unity Editor APIs
- Unity Build Pipeline

That's it.

No external frameworks.  
No third-party packages.  
No external dependencies.  
No runtime components.

The toolkit runs entirely in the Unity Editor and does not add code to your game's runtime.

## Requirements

- Unity 6000.3 or newer
- Unity Editor
- Appropriate Unity build modules for your target platforms

IL2CPP support depends on the installed Unity modules and selected build target.

## Installation

ReverseFiascko is distributed as a Unity Package Manager (UPM) package.

1. Open your Unity project.
2. Open `Window > Package Manager`.
3. Select **Install package from git URL**.
4. Enter the Git repository URL for ReverseFiascko.
5. Install the package.

Alternatively, download the repository and install it as a local package.

No additional dependencies are required.

## Usage

1. Open your Unity project.
2. Navigate to `Tools > ReverseFiascko`.
3. Click **Scan Project** to analyze your current configuration.
4. Review the results and recommendations.
5. Click **Apply Protection** if changes are recommended.
6. Choose your preferred Build Validation mode.
7. Configure the optional AI Analysis Notice.
8. Create a release build.
9. Click **Scan Build** and select the build directory to inspect its contents.

ReverseFiascko does not replace testing your game after changing build settings. Always verify that your release build functions correctly.

## Project Structure

```text
ReverseFiascko/
├── Editor/
│   ├── Build/
│   │   ├── AiNoticeGenerator.cs
│   │   ├── BuildValidator.cs
│   │   └── ProtectionApplier.cs
│   │
│   ├── Core/
│   │   ├── Constants.cs
│   │   └── ValidationSettings.cs
│   │
│   ├── Scanning/
│   │   ├── BuildScanner.cs
│   │   ├── Il2CppSupport.cs
│   │   ├── ProjectScanner.cs
│   │   └── ScanResult.cs
│   │
│   ├── UI/
│   │   └── ReverseFiasckoWindow.cs
│   │
│   └── ReverseFiascko.Editor.asmdef
│
├── .gitattributes
├── .gitignore
├── LICENSE
├── package.json
└── README.md
```

## License

ReverseFiascko is source-available under the MIT License with the Commons Clause.

You are free to use ReverseFiascko for personal or professional purposes, including commercial Unity projects, modify it and redistribute it under the terms of the license.

You may not sell ReverseFiascko itself or offer a product or service whose value derives substantially from ReverseFiascko.

See the `LICENSE` file for the complete license terms.
