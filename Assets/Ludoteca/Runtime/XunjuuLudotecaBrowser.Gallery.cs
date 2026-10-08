using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La galería comparte el navegador, sus recursos y el tiempo sin escala del juego pausado.
public sealed partial class XunjuuLudotecaBrowser
{
    private sealed class GalleryEntry
    {
        public readonly string Kind, Title, Description, Credit;
        public readonly Sprite Art;
        public readonly Color Accent;

        public GalleryEntry(string kind, string title, string description, string credit, Sprite art, Color accent)
        {
            Kind=kind; Title=title; Description=description; Credit=credit; Art=art; Accent=accent;
        }
    }

    private sealed class GalleryView
    {
        public RectTransform Rect;
        public Button Button;
        public GameObject Front, Back;
        public Image Art, Accent;
        public TMP_Text Kind, Title, BackTitle, Description, Credit;
        public Coroutine Flip;
        public bool ShowingBack;
    }

    private GalleryEntry[] galleryEntries;
    private readonly GalleryView[] galleryViews=new GalleryView[2];
    private GameObject galleryToolbar;
    private TMP_Text galleryPageLabel;
    private Button galleryPrevious, galleryNext;
    private int galleryPage;
    private bool galleryFullMode;

    private GameObject BuildInteractiveGallery(Transform parent, Sprite clothing, Sprite dance, Sprite milpa)
    {
        galleryEntries=new[]
        {
            new GalleryEntry("FOTOGRAFÍA · VESTIMENTA", "Quexquémetl mazahua",
                "Observa la forma, los bordados y los colores de esta prenda. Sus diseños pueden variar entre comunidades y ocasiones.",
                "Foto: Amantlali · Wikimedia Commons, CC BY-SA 4.0", clothing, TextileGrana),
            new GalleryEntry("FOTOGRAFÍA · TRADICIÓN", "Danza de las Pastoras",
                "Esta fotografía documenta una danza en Ixtlahuaca. Mira el atuendo y los elementos de la celebración; consulta su contexto local.",
                "Foto: Fernando Oscar Martín · Wikimedia Commons, CC BY-SA 4.0", dance, TextileBlue),
            new GalleryEntry("FOTOGRAFÍA · TERRITORIO", "Campos de maíz",
                "El maíz forma parte del paisaje agrícola. Esta imagen es una referencia visual de cultivo, no una fotografía de todas las comunidades mazahuas.",
                "Foto: Tlaxcala de Xicohténcatl · Wikimedia Commons, CC BY-SA 2.0", milpa, Gold),
            new GalleryEntry("ILUSTRACIÓN · FAUNA", "Venado",
                "En el juego puedes observar y registrar fauna sin dañarla. Al encontrar un animal, consulta su ficha para ampliar lo aprendido.",
                "Ilustración conceptual del proyecto", NavigationIcon(Page.Fauna), ForestGreen),
            new GalleryEntry("ILUSTRACIÓN · MILPA", "Mazorca de maíz",
                "Distingue la mazorca, los granos y las hojas. Después busca la palabra MILPA en la actividad de vocabulario.",
                "Ilustración conceptual del proyecto", ElementIcon(0), Gold),
            new GalleryEntry("ILUSTRACIÓN · OFICIO", "Tejido",
                "Observa la relación entre hilos, herramientas y patrones. La imagen invita a investigar técnicas textiles con fuentes de la comunidad.",
                "Ilustración conceptual; técnica no documentada", NavigationIcon(Page.Culture), TextileGrana),
            new GalleryEntry("ILUSTRACIÓN · OBJETO", "Vasija",
                "La imagen muestra una vasija con formas geométricas. No representa una pieza histórica identificada: úsala para comparar motivos y materiales.",
                "Ilustración conceptual; no es pieza de museo", ElementIcon(1), TextileBlue),
            new GalleryEntry("ILUSTRACIÓN · NATURALEZA", "Lluvia",
                "Observa cómo el agua y las estaciones se relacionan con el cultivo. Busca información local para conocer mejor cada territorio.",
                "Ilustración conceptual del proyecto", ElementIcon(5), TextileBlue),
            new GalleryEntry("ILUSTRACIÓN · FAUNA", "Guajolote",
                "Compara su silueta y sus colores con otras aves. Esta ficha es ilustrativa y no sustituye una guía de identificación de especies.",
                "Ilustración conceptual del proyecto", ElementIcon(2), ForestGreen)
        };

        var root=new GameObject("Galeria_Multimedia",typeof(RectTransform));
        root.transform.SetParent(parent,false);
        SetRect(root.GetComponent<RectTransform>(),new Vector2(.04f,.08f),new Vector2(.96f,.78f));

        galleryToolbar=Panel(root.transform,"Controles_Galeria",new Color(.97f,.94f,.88f,1f));
        SetRect(galleryToolbar.GetComponent<RectTransform>(),new Vector2(.035f,.84f),new Vector2(.965f,.985f));
        AddOutline(galleryToolbar,ParchmentShade,1f);
        TextBlock(galleryToolbar.transform,"EXPLORA LAS FICHAS · TOCA PARA VOLTEAR",17,
            new Vector2(.40f,.5f),new Vector2(640f,40f),TextAnchor.MiddleCenter,GoldDark).fontStyle=FontStyles.Bold;
        galleryPrevious=Button(galleryToolbar.transform,"ANTERIOR",new Vector2(.08f,.5f),new Vector2(160f,48f),()=>ChangeGalleryPage(-1),ForestGreen);
        galleryNext=Button(galleryToolbar.transform,"SIGUIENTE",new Vector2(.91f,.5f),new Vector2(160f,48f),()=>ChangeGalleryPage(1),ForestGreen);
        galleryPageLabel=TextBlock(galleryToolbar.transform,"01 / 05",20,new Vector2(.76f,.5f),new Vector2(110f,40f),TextAnchor.MiddleCenter,TextileBlue);
        galleryPageLabel.fontStyle=FontStyles.Bold;

        for(int i=0;i<galleryViews.Length;i++)galleryViews[i]=CreateGalleryView(root.transform,i);
        RefreshGalleryPage();
        return root;
    }

