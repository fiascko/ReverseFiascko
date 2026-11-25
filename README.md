# ReverseFiascko

**Lightweight. No external dependencies. No runtime overhead. No false promises.**

ReverseFiascko is a lightweight Unity Editor toolkit designed to help developers harden their builds and make reverse engineering more difficult.

It analyzes Unity project settings, applies recommended protection configurations, validates builds, scans project files for potentially embedded secrets and checks build outputs for potentially sensitive files.

ReverseFiascko does not prevent reverse engineering. Its goal is to raise the difficulty, especially against casual attackers and script kiddies, while encouraging good Unity build security practices.

**Make reverse engineering harder. Keep your builds cleaner.**

## The Philosophy

ReverseFiascko is built around two ideas: **make Unity build hardening simple and promote good security practices without unnecessary complexity.**

- No external dependencies or third-party packages
- No runtime components or performance overhead
- No accounts or cloud services
- No tracking
- No unnecessary complexity

ReverseFiascko uses Unity's built-in Editor APIs and build settings.

Everything runs locally inside the Unity Editor.

**Your project. Your build. Your control.**

## Features

- Scan Unity project protection settings, including IL2CPP, scripting backend, compiler configuration and Managed Stripping Level
- Check Development Build, Script Debugging and Stack Trace settings
- Detect potentially embedded secrets, including API keys, access tokens and passwords, without exposing their values
- Apply recommended protection settings automatically
- Validate project settings before building with Off, Warning and Strict modes
- Scan completed Windows and macOS builds for potentially sensitive files
- Detect debug symbols, source files, project files, configuration files and backups
- Identify managed DLL assemblies that may require manual review
- Identify Unity diagnostic and backup directories
- Display scan results and security recommendations
- Generate an optional AI Analysis Notice for release builds with standard or custom content
- Run entirely inside the Unity Editor without external dependencies or runtime overhead

## How It Works

ReverseFiascko focuses on four areas.

### Project Protection

Scan your Unity project to identify settings and potential embedded secrets that may compromise build security.

ReverseFiascko checks the scripting backend, IL2CPP compiler, Managed Stripping Level, development settings and stack traces.

It also scans supported files inside the `Assets` directory for potentially hardcoded API keys, access tokens, passwords and other sensitive credentials.

Potential secrets are reported with file paths and line numbers without exposing their values. Detection is pattern-based and requires manual review.

Files larger than 1 MB are skipped and reported. Incomplete scans are identified in the results.

Use **Apply Protection** to configure the recommended protection settings automatically.

Settings that cannot be verified are reported rather than assumed to be secure.

### Build Validation

Choose how ReverseFiascko handles protection warnings before building.

- **Off** — Disable automatic build validation.
- **Warning** — Report protection issues without blocking the build.
- **Strict** — Block release builds when protection warnings are detected.

Development builds are excluded from automatic validation.

### Build Scanner

Scan completed Windows and macOS builds to identify potentially sensitive files that may have been included in a public release.

ReverseFiascko detects:

- Debug symbols (`.pdb`, `.mdb`, `.dSYM`)
- Source and project files (`.cs`, `.csproj`, `.sln`)
- Potentially sensitive files (`.env`, `.pem`, `.key`, `.p12`, `.pfx`)
- Environment configuration variants (such as `.env.local` and `.env.production`)
- Potentially sensitive configuration files (`appsettings.json`, `credentials.json`)
- Backup files (`.bak`, `.backup`, `.old`)
- Managed DLL assemblies
- Unity diagnostic and backup directories

Each detected file is reported with its location, potential risk and recommended action.

Managed DLL assemblies are identified by inspecting their PE headers and are marked as ReviewRequired. Their presence does not necessarily indicate a security vulnerability.

Unity diagnostic and backup directories are reported separately. Potentially sensitive files inside these directories are counted without generating individual warnings. DLLs in these directories are not inspected.

Scan results distinguish between warnings, findings requiring manual review and conditions that could not be verified.

The scanner does not decompile assemblies, modify files or automatically remove detected items.

Other file detection is based on filenames, extensions and directory structures. A warning does not necessarily indicate a security vulnerability, and legitimate files may be required by the application.

### AI Analysis Notice

Optionally generate an `AI_ANALYSIS_NOTICE.txt` file for release builds using standard or custom content.

On Windows and macOS, the notice is generated in two locations:
- Alongside the game executable or application.
- Inside the Unity build structure (`_Data` directory on Windows or `Contents/Resources` inside the `.app` bundle on macOS).

The notice communicates that the developer does not authorize unauthorized reverse engineering or analysis of the software.

It may discourage certain AI-powered analysis tools from performing unauthorized analysis, depending on how those tools are configured.

This feature is an informational deterrent, not a technical protection mechanism. The notice is not embedded into executable code, and there is no guarantee that AI tools or decompilers will read, recognize or respect it.

It does not prevent reverse engineering or provide guaranteed legal protection.

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

The Build Scanner detects potentially sensitive files using filenames, extensions and directory structures. It also inspects PE headers to identify managed DLL assemblies, without decompiling or analyzing their code.

The Embedded Secrets Scanner analyzes supported project files using pattern-based detection. It cannot guarantee the detection of all hardcoded secrets, credentials or other sensitive information.

Both scanners are intended to assist manual security reviews, not replace them.

**Security is a process, not a checkbox.**

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
4. Enter the following Git repository URL: `https://github.com/fiascko/ReverseFiascko.git`
5. Click **Install**.

**Important:** Make sure the repository URL ends with `.git` when installing through Unity Package Manager.

Alternatively, download the repository and install it as a local package.

No additional dependencies are required.

## Usage

1. Open your Unity project.
2. Navigate to `Tools > ReverseFiascko`.
3. Click **Scan Project** to analyze your current protection settings and scan project files for potentially embedded secrets.
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
│   │   ├── EmbeddedSecretsScanner.cs
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
