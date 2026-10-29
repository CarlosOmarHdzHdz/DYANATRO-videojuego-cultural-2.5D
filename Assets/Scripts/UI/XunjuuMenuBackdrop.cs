using UnityEngine;
using UnityEngine.UI;

// Arte decorativo: no instancia vegetación, animales ni elementos del mapa.
[DisallowMultipleComponent]
public sealed class XunjuuMenuBackdrop : MonoBehaviour
{
    private static Texture2D landscape;
    private RawImage image;
    private RectTransform art;
    private bool paused;
    public static XunjuuMenuBackdrop Attach(Transform parent,bool pause)
    {
        var root=new GameObject(pause?"Fondo_Paisaje_Pausa":"Fondo_Paisaje_Inicio",typeof(RectTransform));root.transform.SetParent(parent,false);root.transform.SetAsFirstSibling();
        Stretch(root.GetComponent<RectTransform>(),0);
        var backdrop=root.AddComponent<XunjuuMenuBackdrop>();backdrop.paused=pause;backdrop.Build();return backdrop;
    }
    private static void Stretch(RectTransform rect,float padding)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.one*-padding;rect.offsetMax=Vector2.one*padding;}
    private void Build()
    {
        if(landscape==null)landscape=Resources.Load<Texture2D>("Sprites/UI/MenuHighlands_20261019");
        var painting=new GameObject("Paisaje_Ilustrado",typeof(RectTransform),typeof(RawImage));painting.transform.SetParent(transform,false);art=painting.GetComponent<RectTransform>();Stretch(art,24);
        image=painting.GetComponent<RawImage>();image.texture=landscape;image.raycastTarget=false;
        image.color=paused?new Color(.61f,.73f,.70f,1):Color.white;
        if(landscape==null){Debug.LogError("Falta el fondo MenuHighlands_20261019");image.color=new Color(.05f,.12f,.10f);}
        var veil=XunjuuInterfaceTheme.Panel(transform,"Velo_De_Lectura",Vector2.zero,Vector2.zero,new Color(.01f,.035f,.025f,paused ? .22f : .08f));Stretch(veil.GetComponent<RectTransform>(),0);
        // Puntadas de esquina, limitadas al margen exterior y sin capturar clics.
        for(int corner=0;corner<4;corner++)
        {
            Vector2 anchor=new Vector2(corner%2,corner/2);float dx=anchor.x==0?1:-1,dy=anchor.y==0?1:-1;
            for(int stitch=0;stitch<4;stitch++)
            {
                var dot=XunjuuInterfaceTheme.Panel(transform,"Puntada_Exterior",new Vector2(3,3),Vector2.zero,new Color(.84f,.68f,.39f,.65f));
                var rect=dot.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=new Vector2(dx*(35+stitch*12),dy*66);rect.localRotation=Quaternion.Euler(0,0,45);
            }
        }
    }
    private void LateUpdate()
    {
        if(art==null||image==null||landscape==null)return;
        float ratio=art.rect.width/Mathf.Max(1,art.rect.height),source=landscape.width/(float)landscape.height;
        float w=ratio<source?ratio/source:1,h=ratio>source?source/ratio:1;
        image.uvRect=new Rect((1-w)*.5f,(1-h)*.5f,w,h);
        // Deriva lenta de pocos píxeles. El panel de controles permanece inmóvil.
        float t=Time.unscaledTime;art.anchoredPosition=XunjuuPresentationPreferences.ReducedMotion?Vector2.zero:new Vector2(Mathf.Sin(t*.065f)*5,Mathf.Sin(t*.049f)*3);
    }
}
