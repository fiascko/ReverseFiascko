using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Fiascko.ReverseFiascko.Editor.Core;
using Fiascko.ReverseFiascko.Editor.Scanning;

namespace Fiascko.ReverseFiascko.Editor.Build
{
    public static class ProtectionApplier
    {
        public static bool Apply()
        {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            if (targetGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogError(Constants.FormatLogMessage(Constants.UnknownBuildTargetError));
                return false;
            }

            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(targetGroup);

            ApplyIl2CppSettings(target);

            PlayerSettings.SetManagedStrippingLevel(target, Constants.RecommendedStrippingLevel);

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            ApplyStackTraceSettings();

            return true;
        }

        private static void ApplyIl2CppSettings(NamedBuildTarget target)
        {
            Il2CppStatus status = Il2CppSupport.Check();

            if (status != Il2CppStatus.Available)
            {
                Debug.LogWarning(Constants.FormatLogMessage(Il2CppSupport.GetMessage(status)));
                return;
            }

            PlayerSettings.SetScriptingBackend(target, Constants.RecommendedScriptingBackend);

            PlayerSettings.SetIl2CppCompilerConfiguration(target, Constants.RecommendedIl2CppCompiler);
        }

        private static void ApplyStackTraceSettings()
        {
            foreach (LogType logType in Constants.ScannedLogTypes)
            {
                PlayerSettings.SetStackTraceLogType(logType, GetRecommendedStackTrace(logType));
            }
        }

        private static StackTraceLogType GetRecommendedStackTrace(LogType logType)
        {
            switch (logType)
            {
                case LogType.Log:
                case LogType.Warning:
                    return Constants.RecommendedStandardStackTrace;
                default:
                    return Constants.RecommendedErrorStackTrace;
            }
        }
    }
}
