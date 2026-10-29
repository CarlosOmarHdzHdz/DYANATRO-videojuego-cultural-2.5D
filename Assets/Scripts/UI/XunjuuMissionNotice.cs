using UnityEngine;
using UnityEngine.UI;

// Un solo aviso de mision para flores, fauna, combate y aprendizaje de plantas.
[DisallowMultipleComponent]
public sealed class XunjuuMissionNotice : MonoBehaviour
{
    private GameObject canvasRoot, card;
    private CanvasGroup group;
    private Text heading, mazahua, spanish;
    private float remaining, elapsed, duration;
    private string lastKey;
    private float lastAt = -10f;
    private bool completion;
    public bool HasNotice => remaining > 0f;
    public string MazahuaText => mazahua != null ? mazahua.text : "";
    public string SpanishText => spanish != null ? spanish.text : "";
    public static XunjuuMissionNotice For(Component owner)
    {
        var director=owner as DyanatroGameDirector ?? Object.FindFirstObjectByType<DyanatroGameDirector>();
        var host=director!=null?director.gameObject:owner.gameObject;
        return host.GetComponent<XunjuuMissionNotice>()??host.AddComponent<XunjuuMissionNotice>();
    }

    private void Build()
    {
        if(canvasRoot!=null)return;
        canvasRoot=new GameObject("HUD_Aviso_Unico",typeof(Canvas),typeof(CanvasScaler));canvasRoot.transform.SetParent(transform,false);
        var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2800;
        var scaler=canvasRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        card=XunjuuInterfaceTheme.Panel(canvasRoot.transform,"Aviso_Mision_Centrado",new Vector2(760,230),Vector2.zero,XunjuuInterfaceTheme.Ink);
        group=card.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        XunjuuInterfaceTheme.Frame(card.transform,new Vector2(760,230));
        heading=XunjuuInterfaceTheme.Label(card.transform,"Estado","",14,new Vector2(660,24),new Vector2(0,76),XunjuuInterfaceTheme.Muted);
        mazahua=XunjuuInterfaceTheme.Label(card.transform,"Mazahua_Arriba","",33,new Vector2(680,58),new Vector2(0,24),XunjuuInterfaceTheme.Gold);
        mazahua.fontStyle=FontStyle.Bold;
        spanish=XunjuuInterfaceTheme.Label(card.transform,"Espanol_Abajo","",24,new Vector2(680,82),new Vector2(0,-45),XunjuuInterfaceTheme.Paper);
        card.SetActive(false);
    }
    public void Show(string key,string mazahuaWords,string spanishMessage,bool completed=true,float seconds=5f)
    {
        if(string.IsNullOrWhiteSpace(mazahuaWords)||string.IsNullOrWhiteSpace(spanishMessage))return;
        if(key==lastKey&&Time.unscaledTime-lastAt<3f)return;
        if(HasNotice&&completion&&!completed)return;
        Build();lastKey=key;lastAt=Time.unscaledTime;completion=completed;
        heading.text=completed?"MISIÓN COMPLETADA  /  MEMORIA Y APRENDIZAJE":"PALABRA RECUPERADA";
        mazahua.text=mazahuaWords;spanish.text=spanishMessage;
        duration=Mathf.Max(2f,seconds);remaining=duration;elapsed=0;card.SetActive(true);group.alpha=0;
    }
    private void Update()
    {
        if(!HasNotice||group==null)return;
        if(Time.timeScale<=0f){group.alpha=0;return;}
        remaining-=Time.deltaTime;elapsed+=Time.deltaTime;
        float fade=Mathf.Min(Mathf.Clamp01(elapsed/.22f),Mathf.Clamp01(remaining/.32f));
        group.alpha=fade;card.transform.localScale=Vector3.one*Mathf.Lerp(.975f,1f,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.25f)));
        if(remaining<=0f)card.SetActive(false);
    }
    public void Clear(){remaining=0;if(card!=null)card.SetActive(false);}
    private void OnDestroy(){if(canvasRoot!=null)Destroy(canvasRoot);}
}
