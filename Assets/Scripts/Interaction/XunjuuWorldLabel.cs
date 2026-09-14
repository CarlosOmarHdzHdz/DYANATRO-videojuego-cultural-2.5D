using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Nombre editable de objeto
// Acción: presentar un nombre sobre cualquier objeto importante del nivel.
// ============================================================================
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class XunjuuWorldLabel : MonoBehaviour
{
    [Header("Xunjuú v0.1 - Nombre")]
    [SerializeField] private string displayName = "Objeto";
    [SerializeField] private bool showLabel = true;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField, Min(0.02f)] private float characterSize = 0.095f;
    [SerializeField, Min(8)] private int fontSize = 32;
    [SerializeField, Min(1f)] private float maxVisibleDistance = 32f;
    [SerializeField] private Color textColor = new Color(1f, 0.94f, 0.68f, 1f);

    private const string LabelObjectName = "Nombre_Objeto_Xunjuu";
    private TextMesh label;
    private Renderer labelRenderer;
    private Transform viewer;

    public string DisplayName => displayName;

    private void OnEnable()
    {
        RefreshLabel();
    }

    private void OnValidate()
    {
        characterSize = Mathf.Max(0.02f, characterSize);
        fontSize = Mathf.Max(8, fontSize);
        maxVisibleDistance = Mathf.Max(1f, maxVisibleDistance);
        if (transform.Find(LabelObjectName) != null)
            RefreshLabel();
    }

    private void LateUpdate()
    {
        if (label == null)
            RefreshLabel();

        if (label == null)
            return;

        Camera camera = Camera.main;
        bool visible = showLabel;
        if (camera != null)
        {
            visible &= Vector3.Distance(camera.transform.position, transform.position) <= maxVisibleDistance;
            label.transform.rotation = camera.transform.rotation;
        }
        if(Application.isPlaying)
        {
            if(viewer==null)viewer=GameObject.FindGameObjectWithTag("Player")?.transform;
            if(viewer!=null)
            {
                Vector3 delta=transform.position-viewer.position;delta.y=0;
                float distance=delta.magnitude;
                visible &= distance<12f;
                Color tint=textColor;tint.a*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(8f,12f,distance));
                label.color=tint;
            }
        }

        if (labelRenderer != null)
            labelRenderer.enabled = visible;
    }

    // ACCIÓN: cambiar el nombre y decidir si debe mostrarse.
    public void Configure(string newName, bool visible = true, Vector3? offset = null)
    {
        displayName = string.IsNullOrWhiteSpace(newName) ? gameObject.name : newName;
        showLabel = visible;
        if (offset.HasValue)
            localOffset = offset.Value;
        RefreshLabel();
    }

    // ACCIÓN: crear o actualizar el texto hijo sin modificar el sprite original.
    [ContextMenu("Xunjuú v0.1/Actualizar nombre visible")]
    public void RefreshLabel()
    {
        Transform child = transform.Find(LabelObjectName);
        if (child == null)
        {
            GameObject labelObject = new GameObject(LabelObjectName);
            labelObject.transform.SetParent(transform, false);
            child = labelObject.transform;
        }

        child.localPosition = localOffset;
        // Xunjuu v0.1 - ACCION: conservar el nombre legible aunque el objeto,
        // como Ocelotl, utilice una escala grande para su sprite y colision.
        Vector3 ownerScale = transform.lossyScale;
        float largestScale = Mathf.Max(0.0001f, Mathf.Abs(ownerScale.x), Mathf.Abs(ownerScale.y), Mathf.Abs(ownerScale.z));
        child.localScale = Vector3.one / largestScale;
        label = child.GetComponent<TextMesh>();
        if (label == null)
            label = child.gameObject.AddComponent<TextMesh>();

        label.text = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = fontSize;
        label.characterSize = characterSize;
        label.color = textColor;
        labelRenderer = label.GetComponent<Renderer>();
        if (labelRenderer != null)
        {
            labelRenderer.sortingOrder = 3000;
            labelRenderer.enabled = showLabel;
        }
    }
}
