// Copyright (c) Fiascko. All rights reserved. | Project: ReverseFiascko | File: BuildScanner.cs
using System;
using System.Collections.Generic;
using System.IO;
using Fiascko.ReverseFiascko.Editor.Core;

namespace Fiascko.ReverseFiascko.Editor.Scanning
{
    public static class BuildScanner
    {
        private static readonly HashSet<string> SensitiveExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Constants.DebugSymbolsExtension,
                Constants.MonoDebugSymbolsExtension,
                Constants.CSharpSourceExtension,
                Constants.CSharpProjectExtension,
                Constants.VisualStudioSolutionExtension,
                Constants.EnvironmentFileExtension,
                Constants.PemFileExtension,
                Constants.KeyFileExtension,
                Constants.P12FileExtension,
                Constants.PfxFileExtension,
                Constants.BackupFileExtension,
                Constants.BackupLongFileExtension,
                Constants.OldFileExtension
            };

        public static List<ScanResult> Scan(string buildPath)
        {
            List<ScanResult> results = new List<ScanResult>();

            if (string.IsNullOrWhiteSpace(buildPath) || !Directory.Exists(buildPath))
            {
                results.Add(new ScanResult(
                    Constants.BuildDirectoryName,
                    Constants.InvalidBuildDirectoryMessage,
                    ScanStatus.Warning
                ));

                return results;
            }

            int scannedFiles = 0;
            int detectedFiles = 0;
            int warningFiles = 0;
            int reviewRequiredFiles = 0;
            int uncheckedFiles = 0;
            int diagnosticFiles = 0;
            List<string> diagnosticDirectories = new List<string>();

            try
            {
                DetectApplication(
                    buildPath,
                    out string buildType,
                    out string applicationName,
                    out string applicationPath
                );

                bool applicationContentsScanned = false;

                foreach (string file in Directory.EnumerateFiles(
                    buildPath,
                    "*",
                    SearchOption.AllDirectories))
                {
                    scannedFiles++;

                    if (!string.IsNullOrEmpty(applicationPath) && IsInsideDirectory(applicationPath, file))
                    {
                        applicationContentsScanned = true;
                    }

                    string extension = Path.GetExtension(file);
                    string fileName = Path.GetFileName(file);

                    if (IsDebugSymbolDirectory(file))
                    {
                        continue;
                    }

                    bool isSensitiveFile = SensitiveExtensions.Contains(extension) ||
                        IsSensitiveFileName(fileName);

                    if (IsDiagnosticFile(buildPath, file))
                    {
                        if (isSensitiveFile || extension.Equals(Constants.ManagedAssemblyExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            diagnosticFiles++;
                        }

                        continue;
                    }

                    bool isManagedAssembly = false;

                    if (extension.Equals(Constants.ManagedAssemblyExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            isManagedAssembly = IsManagedAssembly(file);
                        }
                        catch (IOException exception)
                        {
                            uncheckedFiles++;

                            results.Add(new ScanResult(
                                fileName,
                                string.Format(
                                    Constants.ManagedAssemblyReadErrorFormat,
                                    exception.Message,
                                    Constants.FilePathLabel,
                                    GetRelativePath(buildPath, file)
                                ),
                                ScanStatus.NotChecked
                            ));

                            continue;
                        }
                        catch (UnauthorizedAccessException exception)
                        {
                            uncheckedFiles++;

                            results.Add(new ScanResult(
                                fileName,
                                string.Format(
                                    Constants.ManagedAssemblyReadErrorFormat,
                                    exception.Message,
                                    Constants.FilePathLabel,
                                    GetRelativePath(buildPath, file)
                                ),
                                ScanStatus.NotChecked
                            ));

                            continue;
                        }
                    }

                    if (!isSensitiveFile && !isManagedAssembly)
                    {
                        continue;
                    }

                    detectedFiles++;

                    ScanStatus fileStatus = isManagedAssembly
                        ? ScanStatus.ReviewRequired
                        : ScanStatus.Warning;

                    if (isManagedAssembly)
                    {
                        reviewRequiredFiles++;
                    }
                    else
                    {
                        warningFiles++;
                    }

                    string details = isManagedAssembly
                        ? FormatFileInformation(
                            Constants.ManagedAssemblyType,
                            Constants.ManagedAssemblyRisk,
                            Constants.ManagedAssemblyRecommendation
                        )
                        : GetFileInformation(extension, fileName);

                    string relativePath = GetRelativePath(buildPath, file);

                    results.Add(new ScanResult(
                        Path.GetFileName(file),
                        string.Format(
                            Constants.BuildScanFileDetailsFormat,
                            details,
                            Constants.FilePathLabel,
                            relativePath
                        ),
                        fileStatus
                    ));
                }

                foreach (string directory in Directory.EnumerateDirectories(
                    buildPath,
                    "*",
                    SearchOption.AllDirectories))
                {
                    string directoryName = Path.GetFileName(directory);

                    if ((directoryName.IndexOf(Constants.DiagnosticDirectoryMarker, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        directoryName.IndexOf(Constants.BackupDirectoryMarker, StringComparison.OrdinalIgnoreCase) >= 0) &&
                        !IsDiagnosticFile(buildPath, directory))
                    {
                        diagnosticDirectories.Add(GetRelativePath(buildPath, directory));
                    }

                    if (!directory.EndsWith(
                        Constants.MacDebugSymbolsExtension,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string parentDirectory = Path.GetDirectoryName(directory);

                    if (!string.IsNullOrEmpty(parentDirectory) && IsDebugSymbolDirectory(parentDirectory))
                    {
                        continue;
                    }

                    if (IsDiagnosticFile(buildPath, directory))
                    {
                        diagnosticFiles++;
                        continue;
                    }

                    detectedFiles++;
                    warningFiles++;

                    string details = FormatFileInformation(
                        Constants.MacDebugSymbolsType,
                        Constants.MacDebugSymbolsRisk,
                        Constants.MacDebugSymbolsRecommendation
                    );

                    string relativePath = GetRelativePath(buildPath, directory);

                    results.Add(new ScanResult(
                        Path.GetFileName(directory),
                        string.Format(
                            Constants.BuildScanFileDetailsFormat,
                            details,
                            Constants.FilePathLabel,
                            relativePath
                        ),
                        ScanStatus.Warning
                    ));
                }

                string summary = string.Format(
                    Constants.BuildScanSummaryDetailsFormat,
                    Constants.BuildTypeLabel,
                    buildType,
                    Constants.ApplicationDetectedLabel,
                    applicationName,
                    Constants.FilesScannedLabel,
                    scannedFiles,
                    Constants.ApplicationContentsScannedLabel,
                    applicationContentsScanned
                        ? Constants.YesValue
                        : Constants.NoValue,
                    string.Format(
                        Constants.BuildScanSummaryFormat,
                        scannedFiles,
                        detectedFiles
                    )
                );

                ScanStatus status;

                if (warningFiles > 0)
                {
                    status = ScanStatus.Warning;
                }
                else if (reviewRequiredFiles > 0)
                {
                    status = ScanStatus.ReviewRequired;
                }
                else if (uncheckedFiles > 0 || applicationPath == null)
                {
                    status = ScanStatus.NotChecked;
                }
                else
                {
                    status = ScanStatus.Protected;
                }

                results.Insert(0, new ScanResult(
                    Constants.BuildScanName,
                    summary,
                    status
                ));

                if (diagnosticDirectories.Count > 0)
                {
                    diagnosticDirectories.Sort(StringComparer.OrdinalIgnoreCase);

                    results.Add(new ScanResult(
                        Constants.DiagnosticFilesName,
                        string.Format(
                            Constants.DiagnosticSummaryFormat,
                            diagnosticFiles,
                            Constants.DiagnosticFilesMessage,
                            string.Join("\n", diagnosticDirectories)
                        ),
                        ScanStatus.NotChecked
                    ));
                }
            }
            catch (Exception exception)
            {
                results.Insert(0, new ScanResult(
                    Constants.BuildScanErrorName,
                    Constants.BuildScanIncompleteMessage +
                    "\n" + exception.Message,
                    ScanStatus.Warning
                ));
            }

            return results;
        }

        private static void DetectApplication(
            string buildPath,
            out string buildType,
            out string applicationName,
            out string applicationPath)
        {
            buildType = Constants.UnknownBuildType;
            applicationName = Constants.NoApplicationDetected;
            applicationPath = null;

            if (IsMacApplication(buildPath))
            {
                buildType = Constants.MacApplicationBuildType;
                applicationName = Path.GetFileName(
                    buildPath.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar
                    )
                );
                applicationPath = buildPath;
                return;
            }

            foreach (string directory in Directory.EnumerateDirectories(
                buildPath,
                "*",
                SearchOption.TopDirectoryOnly))
            {
                if (!IsMacApplication(directory))
                {
                    continue;
                }

                buildType = Constants.MacApplicationBuildType;
                applicationName = Path.GetFileName(directory);
                applicationPath = directory;
                return;
            }

            foreach (string file in Directory.EnumerateFiles(
                buildPath,
                "*" + Constants.WindowsExecutableExtension,
                SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string dataDirectory = Path.Combine(buildPath, name + Constants.WindowsDataDirectorySuffix);

                if (!Directory.Exists(dataDirectory))
                {
                    continue;
                }

                buildType = Constants.WindowsBuildType;
                applicationName = Path.GetFileName(file);
                applicationPath = dataDirectory;
                return;
            }
        }

        private static bool IsMacApplication(string path)
        {
            return path.EndsWith(
                       Constants.MacApplicationExtension,
                       StringComparison.OrdinalIgnoreCase
                   ) &&
                   Directory.Exists(Path.Combine(
                       path,
                       Constants.MacApplicationContentsDirectory
                   ));
        }

        private static bool IsManagedAssembly(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (stream.Length < Constants.PeHeaderOffset + sizeof(int))
                {
                    return false;
                }

                if (reader.ReadUInt16() != Constants.PeDosSignature)
                {
                    return false;
                }

                stream.Position = Constants.PeHeaderOffset;
                int peOffset = reader.ReadInt32();

                if (peOffset < Constants.PeHeaderOffset + sizeof(int) ||
                    (long)peOffset + sizeof(uint) + Constants.PeCoffHeaderSize > stream.Length)
                {
                    return false;
                }

                stream.Position = peOffset;

                if (reader.ReadUInt32() != Constants.PeSignature)
                {
                    return false;
                }

                stream.Position += Constants.PeOptionalHeaderSizeOffset;
                ushort optionalHeaderSize = reader.ReadUInt16();
                stream.Position += sizeof(ushort);

                long optionalHeaderStart = stream.Position;

                if (optionalHeaderSize < Constants.PeOptionalHeaderMagicSize ||
                    optionalHeaderStart + optionalHeaderSize > stream.Length)
                {
                    return false;
                }

                ushort magic = reader.ReadUInt16();

                int dataDirectoryOffset;
                int directoryCountOffset;

                switch (magic)
                {
                    case Constants.Pe32Magic:
                        dataDirectoryOffset = Constants.Pe32DataDirectoryOffset;
                        directoryCountOffset = Constants.Pe32DirectoryCountOffset;
                        break;

                    case Constants.Pe32PlusMagic:
                        dataDirectoryOffset = Constants.Pe32PlusDataDirectoryOffset;
                        directoryCountOffset = Constants.Pe32PlusDirectoryCountOffset;
                        break;

                    default:
                        return false;
                }

                int cliDirectoryOffset = dataDirectoryOffset +
                    Constants.PeCliDirectoryIndex * Constants.PeDataDirectorySize;

                if (optionalHeaderSize < cliDirectoryOffset + Constants.PeDataDirectorySize)
                {
                    return false;
                }

                stream.Position = optionalHeaderStart + directoryCountOffset;

                if (reader.ReadUInt32() <= Constants.PeCliDirectoryIndex)
                {
                    return false;
                }

                stream.Position = optionalHeaderStart + cliDirectoryOffset;

                uint cliHeaderRva = reader.ReadUInt32();
                uint cliHeaderSize = reader.ReadUInt32();

                return cliHeaderRva != 0 && cliHeaderSize != 0;
            }
        }

        private static bool IsInsideDirectory(string directory, string file)
        {
            string normalizedDirectory = NormalizeDirectoryPath(directory);
            string normalizedFile = Path.GetFullPath(file);

            return normalizedFile.StartsWith(normalizedDirectory, GetPathComparison());
        }

        private static bool IsDiagnosticFile(string root, string file)
        {
            string relativePath = GetRelativePath(root, file);
            string directoryPath = Path.GetDirectoryName(relativePath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }

            string[] directories = directoryPath.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );

            foreach (string directory in directories)
            {
                if (directory.IndexOf(
                        Constants.DiagnosticDirectoryMarker,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    directory.IndexOf(
                        Constants.BackupDirectoryMarker,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSensitiveFileName(string fileName)
        {
            return fileName.Equals(Constants.EnvironmentFileName, StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith(Constants.EnvironmentFilePrefix, StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals(Constants.AppSettingsFileName, StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals(Constants.CredentialsFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetFileInformation(string extension, string fileName)
        {
            if (fileName.Equals(Constants.EnvironmentFileName, StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith(Constants.EnvironmentFilePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return FormatFileInformation(
                    Constants.EnvironmentFileType,
                    Constants.EnvironmentFileRisk,
                    Constants.EnvironmentFileRecommendation
                );
            }

            if (fileName.Equals(Constants.AppSettingsFileName, StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals(Constants.CredentialsFileName, StringComparison.OrdinalIgnoreCase))
            {
                return FormatFileInformation(
                    Constants.ConfigurationFileType,
                    Constants.ConfigurationFileRisk,
                    Constants.ConfigurationFileRecommendation
                );
            }

            switch (extension.ToLowerInvariant())
            {
                case Constants.DebugSymbolsExtension:
                    return FormatFileInformation(
                        Constants.DebugSymbolsType,
                        Constants.DebugSymbolsRisk,
                        Constants.DebugSymbolsRecommendation
                    );
                case Constants.MonoDebugSymbolsExtension:
                    return FormatFileInformation(
                        Constants.MonoDebugSymbolsType,
                        Constants.MonoDebugSymbolsRisk,
                        Constants.MonoDebugSymbolsRecommendation
                    );
                case Constants.CSharpSourceExtension:
                    return FormatFileInformation(
                        Constants.CSharpSourceType,
                        Constants.CSharpSourceRisk,
                        Constants.CSharpSourceRecommendation
                    );
                case Constants.CSharpProjectExtension:
                    return FormatFileInformation(
                        Constants.CSharpProjectType,
                        Constants.CSharpProjectRisk,
                        Constants.CSharpProjectRecommendation
                    );
                case Constants.VisualStudioSolutionExtension:
                    return FormatFileInformation(
                        Constants.VisualStudioSolutionType,
                        Constants.VisualStudioSolutionRisk,
                        Constants.VisualStudioSolutionRecommendation
                    );
                case Constants.EnvironmentFileExtension:
                case Constants.PemFileExtension:
                case Constants.KeyFileExtension:
                case Constants.P12FileExtension:
                case Constants.PfxFileExtension:
                    return FormatFileInformation(
                        Constants.SecretFileType,
                        Constants.SecretFileRisk,
                        Constants.SecretFileRecommendation
                    );
                case Constants.BackupFileExtension:
                case Constants.BackupLongFileExtension:
                case Constants.OldFileExtension:
                    return FormatFileInformation(
                        Constants.BackupFileType,
                        Constants.BackupFileRisk,
                        Constants.BackupFileRecommendation
                    );
                default:
                    return FormatFileInformation(
                        Constants.UnknownFileType,
                        Constants.UnknownFileRisk,
                        Constants.UnknownFileRecommendation
                    );
            }
        }

        private static string FormatFileInformation(string type, string risk, string recommendation)
        {
            return string.Format(
                Constants.BuildScanFileInformationFormat,
                Constants.FileTypeLabel,
                type,
                Constants.FileRiskLabel,
                risk,
                Constants.FileRecommendationLabel,
                recommendation
            );
        }

        private static bool IsDebugSymbolDirectory(string file)
        {
            string[] directories = file.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );

            foreach (string directory in directories)
            {
                if (directory.EndsWith(
                    Constants.MacDebugSymbolsExtension,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetRelativePath(string root, string file)
        {
            string normalizedRoot = NormalizeDirectoryPath(root);
            string normalizedFile = Path.GetFullPath(file);

            return normalizedFile.StartsWith(
                normalizedRoot,
                GetPathComparison()
            )
                ? normalizedFile.Substring(normalizedRoot.Length)
                : normalizedFile;
        }

        private static string NormalizeDirectoryPath(string directory)
        {
            return Path.GetFullPath(directory).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            ) + Path.DirectorySeparatorChar;
        }

        private static StringComparison GetPathComparison()
        {
            return Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }
    }
}
