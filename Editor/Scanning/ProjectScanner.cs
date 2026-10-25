using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Fiascko.ReverseFiascko.Editor.Core;

namespace Fiascko.ReverseFiascko.Editor.Scanning
{
    public static class ProjectScanner
    {
        public static List<ScanResult> Scan()
        {
            BuildOptions options = BuildOptions.None;

            if (EditorUserBuildSettings.development)
            {
                options |= BuildOptions.Development;
            }

            if (EditorUserBuildSettings.allowDebugging)
            {
                options |= BuildOptions.AllowDebugging;
            }

            return Scan(EditorUserBuildSettings.activeBuildTarget, options);
        }

        public static List<ScanResult> Scan(BuildTarget buildTarget, BuildOptions options)
        {
            List<ScanResult> results = new List<ScanResult>();

            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            if (targetGroup == BuildTargetGroup.Unknown)
            {
                results.Add(new ScanResult(
                    Constants.BuildTargetName,
                    Constants.UnknownBuildTargetMessage,
                    ScanStatus.NotChecked
                ));

                return results;
            }

            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(target);

            ScanIl2CppSupport(buildTarget, results);
            ScanScriptingBackend(backend, results);
            ScanCompilerConfiguration(target, backend, results);
            ScanManagedStripping(target, results);
            ScanDevelopmentBuild(options, results);
            ScanStackTraces(results);

            return results;
        }

        private static void ScanIl2CppSupport(BuildTarget buildTarget, List<ScanResult> results)
        {
            bool isActiveTarget = buildTarget == EditorUserBuildSettings.activeBuildTarget;

            Il2CppStatus status = isActiveTarget ? Il2CppSupport.Check() : Il2CppStatus.Unknown;

            ScanStatus scanStatus;

            switch (status)
            {
                case Il2CppStatus.Available:
                    scanStatus = ScanStatus.Protected;
                    break;

                case Il2CppStatus.Missing:
                    scanStatus = ScanStatus.Warning;
                    break;

                case Il2CppStatus.Unsupported:
                case Il2CppStatus.Unknown:
                default:
                    scanStatus = ScanStatus.NotChecked;
                    break;
            }

            string message = isActiveTarget
                ? Il2CppSupport.GetMessage(status)
                : Constants.Il2CppDifferentTargetMessage;

            results.Add(new ScanResult(
                Constants.Il2CppSupportName,
                message,
                scanStatus
            ));
        }

        private static void ScanScriptingBackend(ScriptingImplementation backend, List<ScanResult> results)
        {
            bool isProtected = backend == ScriptingImplementation.IL2CPP;

            results.Add(new ScanResult(
                Constants.ScriptingBackendName,
                string.Format(Constants.CurrentValueFormat, backend),
                GetProtectionStatus(isProtected)
            ));
        }

        private static void ScanCompilerConfiguration(
            NamedBuildTarget target,
            ScriptingImplementation backend,
            List<ScanResult> results)
        {
            if (backend != ScriptingImplementation.IL2CPP)
            {
                results.Add(new ScanResult(
                    Constants.Il2CppCompilerName,
                    Constants.Il2CppNotEnabledMessage,
                    ScanStatus.NotChecked
                ));

                return;
            }

            Il2CppCompilerConfiguration configuration = PlayerSettings.GetIl2CppCompilerConfiguration(target);

            bool isProtected = configuration == Il2CppCompilerConfiguration.Master;

            results.Add(new ScanResult(
                Constants.Il2CppCompilerName,
                string.Format(Constants.CurrentValueFormat, configuration),
                GetProtectionStatus(isProtected)
            ));
        }

        private static void ScanManagedStripping(NamedBuildTarget target, List<ScanResult> results)
        {
            ManagedStrippingLevel level = PlayerSettings.GetManagedStrippingLevel(target);

            bool isProtected = level == ManagedStrippingLevel.Medium || level == ManagedStrippingLevel.High;

            results.Add(new ScanResult(
                Constants.ManagedStrippingName,
                string.Format(Constants.CurrentValueFormat, level),
                GetProtectionStatus(isProtected)
            ));
        }

        private static void ScanDevelopmentBuild(BuildOptions options, List<ScanResult> results)
        {
            bool development = (options & BuildOptions.Development) != 0;
            bool debugging = (options & BuildOptions.AllowDebugging) != 0;

            bool isProtected = !development && !debugging;

            string details = string.Format(
                Constants.BuildConfigurationFormat,
                development,
                debugging
            );

            results.Add(new ScanResult(
                Constants.ReleaseBuildName,
                details,
                GetProtectionStatus(isProtected)
            ));
        }

        private static void ScanStackTraces(List<ScanResult> results)
        {
            List<string> fullTraces = new List<string>();

            foreach (LogType logType in Constants.ScannedLogTypes)
            {
                if (PlayerSettings.GetStackTraceLogType(logType) == StackTraceLogType.Full)
                {
                    fullTraces.Add(logType.ToString());
                }
            }

            bool isProtected = fullTraces.Count == 0;

            string details = isProtected
                ? Constants.NoFullStackTracesMessage
                : string.Format(
                    Constants.FullStackTracesFormat,
                    string.Join(", ", fullTraces)
                );

            results.Add(new ScanResult(
                Constants.StackTracesName,
                details,
                GetProtectionStatus(isProtected)
            ));
        }

        private static ScanStatus GetProtectionStatus(bool isProtected)
        {
            return isProtected ? ScanStatus.Protected : ScanStatus.Warning;
        }
    }
}
