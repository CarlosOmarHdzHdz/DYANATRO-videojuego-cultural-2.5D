using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================================
// Xunjuu v0.1 - Control global de resolucion
// ACCION: iniciar el juego a resolucion nativa y mantener la interfaz nitida.
// COMPATIBILIDAD: Windows y Android.
// ============================================================================
[DefaultExecutionOrder(-10000)]
public sealed class XunjuuDisplayController : MonoBehaviour
{
    private static XunjuuDisplayController instance;
    private const int WindowedWidth = 1280;
    private const int WindowedHeight = 720;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject controller = new GameObject("Xunjuu_DisplayController");
        instance = controller.AddComponent<XunjuuDisplayController>();
        DontDestroyOnLoad(controller);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyNativeResolution();
    }

    private IEnumerator Start()
    {
        yield return null;
        ConfigureCanvases();
    }

    private void Update()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // ACCION: permitir alternar pantalla completa con F11 durante las pruebas.
        if (Input.GetKeyDown(KeyCode.F11))
            ToggleFullscreen();
#endif
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RefreshSceneDisplay());
    }

    private IEnumerator RefreshSceneDisplay()
    {
        yield return null;
        ConfigureCanvases();
    }

    private void ApplyNativeResolution()
    {
        ScalableBufferManager.ResizeBuffers(1f, 1f);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        Resolution nativeResolution = Screen.currentResolution;
        Screen.SetResolution(
            Mathf.Max(1280, nativeResolution.width),
            Mathf.Max(720, nativeResolution.height),
            FullScreenMode.FullScreenWindow);
#elif UNITY_ANDROID && !UNITY_EDITOR
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
#endif
    }

    private void ConfigureCanvases()
    {
        CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CanvasScaler scaler in scalers)
        {
            if (scaler == null)
                continue;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Canvas canvas = scaler.GetComponent<Canvas>();
            if (canvas != null && canvas.isRootCanvas)
                canvas.pixelPerfect = true;
        }
    }

    private void ToggleFullscreen()
    {
        bool isFullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;
        if (isFullscreen)
        {
            Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
            return;
        }

        ApplyNativeResolution();
    }
}
