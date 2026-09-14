using UnityEngine;

// Hace transparentes los arboles que se colocan entre la camara y el jugador.
// La comprobacion usa el rectangulo proyectado del arbol, por lo que tambien
// funciona con copas grandes que no tienen un collider en las hojas.
[DisallowMultipleComponent]
public sealed class XunjuuTreeCameraOccluder : MonoBehaviour
{
    [SerializeField, Range(0.05f, 0.8f)] private float occludedAlpha = 0.24f;
    [SerializeField, Range(0.02f, 0.8f)] private float fadeSpeed = 0.22f;
    [SerializeField, Range(0f, 0.25f)] private float screenPadding = 0.035f;
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.12f;

    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Camera sceneCamera;
    private Transform player;
    private float nextRefreshTime;
    private bool shouldFade;

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnEnable()
    {
        CacheRenderers();
        nextRefreshTime = 0f;
    }

    private void OnDisable()
    {
        RestoreOpacity();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + refreshInterval;
            shouldFade = IsBlockingPlayerView();
        }

        if (renderers == null || originalColors == null)
            CacheRenderers();

        float targetAlpha = shouldFade ? XunjuuSceneVisualPolicy.OccludedVisibility : 1f;
        float step = Mathf.Clamp01(fadeSpeed * 60f * Time.unscaledDeltaTime);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Color target = originalColors[i];
            target.a = targetAlpha;
            renderer.color = Color.Lerp(renderer.color, target, step);
        }
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    private bool IsBlockingPlayerView()
    {
        sceneCamera = sceneCamera != null ? sceneCamera : Camera.main;
        if (sceneCamera == null)
            return false;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (player == null || !player.gameObject.activeInHierarchy || !isActiveAndEnabled)
            return false;

        Bounds bounds = CalculateBounds();
        Vector3 treeScreen = sceneCamera.WorldToViewportPoint(bounds.center);
        Vector3 playerScreen = sceneCamera.WorldToViewportPoint(player.position + Vector3.up * 0.55f);

        // El arbol debe estar delante del jugador desde el punto de vista de la camara.
        float treeDepth = Vector3.Dot(bounds.center - sceneCamera.transform.position, sceneCamera.transform.forward);
        float playerDepth = Vector3.Dot(player.position - sceneCamera.transform.position, sceneCamera.transform.forward);
        if (treeDepth <= 0f || playerDepth <= 0f || treeDepth >= playerDepth - 0.15f)
            return false;

        Vector3[] corners = new Vector3[8];
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(min.x, min.y, max.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(min.x, max.y, max.z);
        corners[4] = new Vector3(max.x, min.y, min.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(max.x, max.y, min.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

        float minX = 1f;
        float maxX = 0f;
        float minY = 1f;
        float maxY = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 projected = sceneCamera.WorldToViewportPoint(corners[i]);
            minX = Mathf.Min(minX, projected.x);
            maxX = Mathf.Max(maxX, projected.x);
            minY = Mathf.Min(minY, projected.y);
            maxY = Mathf.Max(maxY, projected.y);
        }

        return playerScreen.z > 0f
            && playerScreen.x >= minX - screenPadding
            && playerScreen.x <= maxX + screenPadding
            && playerScreen.y >= minY - screenPadding
            && playerScreen.y <= maxY + screenPadding;
    }

    private Bounds CalculateBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool initialized = false;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
                bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    private void RestoreOpacity()
    {
        if (renderers == null || originalColors == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = originalColors[i];
        }
    }
}