    private GalleryView CreateGalleryView(Transform parent,int slot)
    {
        int selected=slot;
        var card=new GameObject("Boton_Ficha_"+slot,typeof(RectTransform),typeof(Image),typeof(Button));
        card.transform.SetParent(parent,false);
        var rect=card.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(.27f+slot*.46f,.46f);
        rect.sizeDelta=new Vector2(560f,390f);
        rect.anchoredPosition=Vector2.zero;
        var background=card.GetComponent<Image>();background.color=Parchment;
        AddOutline(card,GoldDark,2f);
        var shadow=card.AddComponent<Shadow>();
        shadow.effectColor=new Color(.20f,.12f,.07f,.22f);
        shadow.effectDistance=new Vector2(0,-6f);
        var button=card.GetComponent<Button>();button.transition=Selectable.Transition.None;
        button.onClick.AddListener(()=>FlipGalleryCard(selected));

        var view=new GalleryView{Rect=rect,Button=button};
        view.Front=Panel(card.transform,"Anverso",Parchment);
        view.Front.GetComponent<Image>().raycastTarget=false;
        view.Accent=Panel(view.Front.transform,"Banda_Tipo",Gold).GetComponent<Image>();
        SetRect(view.Accent.rectTransform,new Vector2(0,.965f),Vector2.one);
        view.Accent.raycastTarget=false;
        view.Kind=TextBlock(view.Front.transform,"",18,new Vector2(.5f,.93f),new Vector2(500,32),TextAnchor.MiddleCenter,TextileBlue);
        view.Kind.fontStyle=FontStyles.Bold;
        var artPlate=Panel(view.Front.transform,"Marco_Imagen",new Color(.99f,.975f,.94f,1f));
        SetRect(artPlate.GetComponent<RectTransform>(),new Vector2(.065f,.23f),new Vector2(.935f,.88f));
        artPlate.GetComponent<Image>().raycastTarget=false;
        AddOutline(artPlate,ParchmentShade,1f);
        var art=new GameObject("Imagen",typeof(RectTransform),typeof(Image));
        art.transform.SetParent(artPlate.transform,false);
        SetRect(art.GetComponent<RectTransform>(),new Vector2(.025f,.025f),new Vector2(.975f,.975f));
        view.Art=art.GetComponent<Image>();view.Art.preserveAspect=true;view.Art.raycastTarget=false;
        view.Title=TextBlock(view.Front.transform,"",27,new Vector2(.5f,.145f),new Vector2(500,57),TextAnchor.MiddleCenter,GoldDark);
        view.Title.fontStyle=FontStyles.Bold;
        TextBlock(view.Front.transform,"TOCA PARA LEER",16,new Vector2(.5f,.035f),new Vector2(470,28),TextAnchor.MiddleCenter,ForestGreen);

        view.Back=Panel(card.transform,"Reverso",new Color(.99f,.97f,.91f,1f));
        view.Back.GetComponent<Image>().raycastTarget=false;
        var backStripe=Panel(view.Back.transform,"Banda_Reverso",ForestGreen).GetComponent<Image>();
        SetRect(backStripe.rectTransform,new Vector2(0,.965f),Vector2.one);backStripe.raycastTarget=false;
        TextBlock(view.Back.transform,"ARCHIVO VISUAL  ·  XUNJÚU",17,new Vector2(.5f,.91f),new Vector2(500,32),TextAnchor.MiddleCenter,ForestGreen).fontStyle=FontStyles.Bold;
        view.BackTitle=TextBlock(view.Back.transform,"",29,new Vector2(.5f,.79f),new Vector2(500,58),TextAnchor.MiddleCenter,GoldDark);
        view.BackTitle.fontStyle=FontStyles.Bold;
        CreateRule(view.Back.transform,.69f,ParchmentShade);
        view.Description=TextBlock(view.Back.transform,"",23,new Vector2(.5f,.46f),new Vector2(480,190),TextAnchor.MiddleCenter,ForestDeep);
        view.Credit=TextBlock(view.Back.transform,"",16,new Vector2(.5f,.16f),new Vector2(480,62),TextAnchor.MiddleCenter,GoldDark);
        TextBlock(view.Back.transform,"TOCA PARA VER LA IMAGEN",16,new Vector2(.5f,.035f),new Vector2(470,28),TextAnchor.MiddleCenter,ForestGreen);
        view.Back.SetActive(false);
        return view;
    }

