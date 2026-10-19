using UnityEngine;
using UnityEngine.UI;

public class BarraVidaFrames : MonoBehaviour
{
    [Header("Sprites de 10 frames")]
    public Sprite[] framesVida;

    [Header("Referencias")]
    public Image imagenBarra;
    public Text textoVida;

    [Header("Configuración")]
    public float vidaMaxima = 100f;
    private float vidaActual;
    private float vidaMostrada;
    private float velocidadVisual;
    [SerializeField, Min(0.01f)] private float suavizado = 0.16f;
    public bool usarDisenoMazahua = true;
    private bool vidaInicializada;
    private XunjuuHealthBarVisual visual;

    void Start()
    {
        GameStateManager.RegisterHud(gameObject);
        if (!vidaInicializada)
        {
            EstablecerVida(Mathf.RoundToInt(Mathf.Max(1f, vidaMaxima)));
            return;
        }
        ActualizarFrame();
        ActualizarTexto();
    }

    // ============================================
    // MÉTODO PRINCIPAL - Recibe el daño desde PlayerController
    // ============================================
    public void EstablecerVida(int nuevaVida)
    {
        vidaMaxima = Mathf.Max(1f, vidaMaxima);
        vidaActual = Mathf.Clamp(nuevaVida, 0f, vidaMaxima);
        if (!vidaInicializada)
        {
            vidaMostrada = vidaActual;
            velocidadVisual = 0f;
            vidaInicializada = true;
        }
        // Al cambiar de sentido, responder sin seguir bajando después de curar.
        if ((vidaActual - vidaMostrada) * velocidadVisual < 0f) velocidadVisual = 0f;
        ActualizarFrame();
        ActualizarTexto();
    }

    private void LateUpdate() => ActualizarTransicion(Time.deltaTime);

    // Solo anima el HUD: no retrasa el daño, la curación ni la derrota reales.
    public void ActualizarTransicion(float deltaTime)
    {
        if (!vidaInicializada || deltaTime <= 0f) return;
        vidaMostrada = Mathf.SmoothDamp(vidaMostrada, vidaActual, ref velocidadVisual,
            Mathf.Max(0.01f, suavizado), Mathf.Infinity, deltaTime);
        vidaMostrada = Mathf.Clamp(vidaMostrada, 0f, vidaMaxima);
        if (Mathf.Abs(vidaMostrada - vidaActual) < 0.005f)
        {
            vidaMostrada = vidaActual;
            velocidadVisual = 0f;
        }
        ActualizarFrame();
    }

    public float GetVidaMostrada() => vidaMostrada;

    public void EstablecerMaxima(int maxima)
    {
        vidaMaxima = Mathf.Max(1, maxima);
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        vidaMostrada = Mathf.Clamp(vidaMostrada, 0, vidaMaxima);
        if (vidaInicializada)
        {
            ActualizarFrame();
            ActualizarTexto();
        }
    }

    void OnDestroy() => visual?.Dispose();

    public float GetVidaActual()
    {
        return vidaActual;
    }

    public float GetVidaMaxima()
    {
        return vidaMaxima;
    }

    private void ActualizarFrame()
    {
        if (usarDisenoMazahua && visual == null && imagenBarra != null)
            visual = XunjuuHealthBarVisual.Create(this);
        if (visual != null)
        {
            visual.SetHealth(vidaMostrada / Mathf.Max(1f, vidaMaxima));
            return;
        }
        if (framesVida == null || framesVida.Length == 0)
        {
            Debug.LogError("❌ framesVida no tiene sprites asignados");
            return;
        }

        if (imagenBarra == null)
        {
            Debug.LogError("❌ imagenBarra no asignada");
            return;
        }

        float porcentaje = vidaMostrada / Mathf.Max(1f, vidaMaxima);
        int frameIndex = Mathf.FloorToInt(porcentaje * (framesVida.Length - 1));
        frameIndex = Mathf.Clamp(frameIndex, 0, framesVida.Length - 1);

        if (framesVida[frameIndex] != null)
        {
            imagenBarra.sprite = framesVida[frameIndex];
        }
    }

    private void ActualizarTexto()
    {
        if (textoVida != null)
        {
            textoVida.text = $"VIDA  {vidaActual:0} / {vidaMaxima:0}";
        }
    }
}
