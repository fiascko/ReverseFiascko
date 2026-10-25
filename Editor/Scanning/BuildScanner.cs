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
                Constants.VisualStudioSolutionExtension
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
            int diagnosticFiles = 0;

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

                    if (IsDebugSymbolDirectory(file) || !SensitiveExtensions.Contains(extension))
                    {
                        continue;
                    }

                    if (IsDiagnosticFile(buildPath, file))
                    {
                        diagnosticFiles++;
                        continue;
                    }

                    detectedFiles++;

                    string details = GetFileInformation(extension);
                    string relativePath = GetRelativePath(buildPath, file);

                    results.Add(new ScanResult(
                        Path.GetFileName(file),
                        string.Format(
                            Constants.BuildScanFileDetailsFormat,
                            details,
                            Constants.FilePathLabel,
                            relativePath
                        ),
                        ScanStatus.Warning
                    ));
                }
                
                foreach (string directory in Directory.EnumerateDirectories(
                    buildPath,
                    "*",
                    SearchOption.AllDirectories))
                {
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

                if (detectedFiles > 0)
                {
                    status = ScanStatus.Warning;
                }
                else if (applicationPath == null)
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

                if (diagnosticFiles > 0)
                {
                    results.Add(new ScanResult(
                        Constants.DiagnosticFilesName,
                        string.Format(
                            Constants.DiagnosticSummaryFormat,
                            diagnosticFiles,
                            Constants.DiagnosticFilesMessage
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

        private static string GetFileInformation(string extension)
        {
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
