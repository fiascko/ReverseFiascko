// Copyright (c) Fiascko. All rights reserved. | Project: ReverseFiascko | File: EmbeddedSecretsScanner.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Fiascko.ReverseFiascko.Editor.Core;

namespace Fiascko.ReverseFiascko.Editor.Scanning
{
    public static class EmbeddedSecretsScanner
    {
        private const int BinarySampleSize = 4096;
        private const int MaxDisplayedFindings = 50;

        private const string SensitiveNamePattern =
            @"(?:api[_-]?key|api[_-]?secret|client[_-]?secret|secret[_-]?key|private[_-]?key|access[_-]?token|auth[_-]?token|refresh[_-]?token|password|passwd|credentials?|secret)";

        private static readonly HashSet<string> _extensions = new HashSet<string>(
            Constants.EmbeddedSecretsExtensions,
            StringComparer.OrdinalIgnoreCase
        );

        private static readonly Regex _secretPattern = new Regex(
            @"(?i)(?<![\w-])" + SensitiveNamePattern +
            @"(?![\w-])\s*[""']?\s*(?:=|:|>)\s*[""']?([^""'<\s,;}]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100)
        );

        private static readonly Regex _xmlSecretPattern = new Regex(
            @"<\s*(?<name>" + SensitiveNamePattern +
            @")\s*>\s*(?<value>[^<\r\n]+?)\s*</\s*\k<name>\s*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100)
        );

        public static ScanResult Scan()
        {
            string assetsPath = Application.dataPath;

            if (!Directory.Exists(assetsPath))
            {
                return new ScanResult(
                    Constants.EmbeddedSecretsName,
                    Constants.EmbeddedSecretsIncompleteMessage,
                    ScanStatus.NotChecked
                );
            }

            List<string> findings = new List<string>();
            List<string> skippedFiles = new List<string>();
            int totalFindings = 0;
            int totalSkippedFiles = 0;
            bool incomplete = false;

            try
            {
                foreach (string file in Directory.EnumerateFiles(
                    assetsPath,
                    "*",
                    SearchOption.AllDirectories))
                {
                    if (!IsSupportedFile(file))
                    {
                        continue;
                    }

                    try
                    {
                        FileInfo info = new FileInfo(file);

                        if (info.Length > Constants.EmbeddedSecretsMaxFileSize)
                        {
                            totalSkippedFiles++;

                            if (skippedFiles.Count < MaxDisplayedFindings)
                            {
                                string relativePath = "Assets" + file.Substring(assetsPath.Length);
                                skippedFiles.Add(relativePath.Replace('\\', '/'));
                            }

                            continue;
                        }

                        if (IsBinaryFile(file))
                        {
                            continue;
                        }

                        ScanFile(file, assetsPath, findings, ref totalFindings);
                    }
                    catch (Exception exception) when (
                        exception is IOException ||
                        exception is UnauthorizedAccessException ||
                        exception is RegexMatchTimeoutException)
                    {
                        incomplete = true;
                    }
                }
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                incomplete = true;
            }

            if (incomplete || totalSkippedFiles > 0)
            {
                string details = Constants.EmbeddedSecretsIncompleteMessage;

                if (totalFindings > 0)
                {
                    details += "\n\n" + string.Format(
                        Constants.EmbeddedSecretsReviewFormat,
                        totalFindings,
                        FormatFindings(findings, totalFindings)
                    );
                }

                if (totalSkippedFiles > 0)
                {
                    details += "\n\n" + string.Format(
                        Constants.EmbeddedSecretsSkippedFormat,
                        totalSkippedFiles,
                        string.Join("\n", skippedFiles)
                    );

                    int hiddenFiles = totalSkippedFiles - skippedFiles.Count;

                    if (hiddenFiles > 0)
                    {
                        details += string.Format(
                            Constants.EmbeddedSecretsAdditionalSkippedFormat,
                            hiddenFiles
                        );
                    }
                }

                return new ScanResult(
                    Constants.EmbeddedSecretsName,
                    details,
                    totalFindings > 0 ? ScanStatus.ReviewRequired : ScanStatus.NotChecked
                );
            }

            if (totalFindings == 0)
            {
                return new ScanResult(
                    Constants.EmbeddedSecretsName,
                    Constants.EmbeddedSecretsClearMessage,
                    ScanStatus.Protected
                );
            }

            return new ScanResult(
                Constants.EmbeddedSecretsName,
                string.Format(
                    Constants.EmbeddedSecretsReviewFormat,
                    totalFindings,
                    FormatFindings(findings, totalFindings)
                ),
                ScanStatus.ReviewRequired
            );
        }
  
