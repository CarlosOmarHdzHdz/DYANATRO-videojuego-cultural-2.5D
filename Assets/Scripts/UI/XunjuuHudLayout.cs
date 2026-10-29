using UnityEngine;
using UnityEngine.UI;

// Mantiene el lienzo de diseno completo en 16:9, 16:10, 4:3 y ultrapanoramico.
[DefaultExecutionOrder(11000)]
public sealed class XunjuuHudLayout : MonoBehaviour
{
    private int width,height;
    private float nextScan;
    private float nextDiscovery;
    private CanvasScaler[] scalers;
    public void Invalidate(){nextDiscovery=nextScan=0;}
    private void LateUpdate()
    {
        if(Screen.width==width&&Screen.height==height&&Time.unscaledTime<nextScan)return;
        width=Screen.width;height=Screen.height;nextScan=Time.unscaledTime+1f;
        float fit=width/(float)Mathf.Max(1,height)<1920f/1080f?0f:1f;
        if(scalers==null||Time.unscaledTime>=nextDiscovery){scalers=FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include,FindObjectsSortMode.None);nextDiscovery=Time.unscaledTime+8f;}
        foreach(var scaler in scalers)
        {
            if(scaler==null)continue;
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=fit;
        }
    }
}