    private void SetGalleryMode(bool fullGallery)
    {
        galleryPage=0;
        galleryFullMode=fullGallery;
        galleryToolbar.SetActive(fullGallery);
        RefreshGalleryPage();
    }

    private void ChangeGalleryPage(int direction)
    {
        galleryPage=Mathf.Clamp(galleryPage+direction,0,(galleryEntries.Length-1)/galleryViews.Length);
        RefreshGalleryPage();
    }

    private void RefreshGalleryPage()
    {
        int total=(galleryEntries.Length+galleryViews.Length-1)/galleryViews.Length;
        galleryPageLabel.text=(galleryPage+1).ToString("00")+" / "+total.ToString("00");
        galleryPrevious.interactable=galleryPage>0;
        galleryNext.interactable=galleryPage<total-1;
        for(int i=0;i<galleryViews.Length;i++)
        {
            GalleryView view=galleryViews[i];
            if(view.Flip!=null){StopCoroutine(view.Flip);view.Flip=null;}
            view.Rect.localScale=Vector3.one;
            int entryIndex=galleryPage*galleryViews.Length+i;
            float x=entryIndex==galleryEntries.Length-1 && i==0 && galleryPage==total-1 ? .5f : .27f+i*.46f;
            view.Rect.anchorMin=view.Rect.anchorMax=new Vector2(x,galleryFullMode ? .43f : .46f);
            view.Rect.sizeDelta=new Vector2(560f,galleryFullMode?460f:390f);
            view.ShowingBack=false;
            view.Front.SetActive(true);view.Back.SetActive(false);
            view.Button.gameObject.SetActive(entryIndex<galleryEntries.Length);
            if(entryIndex>=galleryEntries.Length)continue;
            GalleryEntry entry=galleryEntries[entryIndex];
            view.Accent.color=entry.Accent;
            view.Kind.text=entry.Kind;
            view.Title.text=entry.Title;
            view.Art.sprite=entry.Art;
            view.Art.color=entry.Art!=null?Color.white:ParchmentShade;
            view.BackTitle.text=entry.Title;
            view.Description.text=entry.Description;
            view.Credit.text=entry.Credit;
            view.Button.interactable=true;
        }
    }

    private void FlipGalleryCard(int slot)
    {
        GalleryView view=galleryViews[slot];
        if(view.Flip!=null || !view.Button.gameObject.activeInHierarchy)return;
        view.Flip=StartCoroutine(AnimateGalleryFlip(view,!view.ShowingBack));
    }

    private IEnumerator AnimateGalleryFlip(GalleryView view,bool revealBack)
    {
        view.Button.interactable=false;
        yield return AnimateGalleryWidth(view.Rect,1f,0f,.17f);
        view.Front.SetActive(!revealBack);
        view.Back.SetActive(revealBack);
        view.ShowingBack=revealBack;
        yield return AnimateGalleryWidth(view.Rect,0f,1f,.17f);
        view.Rect.localScale=Vector3.one;
        view.Button.interactable=true;
        view.Flip=null;
    }

    private static IEnumerator AnimateGalleryWidth(RectTransform rect,float from,float to,float duration)
    {
        float elapsed=0f;
        while(elapsed<duration)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/duration));
            rect.localScale=new Vector3(Mathf.Lerp(from,to,t),1f,1f);
            yield return null;
        }
        rect.localScale=new Vector3(to,1f,1f);
    }
}
