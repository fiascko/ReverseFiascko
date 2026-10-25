using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Fiascko.ReverseFiascko.Editor.Core;
using Fiascko.ReverseFiascko.Editor.Scanning;

namespace Fiascko.ReverseFiascko.Editor.Build
{
    public class BuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidationMode mode = ValidationSettings.instance.Mode;

            if (mode == ValidationMode.Off)
            {
                return;
            }

            BuildTarget buildTarget = report.summary.platform;
            BuildOptions options = report.summary.options;

            if ((options & BuildOptions.Development) != 0)
            {
                Debug.Log(Constants.FormatLogMessage(Constants.ValidationSkippedMessage));
                return;
            }

            List<ScanResult> results = ProjectScanner.Scan(buildTarget, options);

            List<string> issues = GetProtectionIssues(results);

            if (issues.Count == 0)
            {
                Debug.Log(Constants.FormatLogMessage(Constants.ValidationPassedMessage));
                return;
            }

            string title = mode == ValidationMode.Strict
                ? Constants.ValidationFailedMessage
                : Constants.ValidationWarningMessage;

            string message = Constants.FormatLogMessage(string.Format(
                Constants.ValidationIssuesFormat,
                title,
                string.Join("\n", issues)
            ));

            if (mode == ValidationMode.Strict)
            {
                throw new BuildFailedException(message);
            }

            Debug.LogWarning(message);
        }

        private static List<string> GetProtectionIssues(List<ScanResult> results)
        {
            List<string> issues = new List<string>();

            foreach (ScanResult result in results)
            {
                if (result.Status != ScanStatus.Warning)
                {
                    continue;
                }

                issues.Add(string.Format(
                    Constants.ValidationIssueFormat,
                    result.Name,
                    result.Details
                ));
            }

            return issues;
        }
    }
}
