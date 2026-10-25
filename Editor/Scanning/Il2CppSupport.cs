using System;
using System.IO;
using UnityEditor;
using Fiascko.ReverseFiascko.Editor.Core;

namespace Fiascko.ReverseFiascko.Editor.Scanning
{
    public static class Il2CppSupport
    {
        public static Il2CppStatus Check()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

            if (group == BuildTargetGroup.Unknown)
            {
                return Il2CppStatus.Unknown;
            }

            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                return Il2CppStatus.Unsupported;
            }

            if (!IsDesktopTarget(target))
            {
                return Il2CppStatus.Unknown;
            }

            try
            {
                string playerDirectory = BuildPipeline.GetPlaybackEngineDirectory(target, BuildOptions.None);
                string variationsDirectory = Path.Combine(playerDirectory, Constants.Il2CppVariationsDirectory);

                if (!Directory.Exists(variationsDirectory))
                {
                    return Il2CppStatus.Unknown;
                }

                foreach (string variation in Directory.EnumerateDirectories(variationsDirectory))
                {
                    string variationName = Path.GetFileName(variation);

                    if (variationName.IndexOf(
                        Constants.Il2CppVariationMarker,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return Il2CppStatus.Available;
                    }
                }

                return Il2CppStatus.Missing;
            }
            catch (Exception)
            {
                return Il2CppStatus.Unknown;
            }
        }

        private static bool IsDesktopTarget(BuildTarget target)
        {
            return target == BuildTarget.StandaloneOSX ||
                   target == BuildTarget.StandaloneWindows ||
                   target == BuildTarget.StandaloneWindows64 ||
                   target == BuildTarget.StandaloneLinux64;
        }

        public static string GetMessage(Il2CppStatus status)
        {
            switch (status)
            {
                case Il2CppStatus.Available:
                    return Constants.Il2CppAvailableMessage;

                case Il2CppStatus.Missing:
                    return Constants.Il2CppMissingMessage;

                case Il2CppStatus.Unsupported:
                    return Constants.Il2CppUnsupportedMessage;

                default:
                    return Constants.Il2CppUnknownMessage;
            }
        }
    }
}
