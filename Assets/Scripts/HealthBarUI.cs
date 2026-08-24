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

    void Start()
    {
        vidaActual = vidaMaxima;
        ActualizarFrame();
        ActualizarTexto();
        Debug.Log("✅ BarraVidaFrames iniciada. Vida: " + vidaActual);
    }

    // ============================================
    // MÉTODO PRINCIPAL - Recibe el daño desde PlayerController
    // ============================================
    public void EstablecerVida(int nuevaVida)
    {
        vidaActual = Mathf.Clamp(nuevaVida, 0f, vidaMaxima);
        ActualizarFrame();
        ActualizarTexto();
        Debug.Log($"🩸 BarraVidaFrames actualizada: {vidaActual}/{vidaMaxima}");
    }

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

        float porcentaje = vidaActual / vidaMaxima;
        int frameIndex = Mathf.FloorToInt(porcentaje * (framesVida.Length - 1));
        frameIndex = Mathf.Clamp(frameIndex, 0, framesVida.Length - 1);

        if (framesVida[frameIndex] != null)
        {
            imagenBarra.sprite = framesVida[frameIndex];
            Debug.Log($"🎨 Frame actualizado: {frameIndex} - {framesVida[frameIndex].name}");
        }
    }

    private void ActualizarTexto()
    {
        if (textoVida != null)
        {
            textoVida.text = $"{vidaActual}/{vidaMaxima}";
        }
    }
}