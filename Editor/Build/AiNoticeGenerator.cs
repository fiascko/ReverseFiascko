using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Fiascko.ReverseFiascko.Editor.Core;

namespace Fiascko.ReverseFiascko.Editor.Build
{
    public class AiNoticeGenerator : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            ValidationSettings settings = ValidationSettings.instance;

            if (!settings.EnableAiNotice)
            {
                return;
            }

            if (report.summary.result == BuildResult.Failed ||
                report.summary.result == BuildResult.Cancelled)
            {
                return;
            }

            if ((report.summary.options & BuildOptions.Development) != 0)
            {
                return;
            }

            string outputPath = report.summary.outputPath;

            if (string.IsNullOrEmpty(outputPath) ||
                (!File.Exists(outputPath) && !Directory.Exists(outputPath)))
            {
                return;
            }

            try
            {
                string noticeContent = settings.GetAiNoticeContent();

                string externalDirectory = GetBuildDirectory(outputPath);
                string internalDirectory = GetInternalNoticeDirectory(outputPath);

                WriteNotice(externalDirectory, noticeContent);
                WriteNotice(internalDirectory, noticeContent);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(Constants.FormatLogMessage(string.Format(
                    Constants.AiNoticeErrorFormat,
                    Constants.AiNoticeGenerationFailedMessage,
                    exception.Message
                )));
            }
        }

        private static void WriteNotice(string directory, string content)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            string noticePath = Path.Combine(
                directory,
                Constants.AiNoticeFileName
            );

            if (!Directory.Exists(directory))
            {
                Debug.LogWarning(Constants.FormatLogMessage(string.Format(
                    Constants.AiNoticeErrorFormat,
                    Constants.AiNoticeGenerationFailedMessage,
                    noticePath
                )));

                return;
            }

            try
            {
                File.WriteAllText(noticePath, content, new UTF8Encoding(false));

                Debug.Log(Constants.FormatLogMessage(string.Format(
                    Constants.AiNoticeGeneratedPathFormat,
                    Constants.AiNoticeGeneratedMessage,
                    noticePath
                )));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(Constants.FormatLogMessage(string.Format(
                    Constants.AiNoticeErrorFormat,
                    Constants.AiNoticeGenerationFailedMessage,
                    noticePath + " - " + exception.Message
                )));
            }
        }

        private static string GetInternalNoticeDirectory(string buildPath)
        {
            string fullPath = Path.GetFullPath(buildPath);

            if (fullPath.EndsWith(
                Constants.MacApplicationExtension,
                StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(fullPath, "Contents", "Resources");
            }

            if (fullPath.EndsWith(Constants.WindowsExecutableExtension, StringComparison.OrdinalIgnoreCase))
            {
                string directory = Path.GetDirectoryName(fullPath);
                string name = Path.GetFileNameWithoutExtension(fullPath);

                return Path.Combine(directory, name + Constants.WindowsDataDirectorySuffix);
            }

            return null;
        }

        private static string GetBuildDirectory(string outputPath)
        {
            string fullPath = Path.GetFullPath(outputPath);

            if (fullPath.EndsWith(
                Constants.MacApplicationExtension,
                StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetDirectoryName(fullPath);
            }

            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }

            return Path.GetDirectoryName(fullPath);
        }
    }
}
