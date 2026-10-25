using UnityEditor;

namespace Fiascko.ReverseFiascko.Editor.Core
{
    [FilePath(Constants.SettingsFilePath, FilePathAttribute.Location.ProjectFolder)]
    public class ValidationSettings : ScriptableSingleton<ValidationSettings>
    {
        public ValidationMode Mode = ValidationMode.Warning;
        public bool EnableAiNotice = true;
        public AiNoticeMode AiNoticeMode = AiNoticeMode.Standard;
        public string CustomAiNotice = Constants.AiNoticeStandardContent;

        public string GetAiNoticeContent()
        {
            if (AiNoticeMode == AiNoticeMode.Custom && !string.IsNullOrWhiteSpace(CustomAiNotice))
            {
                return CustomAiNotice;
            }

            return Constants.AiNoticeStandardContent;
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }
}
