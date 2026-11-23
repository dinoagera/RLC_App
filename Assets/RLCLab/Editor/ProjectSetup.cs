using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RLCLab.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Laboratory.unity";
        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !BuildPipeline.isBuildingPlayer)
                    Configure();
            };
        }

        [MenuItem("RLC Lab/Подготовить проект")]
        public static void Configure()
        {
            PlayerSettings.companyName = "RLC Laboratory";
            PlayerSettings.productName = "RLC Lab";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            // Legacy input is sufficient; no external Input System package required.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null && input.intValue != 0) { input.intValue = 0; settings.ApplyModifiedProperties(); }

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                Scene previous = SceneManager.GetActiveScene();
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var marker = new GameObject("RLC Lab — interface is created at runtime");
                marker.AddComponent<UIController>();
                SceneManager.MoveGameObjectToScene(marker, scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                AssetDatabase.Refresh();
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("RLC Lab/Открыть лабораторию")]
        public static void OpenLaboratory()
        {
            Configure();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("RLC Lab/Собрать Windows x64")]
        public static void BuildWindows()
        {
            Configure();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Установите Windows Build Support (Mono) для этой версии Unity через Unity Hub.");
            string output = Environment.GetEnvironmentVariable("RLC_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output)) output = "Builds/Windows/RLC Lab.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result + ". See Unity log.");
            Directory.CreateDirectory("Builds");
            File.WriteAllText("Builds/windows-build-report.json", JsonUtility.ToJson(new BuildInfo
            {
                unityVersion = Application.unityVersion,
                target = report.summary.platform.ToString(),
                result = report.summary.result.ToString(),
                executable = Path.GetFullPath(output),
                timestampUtc = DateTime.UtcNow.ToString("O"),
                outputBytes = (long)report.summary.totalSize,
                elapsedSeconds = report.summary.totalTime.TotalSeconds,
                warnings = (int)report.summary.totalWarnings,
                errors = (int)report.summary.totalErrors
            }, true));
            Debug.Log("RLC Lab Windows x64: " + Path.GetFullPath(output));
        }

        [Serializable]
        private sealed class BuildInfo
        {
            public string unityVersion, target, result, executable, timestampUtc;
            public long outputBytes;
            public double elapsedSeconds;
            public int warnings, errors;
        }
    }
}
