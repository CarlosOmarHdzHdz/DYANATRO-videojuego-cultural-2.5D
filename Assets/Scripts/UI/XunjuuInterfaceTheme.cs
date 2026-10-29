using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Sistema visual compartido: superficies sobrias, texto legible y bordado discreto.
public static class XunjuuInterfaceTheme
{
    public static readonly Color Ink = new Color(.055f,.105f,.095f,.97f);
    public static readonly Color Forest = new Color(.12f,.26f,.22f,1f);
    public static readonly Color Paper = new Color(.97f,.94f,.86f,1f);
    public static readonly Color Gold = new Color(.83f,.65f,.33f,1f);
    public static readonly Color Muted = new Color(.70f,.77f,.70f,1f);
    public static Font Font => XunjuuTypography.Regular;

    public static RectTransform Rect(GameObject go, Vector2 size, Vector2 position)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f,.5f);
        rt.sizeDelta = size; rt.anchoredPosition = position;
        return rt;
    }
    public static GameObject Panel(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(XunjuuRoundedImage)); go.transform.SetParent(parent,false);
        Rect(go,size,position); var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        return go;
    }
    public static Text Label(Transform parent,string name,string value,int size,Vector2 dimensions,Vector2 position,Color color,TextAnchor align=TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
        Rect(go,dimensions,position); var text=go.GetComponent<Text>(); text.font=Font; text.fontSize=size;
        text.text=value;text.color=color;text.alignment=align;text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
        return text;
    }
    public static void Frame(Transform parent,Vector2 size)
    {
        var outline=parent.GetComponent<Outline>()??parent.gameObject.AddComponent<Outline>();
        outline.effectColor=new Color(Gold.r,Gold.g,Gold.b,.55f);outline.effectDistance=new Vector2(1,-1);
        Panel(parent,"Acento_Superior",new Vector2(size.x-48,2),new Vector2(0,size.y*.5f-14),Gold);
        // En placas pequeñas el bordado competía con el texto del objetivo.
        for(int i=0;i<(size.y>=180?7:0);i++)
        {
            var stitch=Panel(parent,"Puntada",new Vector2(5,5),new Vector2((i-3)*15,-size.y*.5f+13),i%2==0?Gold:new Color(.62f,.24f,.30f));
            stitch.transform.localRotation=Quaternion.Euler(0,0,45);
        }
    }
    public static Button Button(Transform parent,string name,string value,Vector2 size,Vector2 position,UnityAction action)
    {
        var go=Panel(parent,name,size,position,Forest);go.GetComponent<Image>().raycastTarget=true;
        var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.2f,1.15f,.95f);
        colors.pressedColor=new Color(.75f,.86f,.78f);colors.fadeDuration=.14f;button.colors=colors;
        button.onClick.AddListener(action);go.AddComponent<XunjuuButtonMotion>();Label(go.transform,"Etiqueta",value,19,size-new Vector2(16,8),Vector2.zero,Paper);
        return button;
    }
    public static void PolishButtons(Transform root)
    {
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            var colors=button.colors;colors.fadeDuration=.16f;
            colors.highlightedColor=new Color(1.16f,1.12f,.96f);colors.pressedColor=new Color(.76f,.86f,.78f);
            colors.disabledColor=new Color(.55f,.58f,.55f,.65f);button.colors=colors;
        }
    }
}
