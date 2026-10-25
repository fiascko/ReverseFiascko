using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Fiascko.ReverseFiascko.Editor.Core;
using Fiascko.ReverseFiascko.Editor.Scanning;
using Fiascko.ReverseFiascko.Editor.Build;

namespace Fiascko.ReverseFiascko.Editor.UI
{
    public class ReverseFiasckoWindow : EditorWindow
    {
        private List<ScanResult> _results;
        private List<ScanResult> _buildResults;
        private BuildTarget _scannedBuildTarget;
        private Vector2 _scrollPosition;
        private Vector2 _customNoticeScrollPosition;
        private string _buildPath;
        private string _statusMessage;
        private bool _showAiNoticeSettings;
        private MessageType _statusMessageType = MessageType.Info;

        [MenuItem(Constants.MenuPath)]
        public static void Open()
        {
            GetWindow<ReverseFiasckoWindow>(Constants.PackageName);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Constants.WindowHorizontalPadding);
            EditorGUILayout.BeginVertical();

            DrawHeader();

            DrawProjectProtectionSection();

            EditorGUILayout.Space();
            DrawBuildValidationSection();

            EditorGUILayout.Space();
            DrawAiNoticeSettings();

            GUILayout.Space(Constants.WindowBottomSpacing);
            EditorGUILayout.EndVertical();
            GUILayout.Space(Constants.WindowHorizontalPadding);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(Constants.PackageName, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(Constants.PackageDescription, MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawProjectActionButtons()
        {
            if (GUILayout.Button(
                Constants.ScanButton,
                GUILayout.Height(Constants.ActionButtonHeight)))
            {
                ScanProject();
            }

            bool canApply = CanApplyProtection();

            EditorGUI.BeginDisabledGroup(!canApply);

            if (GUILayout.Button(
                Constants.ApplyButton,
                GUILayout.Height(Constants.ActionButtonHeight)))
            {
                ApplyProtection();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawStatusMessage()
        {
            if (string.IsNullOrEmpty(_statusMessage))
            {
                return;
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(_statusMessage, _statusMessageType);
        }

        private static void DrawValidationSettings()
        {
            ValidationSettings settings = ValidationSettings.instance;

            EditorGUILayout.LabelField(Constants.ValidationModeLabel, EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Constants.ValidationModeDescription, MessageType.Info);

            EditorGUI.BeginChangeCheck();

            ValidationMode mode = (ValidationMode)EditorGUILayout.EnumPopup(settings.Mode, GUILayout.ExpandWidth(false));

            if (EditorGUI.EndChangeCheck())
            {
                settings.Mode = mode;
                settings.SaveSettings();
            }
        }

        private void DrawAiNoticeSettings()
        {
            ValidationSettings settings = ValidationSettings.instance;

            _showAiNoticeSettings = EditorGUILayout.Foldout(
                _showAiNoticeSettings,
                Constants.AiNoticeTitle,
                true
            );

            if (!_showAiNoticeSettings)
            {
                return;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Constants.AiNoticeDescription, MessageType.Info);
            EditorGUI.BeginChangeCheck();

            bool enabled = EditorGUILayout.ToggleLeft(
                Constants.AiNoticeEnabledLabel,
                settings.EnableAiNotice
            );

            if (EditorGUI.EndChangeCheck())
            {
                settings.EnableAiNotice = enabled;
                settings.SaveSettings();
            }

            EditorGUI.BeginDisabledGroup(!settings.EnableAiNotice);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Constants.AiNoticeModeLabel, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            AiNoticeMode mode =
                (AiNoticeMode)EditorGUILayout.EnumPopup(
                    settings.AiNoticeMode,
                    GUILayout.ExpandWidth(false)
                );

            if (EditorGUI.EndChangeCheck())
            {
                settings.AiNoticeMode = mode;
                settings.SaveSettings();
            }

            EditorGUILayout.Space();

            if (settings.AiNoticeMode == AiNoticeMode.Custom)
            {
                DrawCustomAiNotice(settings);
            }
            else
            {
                DrawStandardAiNotice();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawCustomAiNotice(ValidationSettings settings)
        {
            EditorGUILayout.LabelField(Constants.AiNoticeCustomContentLabel, EditorStyles.boldLabel);

            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };

            _customNoticeScrollPosition =
                EditorGUILayout.BeginScrollView(
                    _customNoticeScrollPosition,
                    GUILayout.Height(
                        Constants.CustomNoticeEditorHeight
                    )
                );

            EditorGUI.BeginChangeCheck();

            string content = EditorGUILayout.TextArea(
                settings.CustomAiNotice ?? string.Empty,
                textAreaStyle,
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            );

            if (EditorGUI.EndChangeCheck())
            {
                settings.CustomAiNotice = content;
                settings.SaveSettings();
            }

            EditorGUILayout.EndScrollView();

            if (string.IsNullOrWhiteSpace(settings.CustomAiNotice))
            {
                EditorGUILayout.HelpBox(
                    Constants.AiNoticeEmptyWarning,
                    MessageType.Warning
                );
            }
        }

        private static void DrawStandardAiNotice()
        {
            EditorGUILayout.LabelField(Constants.AiNoticePreviewLabel, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Constants.AiNoticeStandardContent, MessageType.None);
        }

        private void SelectBuildFolder()
        {
            string initialDirectory = Application.dataPath;

            if (!string.IsNullOrEmpty(_buildPath))
            {
                string directory = Directory.Exists(_buildPath)
                    ? _buildPath
                    : Path.GetDirectoryName(_buildPath);

                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    initialDirectory = directory;
                }
            }

            string path = EditorUtility.OpenFolderPanel(
                Constants.SelectBuildDirectoryTitle,
                initialDirectory,
                string.Empty
            );

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ScanBuild(path);
        }

        private void ScanProject()
        {
            _buildResults = null;
            _buildPath = null;
            _scrollPosition = Vector2.zero;

            RefreshResults();

            CountScanResults(_results, out int warnings, out int uncheckedResults);

            _statusMessage = string.Format(
                Constants.ProjectScanCompletedFormat,
                Constants.ScanCompletedMessage,
                warnings,
                uncheckedResults
            );

            _statusMessageType = GetSummaryMessageType(warnings, uncheckedResults);
        }

        private void ScanBuild(string path)
        {
            _results = null;
            _buildResults = null;
            _scrollPosition = Vector2.zero;

            _buildPath = path;

            _buildResults = BuildScanner.Scan(path);

            CountScanResults(_buildResults, out int warnings, out int uncheckedResults);

            _statusMessage = string.Format(
                Constants.BuildScanCompletedFormat,
                Constants.BuildScanCompletedMessage,
                warnings,
                uncheckedResults
            );

            _statusMessageType = GetSummaryMessageType(warnings, uncheckedResults);
        }

        private void ApplyProtection()
        {
            if (!CanApplyProtection())
            {
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                Constants.ConfirmationTitle,
                Constants.ConfirmationMessage,
                Constants.ConfirmButton,
                Constants.CancelButton
            );

            if (!confirmed)
            {
                return;
            }

            if (ProtectionApplier.Apply())
            {
                RefreshResults();
                CountScanResults(_results, out int warnings, out int uncheckedResults);

                _statusMessage = warnings > 0 || uncheckedResults > 0
                    ? Constants.ProtectionPartiallyAppliedMessage
                    : Constants.ProtectionFullyAppliedMessage;

                _statusMessageType = GetSummaryMessageType(warnings, uncheckedResults);
            }
        }

        private bool CanApplyProtection()
        {
            return _results != null &&
                   _scannedBuildTarget ==
                       EditorUserBuildSettings.activeBuildTarget &&
                   _results.Exists(result =>
                       result.Status == ScanStatus.Warning);
        }

        private void RefreshResults()
        {
            _results = ProjectScanner.Scan();
            _scannedBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        }

        private void DrawProjectResults()
        {
            if (_scannedBuildTarget != EditorUserBuildSettings.activeBuildTarget)
            {
                EditorGUILayout.HelpBox(
                    Constants.PlatformChangedMessage,
                    MessageType.Warning
                );
            }

            EditorGUILayout.LabelField(Constants.ScanResultsTitle, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            foreach (ScanResult result in _results)
            {
                DrawResult(result);
            }
        }

        private void DrawBuildResults()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Constants.BuildScanResultsTitle, EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Constants.BuildDirectoryName, EditorStyles.boldLabel);

            EditorGUILayout.SelectableLabel(
                _buildPath,
                EditorStyles.wordWrappedLabel,
                GUILayout.MinHeight(
                    Constants.BuildPathMinHeight
                )
            );

            EditorGUILayout.Space();

            foreach (ScanResult result in _buildResults)
            {
                DrawResult(result);
            }
        }

        private void DrawProjectProtectionSection()
        {
            EditorGUILayout.LabelField(
                Constants.ProjectProtectionLabel,
                EditorStyles.boldLabel
            );

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(
                Constants.ActivePlatformLabel,
                EditorUserBuildSettings.activeBuildTarget.ToString()
            );

            EditorGUILayout.Space();

            DrawProjectActionButtons();

            if (_results != null)
            {
                DrawStatusMessage();
                DrawProjectResults();
            }
            else if (_buildResults == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    Constants.StatusLabel,
                    Constants.NotScannedStatus
                );
            }
        }

        private void DrawBuildValidationSection()
        {
            DrawValidationSettings();

            EditorGUILayout.Space();

            if (GUILayout.Button(
                Constants.ScanBuildButton,
                GUILayout.Height(Constants.ActionButtonHeight)))
            {
                SelectBuildFolder();
            }

            if (_buildResults != null)
            {
                DrawStatusMessage();
                DrawBuildResults();
            }
        }

        private static void DrawResult(ScanResult result)
        {
            MessageType messageType = GetMessageType(result.Status);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            if (result.Status == ScanStatus.Protected)
            {
                GUIContent icon = EditorGUIUtility.IconContent(Constants.ProtectedIconName);

                if (icon != null && icon.image != null)
                {
                    GUILayout.Label(
                        icon,
                        GUILayout.Width(
                            Constants.ResultIconSize
                        ),
                        GUILayout.Height(
                            Constants.ResultIconSize
                        )
                    );
                }
            }

            EditorGUILayout.LabelField(result.Name, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(result.Status.ToString(), EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(result.Details, messageType);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(Constants.ResultSpacing);
        }

        private static MessageType GetMessageType(ScanStatus status)
        {
            switch (status)
            {
                case ScanStatus.Warning:
                    return MessageType.Warning;

                default:
                    return MessageType.None;
            }
        }

        private static MessageType GetSummaryMessageType(int warnings, int uncheckedResults)
        {
            return warnings > 0 || uncheckedResults > 0
                ? MessageType.Warning
                : MessageType.Info;
        }

        private static void CountScanResults(List<ScanResult> results, out int warnings, out int uncheckedResults)
        {
            warnings = 0;
            uncheckedResults = 0;

            foreach (ScanResult result in results)
            {
                if (result.Status == ScanStatus.Warning)
                {
                    warnings++;
                }
                else if (result.Status == ScanStatus.NotChecked)
                {
                    uncheckedResults++;
                }
            }
        }
    }
}
