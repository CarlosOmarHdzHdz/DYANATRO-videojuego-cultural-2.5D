using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation only: the inventory remains the authoritative source for stacks and selection.
[DisallowMultipleComponent]
public sealed class XunjuuInventoryUI : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [SerializeField] private bool startVisible;
    private static readonly Color Wood = new Color(.30f,.15f,.12f,1);
    private static readonly Color Rim = new Color(.66f,.39f,.23f,1);
    private static readonly Color Inset = new Color(.40f,.20f,.22f,1);
    private static readonly Color Cream = new Color(1f,.91f,.72f,1);
    private static readonly Color Gold = new Color(.96f,.69f,.27f,1);
    private XunjuuInventory inventory;
    private DyanatroGameDirector gameDirector;
    private GameObject panel;
    private RectTransform itemContainer;
    private Font interfaceFont;
    private Text selectedItemText, capacityText;
    private Image selectedIcon;
    private ScrollRect scroll;
    private const int Columns = 8;
    public bool IsVisible => panel != null && panel.activeInHierarchy
        && (gameDirector == null || gameDirector.IsGameplayHudVisible);

    private void Start()
    {
        gameDirector = FindFirstObjectByType<DyanatroGameDirector>();
        inventory = GetComponent<XunjuuInventory>() ?? gameObject.AddComponent<XunjuuInventory>();
        EnsureEventSystem();
        BuildInterface();
        inventory.Changed += Refresh;
        SetVisible(startVisible);
        Refresh();
    }
    private void Update()
    {
        if (gameDirector != null && !gameDirector.IsGameplayHudVisible) return;
        if (Input.GetKeyDown(toggleKey)) Toggle();
        if (!IsVisible) return;
        if (Input.GetKeyDown(KeyCode.RightArrow)) inventory.SelectNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) inventory.SelectPrevious();
        if (Input.GetKeyDown(KeyCode.DownArrow) && inventory.Items.Count > 0)
            inventory.SelectIndex(Mathf.Min(inventory.Items.Count-1, inventory.SelectedIndex+Columns));
        if (Input.GetKeyDown(KeyCode.UpArrow) && inventory.Items.Count > 0)
            inventory.SelectIndex(Mathf.Max(0, inventory.SelectedIndex-Columns));
    }
    private void OnDestroy()
    {
        if (inventory != null) inventory.Changed -= Refresh;
    }

    private void BuildInterface()
    {
        interfaceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("Inventario_Xunjuu_UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        root.transform.SetParent(transform,false);
        Canvas canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2600;
        CanvasScaler scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        GameStateManager.RegisterHud(root);
        Button open=MakeButton(root.transform,"Abrir_Inventario","MORRAL  [I]",new Vector2(252,60),Toggle);
        Place(open.GetComponent<RectTransform>(),new Vector2(1,0),new Vector2(1,0),new Vector2(-28,28));
        panel=ImageObject("Panel_Inventario",root.transform,Wood).gameObject;
        RectTransform rect=panel.GetComponent<RectTransform>();
        rect.sizeDelta=new Vector2(864,642);Place(rect,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero);
        Border(panel.transform,new Vector2(864,642));
        Label(panel.transform,"MORRAL DE MATEO",28,TextAnchor.MiddleLeft,new Vector2(580,46),new Vector2(-102,263));
        capacityText=Label(panel.transform,"",16,TextAnchor.MiddleLeft,new Vector2(620,24),new Vector2(-82,226));
        Button close=MakeButton(panel.transform,"Cerrar","X",new Vector2(44,42),()=>SetVisible(false));
        close.GetComponent<RectTransform>().anchoredPosition=new Vector2(384,265);
        // Small embroidered accents and a double wooden rim, drawn natively without stock image text.
        for(int i=0;i<27;i++)
        {
            var stitch=ImageObject("Puntada",panel.transform,i%2==0?Gold:new Color(.72f,.24f,.31f));
            stitch.rectTransform.sizeDelta=new Vector2(7,7);stitch.rectTransform.anchoredPosition=new Vector2(-390+i*30,199);
            stitch.rectTransform.localRotation=Quaternion.Euler(0,0,45);
        }
        var viewport=ImageObject("Cuadricula_Madera",panel.transform,new Color(.18f,.09f,.10f));
        viewport.rectTransform.sizeDelta=new Vector2(808,372);viewport.rectTransform.anchoredPosition=new Vector2(0,-4);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content=new GameObject("Objetos_Con_Sprites",typeof(RectTransform),typeof(GridLayoutGroup),typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform,false);itemContainer=content.GetComponent<RectTransform>();
        itemContainer.anchorMin=new Vector2(0,1);itemContainer.anchorMax=new Vector2(1,1);itemContainer.pivot=new Vector2(.5f,1);
        itemContainer.sizeDelta=Vector2.zero;
        GridLayoutGroup grid=content.GetComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount=Columns;grid.cellSize=new Vector2(90,80);grid.spacing=new Vector2(9,9);grid.padding=new RectOffset(12,12,12,12);
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=itemContainer;
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        var detail=ImageObject("Detalle_Seleccion",panel.transform,new Color(.22f,.11f,.12f));
        detail.rectTransform.sizeDelta=new Vector2(808,66);detail.rectTransform.anchoredPosition=new Vector2(0,-235);
        selectedIcon=ImageObject("Icono_Seleccion",detail.transform,Color.white);
        selectedIcon.rectTransform.sizeDelta=new Vector2(46,46);selectedIcon.rectTransform.anchoredPosition=new Vector2(-368,0);selectedIcon.preserveAspect=true;
        selectedItemText=Label(detail.transform,"",20,TextAnchor.MiddleLeft,new Vector2(660,52),new Vector2(15,0));
        Label(panel.transform,"I  abrir / cerrar    |    Flechas o clic para elegir",16,TextAnchor.MiddleCenter,new Vector2(790,26),new Vector2(0,-290));
    }
    private void Refresh()
    {
        if(itemContainer==null||inventory==null)return;
        for(int i=itemContainer.childCount-1;i>=0;i--)
        { var child=itemContainer.GetChild(i);child.SetParent(null,false);Destroy(child.gameObject); }
        int count=Mathf.Max(32,Mathf.CeilToInt(inventory.Capacity/(float)Columns)*Columns);
        for(int i=0;i<count;i++) CreateSlot(i);
        var selected=inventory.SelectedItem;
        capacityText.text=inventory.Items.Count+" / "+inventory.Capacity+" tipos de objetos  ·  Recursos y recuerdos del camino";
        selectedItemText.text=selected!=null?selected.DisplayName+"   ×"+selected.Amount:"Tu morral está vacío. Recoge recursos para llenarlo.";
        selectedIcon.sprite=selected!=null?selected.Icon:null;selectedIcon.enabled=selected!=null&&selected.Icon!=null;
        Canvas.ForceUpdateCanvases();
        if(inventory.SelectedIndex>=0 && itemContainer.rect.height>scroll.viewport.rect.height)
        {
            float row=inventory.SelectedIndex/Columns;
            float max=Mathf.Max(1,itemContainer.rect.height-scroll.viewport.rect.height);
            scroll.verticalNormalizedPosition=1-Mathf.Clamp01(Mathf.Max(0,row*89-220)/max);
        }
    }
    private void CreateSlot(int index)
    {
        bool occupied=index<inventory.Items.Count,selected=occupied&&index==inventory.SelectedIndex;
        var rim=ImageObject("Casilla_"+index,itemContainer,selected?Gold:Rim);
        Image inset=ImageObject("Interior",rim.transform,selected?new Color(.49f,.25f,.22f):Inset);
        Stretch(inset.rectTransform,4);
        var shade=ImageObject("Sombra_Superior",inset.transform,new Color(.18f,.07f,.10f,.55f));
        shade.rectTransform.anchorMin=new Vector2(0,1);shade.rectTransform.anchorMax=new Vector2(1,1);
        shade.rectTransform.pivot=new Vector2(.5f,1);shade.rectTransform.sizeDelta=new Vector2(0,4);
        if(!occupied)
        {
            if(index>=inventory.Capacity)rim.color=new Color(.37f,.24f,.19f);
            return;
        }
        var slot=inventory.Items[index];
        Button button=rim.gameObject.AddComponent<Button>();button.targetGraphic=rim;
        int selectedIndex=index;button.onClick.AddListener(()=>inventory.SelectIndex(selectedIndex));
        var icon=ImageObject("Icono",rim.transform,Color.white);icon.sprite=slot.Icon;icon.preserveAspect=true;
        icon.rectTransform.sizeDelta=new Vector2(54,49);icon.rectTransform.anchoredPosition=new Vector2(0,5);
        if(slot.Icon==null)
        {
            icon.enabled=false;
            Label(rim.transform,slot.DisplayName,12,TextAnchor.MiddleCenter,new Vector2(75,46),new Vector2(0,6));
        }
        Label(rim.transform,"×"+slot.Amount,15,TextAnchor.LowerRight,new Vector2(70,22),new Vector2(0,-23));
        // Only the slot receives the click: decorative children never intercept input.
        rim.raycastTarget=true;
    }
    public void Toggle()=>SetVisible(panel==null||!panel.activeSelf);
    public void SetVisible(bool visible){if(panel!=null)panel.SetActive(visible);}
    private Button MakeButton(Transform parent,string name,string text,Vector2 size,UnityEngine.Events.UnityAction onClick)
    {
        var face=ImageObject(name,parent,Wood);face.rectTransform.sizeDelta=size;face.raycastTarget=true;
        Border(face.transform,size);
        Button button=face.gameObject.AddComponent<Button>();button.targetGraphic=face;
        button.onClick.AddListener(onClick);
        Label(face.transform,text,19,TextAnchor.MiddleCenter,size-Vector2.one*8,Vector2.zero);
        return button;
    }
    private static void Border(Transform parent,Vector2 size)
    {
        for(int edge=0;edge<4;edge++)
        {
            bool horizontal=edge<2;
            var line=ImageObject("Marco_Madera",parent,Rim);
            line.rectTransform.sizeDelta=horizontal?new Vector2(size.x,6):new Vector2(6,size.y);
            line.rectTransform.anchoredPosition=horizontal?new Vector2(0,(edge==0?1:-1)*(size.y*.5f-3)):new Vector2((edge==2?1:-1)*(size.x*.5f-3),0);
        }
        foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})
        {
            var nail=ImageObject("Clavo_Laton",parent,Gold);nail.rectTransform.sizeDelta=new Vector2(5,5);
            nail.rectTransform.anchoredPosition=new Vector2(x*(size.x*.5f-12),y*(size.y*.5f-12));
        }
    }
    private static Image ImageObject(string name,Transform parent,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        Image image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    private Text Label(Transform parent,string value,int size,TextAnchor alignment,Vector2 dimensions,Vector2 position)
    {
        var go=new GameObject("Texto",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        var text=go.GetComponent<Text>();text.font=interfaceFont;text.text=value;text.color=Cream;text.fontSize=size;text.alignment=alignment;
        text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
        text.rectTransform.sizeDelta=dimensions;text.rectTransform.anchoredPosition=position;return text;
    }
    private static void Place(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 pos)
    {rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=pos;}
    private static void Stretch(RectTransform rect,float padding)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.one*padding;rect.offsetMax=-Vector2.one*padding;}
    private static void EnsureEventSystem()
    {if(EventSystem.current==null)new GameObject("EventSystem_Xunjuu",typeof(EventSystem),typeof(StandaloneInputModule));}
}
