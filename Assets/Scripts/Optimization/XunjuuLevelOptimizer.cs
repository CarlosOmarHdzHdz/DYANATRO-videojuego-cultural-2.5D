using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Optimizador del nivel 1
// Acción: reducir renderizado y colisiones lejanas con un único controlador.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuLevelOptimizer : MonoBehaviour
{
    [Header("Xunjuú v0.1 - Distancias")]
    [SerializeField, Min(20f)] private float renderDistance = 145f;
    [SerializeField, Min(5f)] private float collisionDistance = 30f;
    [SerializeField, Min(0.1f)] private float updateInterval = 0.45f;
    [SerializeField, Min(1f)] private float refreshObjectsInterval = 6f;
    [SerializeField] private bool keepTreesVisible = true;

    private sealed class Entry
    {
        public XunjuuHitInteractable interactable;
        public Renderer[] renderers;
        public Collider[] colliders;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private Transform player;
    private float nextUpdate;
    private float nextRefresh;

    private void Start()
    {
        Application.targetFrameRate = 60;
        ResolvePlayer();
        RefreshObjects();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
            RefreshObjects();

        if (Time.unscaledTime < nextUpdate || player == null)
            return;

        nextUpdate = Time.unscaledTime + updateInterval;
        UpdateVisibility();
    }

    // ACCIÓN: volver a registrar árboles y cultivos creados durante la partida.
    [ContextMenu("Xunjuú v0.1/Actualizar objetos optimizados")]
    public void RefreshObjects()
    {
        ResolvePlayer();
        entries.Clear();
        XunjuuHitInteractable[] interactables = FindObjectsByType<XunjuuHitInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (XunjuuHitInteractable interactable in interactables)
        {
            if (interactable == null)
                continue;

            List<Renderer> optimizedRenderers = new List<Renderer>();
            foreach (Renderer renderer in interactable.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.GetComponent<TextMesh>() == null)
                    optimizedRenderers.Add(renderer);
            }

            entries.Add(new Entry
            {
                interactable = interactable,
                renderers = optimizedRenderers.ToArray(),
                colliders = interactable.GetComponentsInChildren<Collider>(true)
            });
        }

        nextRefresh = Time.unscaledTime + refreshObjectsInterval;
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    // ACCIÓN: mantener visibles los objetos cercanos y apagar trabajo innecesario a distancia.
    private void UpdateVisibility()
    {
        float renderSqr = renderDistance * renderDistance;
        float collisionSqr = collisionDistance * collisionDistance;
        foreach (Entry entry in entries)
        {
            if (entry.interactable == null)
                continue;

            Vector3 delta = entry.interactable.transform.position - player.position;
            delta.y = 0f;
            float distanceSqr = delta.sqrMagnitude;
            bool isTree = entry.interactable is TreeInteractivo;
            bool visible = entry.interactable.IsVisuallyAvailable && (isTree && keepTreesVisible || distanceSqr <= renderSqr);
            bool collisionEnabled = distanceSqr <= collisionSqr && entry.interactable.IsInteractionAvailable;

            foreach (Renderer renderer in entry.renderers)
            {
                if (renderer == null)
                    continue;
                renderer.forceRenderingOff = false;
                if (renderer.enabled != visible)
                    renderer.enabled = visible;
            }
            foreach (Collider collider in entry.colliders)
            {
                if(collider==null)continue;
                bool enabledForCollision=collisionEnabled;
                if(XunjuuSceneVisualPolicy.IsSoftDecoration(collider.transform))
                {if(collider is MeshCollider mesh && !mesh.convex)enabledForCollision=false;else collider.isTrigger=true;}
                if (collider.enabled != enabledForCollision)collider.enabled = enabledForCollision;
            }
        }
    }
}