        private static bool IsSupportedFile(string file)
        {
            string fileName = Path.GetFileName(file);

            if (fileName.StartsWith(Constants.EnvironmentFilePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return _extensions.Contains(Path.GetExtension(file));
        }
        
        private static bool IsBinaryFile(string file)
        {
            byte[] buffer = new byte[BinarySampleSize];

            using (FileStream stream = File.OpenRead(file))
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead >= 4)
                {
                    bool utf32 = (buffer[0] == 0x00 && buffer[1] == 0x00 &&
                                buffer[2] == 0xFE && buffer[3] == 0xFF) ||
                                (buffer[0] == 0xFF && buffer[1] == 0xFE &&
                                buffer[2] == 0x00 && buffer[3] == 0x00);

                    if (utf32)
                    {
                        return false;
                    }
                }

                if (bytesRead >= 2)
                {
                    bool utf16 = (buffer[0] == 0xFF && buffer[1] == 0xFE) ||
                                (buffer[0] == 0xFE && buffer[1] == 0xFF);

                    if (utf16)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < bytesRead; i++)
                {
                    byte value = buffer[i];

                    if (value < 32 && value != 9 && value != 10 &&
                        value != 12 && value != 13)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void ScanFile(
            string file,
            string assetsPath,
            List<string> findings,
            ref int totalFindings)
        {
            using (StreamReader reader = new StreamReader(file, true))
            {
                string line;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    if (line.IndexOf('\0') >= 0)
                    {
                        return;
                    }

                    string trimmedLine = line.TrimStart();

                    if (trimmedLine.StartsWith("//", StringComparison.Ordinal) ||
                        trimmedLine.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Match xmlMatch = _xmlSecretPattern.Match(line);
                    Match match = xmlMatch.Success ? xmlMatch : _secretPattern.Match(line);

                    if (!match.Success)
                    {
                        continue;
                    }

                    string value = xmlMatch.Success
                        ? xmlMatch.Groups["value"].Value
                        : match.Groups[1].Value;

                    if (IsPlaceholder(value))
                    {
                        continue;
                    }

                    totalFindings++;

                    if (findings.Count >= MaxDisplayedFindings)
                    {
                        continue;
                    }

                    string relativePath = "Assets" + file.Substring(assetsPath.Length);

                    findings.Add(string.Format(
                        Constants.EmbeddedSecretsResultFormat,
                        relativePath.Replace('\\', '/'),
                        lineNumber
                    ));
                }
            }
        }

        private static string FormatFindings(List<string> findings, int totalFindings)
        {
            string details = string.Join("\n", findings);

            int hiddenFindings = totalFindings - findings.Count;

            if (hiddenFindings > 0)
            {
                details += string.Format(
                    Constants.EmbeddedSecretsAdditionalFindingsFormat,
                    hiddenFindings
                );
            }

            return details;
        }

        private static bool IsPlaceholder(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            string normalized = value.Trim().ToLowerInvariant();

            foreach (string placeholder in Constants.PlaceholderValues)
            {
                if (normalized.StartsWith(placeholder, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return IsEnvironmentReference(normalized);
        }

        private static bool IsEnvironmentReference(string value)
        {
            return value.StartsWith("environment.getenvironmentvariable(", StringComparison.Ordinal) ||
                   value.StartsWith("system.environment.getenvironmentvariable(", StringComparison.Ordinal) ||
                   value.StartsWith("environment.expandenvironmentvariables(", StringComparison.Ordinal) ||
                   value.StartsWith("system.environment.expandenvironmentvariables(", StringComparison.Ordinal) ||
                   value.StartsWith("process.env.", StringComparison.Ordinal) ||
                   value.StartsWith("os.getenv(", StringComparison.Ordinal) ||
                   value.StartsWith("os.environ[", StringComparison.Ordinal) ||
                   value.StartsWith("getenv(", StringComparison.Ordinal) ||
                   value.StartsWith("${", StringComparison.Ordinal) ||
                   value.StartsWith("$(", StringComparison.Ordinal) ||
                   (value.StartsWith("$", StringComparison.Ordinal) && value.Length > 1) ||
                   (value.StartsWith("%", StringComparison.Ordinal) &&
                    value.EndsWith("%", StringComparison.Ordinal));
        }
    }
}
