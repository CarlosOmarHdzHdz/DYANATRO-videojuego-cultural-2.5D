using UnityEngine;

// Real frame cycles, driven by displacement; no deformation of the animal silhouette.
[DisallowMultipleComponent]
public sealed class XunjuuAnimalSpriteMotion : MonoBehaviour
{
    private SpriteRenderer visual;
    private Sprite source;
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private GameObject meshObject;
    private MaterialPropertyBlock properties;
    private Vector3 previous;
    private float phase;
    private float movementGraceUntil, displayedSpeed;
    private float walkFramesPerSecond = 6f, runFramesPerSecond = 8f;
    private Animal animal;
    private static Material faunaMaterial;
    private readonly Vector2[] uv = new Vector2[4];
    public float WalkPhase => phase;
    public int CurrentFrame { get; private set; } = -1;
    public bool IsRunning { get; private set; }
    public bool HasFrameAnimation => mesh != null;
    private void OnEnable()
    {
        previous = transform.position;
        displayedSpeed = 0f;
        movementGraceUntil = 0f;
        phase = 0f;
        IsRunning = false;
        CurrentFrame = -1;
    }

    private void LateUpdate()
    {
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>(true);
        if (visual == null || visual.sprite == null) return;
        if (visual.sprite != source || mesh == null) BuildMesh(visual.sprite);
        if (mesh == null) return;
        meshRenderer.sortingLayerID = visual.sortingLayerID;
        meshRenderer.sortingOrder = visual.sortingOrder;
        meshRenderer.enabled = visual.enabled;
        meshObject.transform.localScale = new Vector3(visual.flipX ? -1 : 1, visual.flipY ? -1 : 1, 1);
        properties.SetColor("_Color", visual.color);
        meshRenderer.SetPropertyBlock(properties);
        Vector3 delta = transform.position - previous;
        previous = transform.position;
        delta.y = 0;
        if (Time.deltaTime <= 0) return;
        float distance = delta.magnitude;
        if (distance > 2f) distance = 0;
        displayedSpeed = Mathf.MoveTowards(displayedSpeed, distance / Time.deltaTime, Time.deltaTime * 8f);
        if (distance > .002f && displayedSpeed > .12f)
            movementGraceUntil = Time.time + .18f;
        bool moving = Time.time < movementGraceUntil;
        // Hysteresis prevents rapid switches between walk and flee poses at low speed.
        bool fleeing = animal != null && animal.State == Animal.BehaviourState.Flee;
        bool running = moving && fleeing && displayedSpeed > (IsRunning ? .5f : 1f);
        if (running != IsRunning)
        {
            IsRunning = running;
            // Both sequences begin with a planted stance; do not enter an airborne frame mid-step.
            phase = 0f;
        }
        float stride = Mathf.Max(.45f, transform.lossyScale.x * (IsRunning ? 2.8f : 1.7f));
        float posesPerSecond = IsRunning ? runFramesPerSecond : walkFramesPerSecond;
        float cycles = Mathf.Min(distance / stride, Time.deltaTime * posesPerSecond / 8f);
        phase = Mathf.Repeat(phase + cycles * Mathf.PI * 2f, Mathf.PI * 2f);
        Pose(phase, moving ? 1f : 0f);
    }

    private void BuildMesh(Sprite sprite)
    {
        Release();
        source = sprite;
        var capture = GetComponent<XunjuuAnimalCapture>();
        var entry = XunjuuFaunaCatalog.Find(capture != null ? capture.FaunaId : sprite.name);
        if (entry == null) return;
        var atlas = Resources.Load<Texture2D>("Sprites/Animals/CyclesV2/" + entry.Id);
        if (atlas == null) return; // Keep original visible if resources are unavailable.
        // The low-bodied tlacuache and short-stride woodland animals read better with
        // a slightly slower cycle; only presentation changes, never their movement.
        walkFramesPerSecond = entry.Id == "tlacuache" ? 4.5f : 6f;
        runFramesPerSecond = entry.Id == "tlacuache" ? 5.5f :
            entry.Id == "conejo_serrano" || entry.Id == "ardilla_gris" ? 7f : 6.5f;
        animal = GetComponent<Animal>();
        Shader shader = Resources.Load<Shader>("XunjuuAnimatedFauna");
        if (shader == null) shader = Shader.Find("Xunjuu/AnimatedFauna");
        if (shader == null) return; // Do not hide the source sprite in an incomplete build.
        meshObject = new GameObject("Fauna_Articulada", typeof(MeshFilter), typeof(MeshRenderer));
        meshObject.transform.SetParent(visual.transform, false);
        meshObject.layer = visual.gameObject.layer;
        meshRenderer = meshObject.GetComponent<MeshRenderer>();
        if (faunaMaterial == null)
            faunaMaterial = new Material(shader) { name = "Fauna_Ciclos_Pixel" };
        meshRenderer.sharedMaterial = faunaMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        properties = new MaterialPropertyBlock();
        properties.SetTexture("_MainTex", atlas);
        float side = Mathf.Max(entry.ColliderSize.x, entry.ColliderSize.y) * 128f / 104f;
        float bottom = -side * 12f / 128f;
        mesh = new Mesh { name = entry.Id + "_FrameQuad" };
        mesh.vertices = new[] { new Vector3(-side/2,bottom,0), new Vector3(side/2,bottom,0),
            new Vector3(-side/2,bottom+side,0), new Vector3(side/2,bottom+side,0) };
        mesh.triangles = new[] {0,2,1,1,2,3};
        mesh.colors = new[] {Color.white,Color.white,Color.white,Color.white};
        mesh.RecalculateBounds();
        meshObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        visual.forceRenderingOff = true;
        CurrentFrame = -1;
        Pose(0,0);
    }

    private void Pose(float cycle, float moving)
    {
        int frame = moving > 0 ? Mathf.FloorToInt(Mathf.Repeat(cycle / (Mathf.PI*2),1)*8) + (IsRunning ? 8 : 0) : 0;
        if (frame == CurrentFrame || mesh == null) return;
        CurrentFrame = frame;
        float x = (frame % 4) * .25f, y = (3-frame/4)*.25f;
        const float inset = .5f / 512;
        uv[0] = new Vector2(x+inset,y+inset); uv[1] = new Vector2(x+.25f-inset,y+inset);
        uv[2] = new Vector2(x+inset,y+.25f-inset); uv[3] = new Vector2(x+.25f-inset,y+.25f-inset);
        mesh.uv = uv;
    }

    private void Release()
    {
        if (visual != null) visual.forceRenderingOff = false;
        if (mesh != null) Destroy(mesh);
        if (meshObject != null) { meshObject.SetActive(false); Destroy(meshObject); }
        mesh = null;
    }
    private void OnDisable() { Release(); }
    private void OnDestroy() { Release(); }
}
