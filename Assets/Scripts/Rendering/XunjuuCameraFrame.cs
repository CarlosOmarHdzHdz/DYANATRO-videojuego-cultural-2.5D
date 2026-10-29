using UnityEngine;

// Cientos de sprites comparten una sola consulta y los mismos ejes por frame.
// Se renueva incluso al cambiar de camara o escena y no guarda estado de partida.
public static class XunjuuCameraFrame
{
    private static int frame = -1;
    private static Camera camera;
    private static Vector3 forward = Vector3.forward;
    private static Vector3 groundForward = Vector3.forward;
    private static Quaternion upright = Quaternion.identity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { frame = -1; camera = null; }
    public static void Invalidate(){frame=-1;}

    private static void Refresh()
    {
        if (frame == Time.frameCount) return;
        frame = Time.frameCount;
        camera = Camera.main;
        forward = camera != null ? camera.transform.forward : Vector3.forward;
        groundForward = new Vector3(forward.x, 0f, forward.z).normalized;
        if (groundForward.sqrMagnitude < .001f) groundForward = Vector3.forward;
        upright = Quaternion.Euler(0f, camera != null ? camera.transform.eulerAngles.y : 0f, 0f);
    }

    public static bool HasCamera { get { Refresh(); return camera != null; } }
    public static Camera Current { get { Refresh(); return camera; } }
    public static Vector3 Forward { get { Refresh(); return forward; } }
    public static Vector3 GroundForward { get { Refresh(); return groundForward; } }
    public static Quaternion Upright { get { Refresh(); return upright; } }
}
