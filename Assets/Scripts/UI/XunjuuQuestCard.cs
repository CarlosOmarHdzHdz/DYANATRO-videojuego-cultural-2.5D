using UnityEngine;
using UnityEngine.UI;

// Áreas explícitas: ninguna decoración se coloca detrás de la instrucción.
[DisallowMultipleComponent]
public sealed class XunjuuQuestCard : MonoBehaviour
{
    public Text Body {get;private set;}
    private Text heading,counter;
    private RectTransform progress;
    private CanvasGroup group;
    private XunjuuPanelReveal reveal;
    private float target,shown;
    private bool visible;
    private int lastCount=-1,lastGoal=-1;
    public static XunjuuQuestCard Create(Transform parent,string name)
    {
        var root=XunjuuInterfaceTheme.Panel(parent,name,new Vector2(660,120),new Vector2(0,-84),XunjuuInterfaceTheme.Ink);
        var rt=root.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);
        var outline=root.AddComponent<Outline>();outline.effectColor=new Color(.48f,.59f,.48f,.75f);outline.effectDistance=new Vector2(1,-1);
        var card=root.AddComponent<XunjuuQuestCard>();card.Build();return card;
    }
    private void Build()
    {
        group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        heading=XunjuuInterfaceTheme.Label(transform,"Capitulo_Objetivo","",15,new Vector2(470,22),new Vector2(-20,37),XunjuuInterfaceTheme.Gold,TextAnchor.MiddleLeft);
        heading.fontStyle=FontStyle.Bold;
        counter=XunjuuInterfaceTheme.Label(transform,"Contador_Objetivo","",20,new Vector2(80,28),new Vector2(270,37),XunjuuInterfaceTheme.Paper,TextAnchor.MiddleRight);
        Body=XunjuuInterfaceTheme.Label(transform,"Texto_Objetivo","",21,new Vector2(584,54),new Vector2(0,-8),XunjuuInterfaceTheme.Paper,TextAnchor.MiddleLeft);
        var track=XunjuuInterfaceTheme.Panel(transform,"Pista_Progreso",new Vector2(594,3),new Vector2(0,-48),new Color(.24f,.34f,.28f));
        var fill=XunjuuInterfaceTheme.Panel(track.transform,"Avance_Objetivo",new Vector2(594,3),new Vector2(-297,0),XunjuuInterfaceTheme.Gold);
        progress=fill.GetComponent<RectTransform>();progress.pivot=new Vector2(0,.5f);progress.localScale=new Vector3(0,1,1);
        // Un único rombo, en el margen y fuera de las cajas de texto.
        var stitch=XunjuuInterfaceTheme.Panel(transform,"Identificador_Textil",new Vector2(8,8),new Vector2(-285,37),XunjuuInterfaceTheme.Gold);
        stitch.transform.localRotation=Quaternion.Euler(0,0,45);
        reveal=gameObject.AddComponent<XunjuuPanelReveal>();
    }
    public void Set(string chapter,string instruction,int count,int goal)
    {
        bool changed=heading.text!=chapter;heading.text=chapter;Body.text=instruction;SetProgress(count,goal);
        if(changed)reveal.Replay();
    }
    public void SetProgress(int count,int goal)
    {
        if(count==lastCount&&goal==lastGoal)return;
        lastCount=count;lastGoal=goal;counter.text=goal>0?Mathf.Clamp(count,0,goal)+" / "+goal:"";
        target=goal>0?Mathf.Clamp01(count/(float)goal):0;
    }
    private void LateUpdate()
    {
        if(Body==null)return;
        bool next=Body.gameObject.activeSelf;
        if(!next){group.alpha=0;visible=false;return;}
        if(!visible){visible=true;reveal.Replay();}
        shown=Mathf.Lerp(shown,target,1-Mathf.Exp(-Time.unscaledDeltaTime*12));
        if(Mathf.Abs(shown-target)<.001f)shown=target;
        progress.localScale=new Vector3(shown,1,1);
    }
}
