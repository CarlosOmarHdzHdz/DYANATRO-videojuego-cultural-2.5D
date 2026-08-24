using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// ============================================================================
// Xunjuu beta v0.1 - Distribucion para Android y Windows
// ACCION: generar un APK o una carpeta EXE usando las escenas habilitadas.
// MODIFICACION: cambia nombres, identificador y rutas en las constantes.
// ============================================================================
public static class AndroidBuildScript
{
    private const string AppName = "Xunjuu";
    private const string AndroidOutputDirectory = "Builds/Android";
    private const string AndroidOutputFile = "Xunjuu_v0.1.apk";
    private const string WindowsOutputDirectory = "Builds/Windows/Xunjuu_v0.1";
    private const string WindowsOutputFile = "Xunjuu.exe";
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Build/Xunjuu/Build Android APK")]
    public static void BuildAndroidApk()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError(
                "No se puede generar el APK porque esta instalacion de Unity no tiene Android Build Support. " +
                "Instala en Unity Hub, para Unity 6000.3.11f1: Android Build Support, Android SDK & NDK Tools y OpenJDK."
            );
            EditorUtility.DisplayDialog(
                "Falta Android Build Support",
                "Unity no puede crear el APK porque falta el modulo Android Build Support para Unity 6000.3.11f1.\n\n" +
                "Instala desde Unity Hub:\n" +
                "- Android Build Support\n" +
                "- Android SDK & NDK Tools\n" +
                "- OpenJDK\n\n" +
                "Despues vuelve a usar Build > Xunjuu > Build Android APK.",
                "Entendido"
            );
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, AndroidOutputDirectory, AndroidOutputFile);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        PlayerSettings.productName = AppName;
        PlayerSettings.companyName = "Proyecto Escolar";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.proyectoescolar.xunjuu");
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("No hay escenas activadas en Build Settings.");
            return;
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("APK generado: " + outputPath);
        }
        else
        {
            Debug.LogError("No se pudo generar el APK. Resultado: " + report.summary.result);
        }
    }

    // ACCION: crear una carpeta portable de Windows para laboratorios escolares.
    [MenuItem("Build/Xunjuu/Build Windows EXE %&w")]
    public static void BuildWindowsExe()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            Debug.LogError("Xunjuu v0.1: falta Windows Build Support en Unity Hub.");
            return;
        }

        string windowsPlayerPath = Path.Combine(
            EditorApplication.applicationContentsPath,
            "PlaybackEngines",
            "WindowsStandaloneSupport",
            "Variations",
            "win64_player_nondevelopment_mono",
            "WindowsPlayer.exe"
        );
        if (!File.Exists(windowsPlayerPath))
        {
            Debug.LogError(
                "Xunjuu v0.1: Windows Build Support esta incompleto. " +
                "Agrega el modulo Windows Build Support (Mono) a Unity 6000.3.11f1 desde Unity Hub."
            );
            EditorUtility.DisplayDialog(
                "Falta Windows Build Support",
                "El proyecto esta preparado, pero esta instalacion de Unity no contiene WindowsPlayer.exe.\n\n" +
                "En Unity Hub abre Installs > Unity 6000.3.11f1 > Add modules e instala " +
                "Windows Build Support (Mono). Despues vuelve a usar Build > Xunjuu > Build Windows EXE.",
                "Entendido"
            );
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, WindowsOutputDirectory, WindowsOutputFile);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        ConfigureCommonPlayerSettings();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        // Xunjuu v0.1 - ACCION: Mono evita exigir el modulo Windows IL2CPP.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[XUNJUU BUILD PASS] EXE generado: " + outputPath);
        else
            Debug.LogError("Xunjuu v0.1: no se pudo generar el EXE. Resultado: " + report.summary.result);
    }

    // ACCION: dejar SampleScene habilitada para que otra PC compile sin ajustes.
    [MenuItem("Build/Xunjuu/Preparar Build Settings")]
    public static void PrepareBuildSettings()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
        ConfigureCommonPlayerSettings();
        Debug.Log("Xunjuu v0.1: SampleScene esta lista para APK y EXE.");
    }

    private static void ConfigureCommonPlayerSettings()
    {
        PlayerSettings.productName = AppName;
        PlayerSettings.companyName = "Proyecto Escolar";
        PlayerSettings.bundleVersion = "0.1";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabledScenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.enabled)
                enabledScenes.Add(scene.path);
        }

        if (enabledScenes.Count == 0 && File.Exists(MainScenePath))
        {
            PrepareBuildSettings();
            enabledScenes.Add(MainScenePath);
        }

        return enabledScenes.ToArray();
    }
}
