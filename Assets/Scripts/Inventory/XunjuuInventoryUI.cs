using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class XunjuuInventoryUI : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey=KeyCode.I;
    [SerializeField] private bool startVisible;
    private XunjuuInventory inventory;
    private XunjuuHerbalCrafting crafting;
    private DyanatroGameDirector director;
    private GameObject root, panel, dim;
    private RectTransform cells;
    private Text capacity, itemName, itemCategory, itemDescription, stock, recipeMessage;
    private Text chamomileStock, mintStock;
    private Image portrait;
    private Button use, prepare, open;
    private readonly List<int> visibleItems=new List<int>();
    private readonly List<Button> tabs=new List<Button>();
    private sealed class SlotView
    {
        public GameObject root;
        public Button button;
        public Image icon, quantityPlate;
        public Text title, category, quantity, fallback;
        public Outline outline;
        public int itemIndex = -1;
    }
    private readonly List<SlotView> slotViews = new List<SlotView>();
    private int filter;
    private bool holdsPause;
    private float previousScale;
    private float nextRecipeUpdate;
    public bool IsPanelOpen => panel!=null&&panel.activeSelf;
    public bool IsVisible => IsPanelOpen&&(director==null||director.IsGameplayHudVisible);

    private void Start()
    {
        director=FindFirstObjectByType<DyanatroGameDirector>();
        inventory=GetComponent<XunjuuInventory>()??gameObject.AddComponent<XunjuuInventory>();
        crafting=GetComponent<XunjuuHerbalCrafting>()??gameObject.AddComponent<XunjuuHerbalCrafting>();
        if(EventSystem.current==null)new GameObject("EventSystem_Xunjuu",typeof(EventSystem),typeof(StandaloneInputModule));
        Build();inventory.Changed+=InventoryChanged;SetVisible(startVisible);Refresh();
    }
    private void Update()
    {
        if(director!=null&&!director.IsGameplayHudVisible)return;
        if(Input.GetKeyDown(toggleKey))Toggle();
        if(!IsVisible)return;
        if(Input.GetKeyDown(KeyCode.H)){crafting.TryUseRemedy();Refresh();}
        if(Time.unscaledTime>=nextRecipeUpdate){nextRecipeUpdate=Time.unscaledTime+.1f;Recipe();}
        if(Input.GetKeyDown(KeyCode.RightArrow))inventory.SelectNext();
        if(Input.GetKeyDown(KeyCode.LeftArrow))inventory.SelectPrevious();
    }
    private static GameObject Surface(Transform parent,string name,Vector2 size,Vector2 at,Color color)
        => XunjuuInterfaceTheme.Panel(parent,name,size,at,color);
    private static Text Label(Transform parent,string name,string text,int size,Vector2 box,Vector2 at,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
        => XunjuuInterfaceTheme.Label(parent,name,text,size,box,at,color,alignment);
    private void Build()
    {
        root=new GameObject("Inventario_Xunjuu_UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2700;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=0;
        GameStateManager.RegisterHud(root,false);
        open=XunjuuInterfaceTheme.Button(root.transform,"Abrir_Inventario","MORRAL  [I]",new Vector2(220,54),Vector2.zero,Toggle);
        var openRect=open.GetComponent<RectTransform>();openRect.anchorMin=openRect.anchorMax=openRect.pivot=new Vector2(1,0);openRect.anchoredPosition=new Vector2(-26,24);
        dim=Surface(root.transform,"Fondo_Modal",Vector2.zero,Vector2.zero,new Color(.015f,.025f,.023f,.80f));
        var dr=dim.GetComponent<RectTransform>();dr.anchorMin=Vector2.zero;dr.anchorMax=Vector2.one;dr.offsetMin=dr.offsetMax=Vector2.zero;dim.GetComponent<Image>().raycastTarget=true;
        panel=XunjuuModalStyle.Card(dim.transform,"Panel_Inventario",new Vector2(1280,840),Vector2.zero);
        Label(panel.transform,"Titulo","MORRAL  /  MEMORIA Y CUIDADO",30,new Vector2(1050,46),new Vector2(-63,365),XunjuuInterfaceTheme.Paper);
        capacity=Label(panel.transform,"Capacidad","",18,new Vector2(1110,30),new Vector2(-33,326),XunjuuInterfaceTheme.Muted);
        XunjuuModalStyle.Action(panel.transform,"Cerrar","×",new Vector2(44,44),new Vector2(590,365),()=>SetVisible(false),XunjuuModalStyle.Quiet);
        string[] names={"TODO","MEMORIA","PLANTAS","PREPARADOS"};
        for(int i=0;i<4;i++)
        {
            int selected=i;
            tabs.Add(XunjuuModalStyle.Action(panel.transform,"Filtro_"+i,names[i],new Vector2(177,38),new Vector2(-499+i*189,276),()=>{filter=selected;Refresh();},XunjuuModalStyle.Accent));
        }
        var viewport=Surface(panel.transform,"Cuadricula_Objetos",new Vector2(752,474),new Vector2(-218,0),new Color(.09f,.15f,.13f));
        viewport.AddComponent<RectMask2D>();
        var content=new GameObject("Objetos_Con_Sprites",typeof(RectTransform),typeof(GridLayoutGroup),typeof(ContentSizeFitter));content.transform.SetParent(viewport.transform,false);cells=content.GetComponent<RectTransform>();
        cells.anchorMin=new Vector2(0,1);cells.anchorMax=Vector2.one;cells.pivot=new Vector2(.5f,1);cells.sizeDelta=Vector2.zero;
        var grid=content.GetComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=5;grid.cellSize=new Vector2(136,132);grid.spacing=new Vector2(10,10);grid.padding=new RectOffset(16,16,16,16);
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.AddComponent<ScrollRect>();scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=cells;scroll.horizontal=false;scroll.scrollSensitivity=24;scroll.movementType=ScrollRect.MovementType.Clamped;
        var detail=Surface(panel.transform,"Ficha_Objeto",new Vector2(752,156),new Vector2(-218,-326),XunjuuModalStyle.Inset);
        var portraitPlate=Surface(detail.transform,"Fondo_Retrato",new Vector2(112,128),new Vector2(-306,0),new Color(.85f,.88f,.79f));
        portrait=Surface(portraitPlate.transform,"Retrato",new Vector2(96,112),Vector2.zero,Color.white).GetComponent<Image>();portrait.preserveAspect=true;
        itemCategory=Label(detail.transform,"Categoria","",13,new Vector2(570,22),new Vector2(43,48),XunjuuInterfaceTheme.Gold);
        itemName=Label(detail.transform,"Nombre","",21,new Vector2(570,28),new Vector2(43,20),XunjuuInterfaceTheme.Paper);
        itemDescription=Label(detail.transform,"Descripcion","",17,new Vector2(570,70),new Vector2(43,-27),XunjuuInterfaceTheme.Muted,TextAnchor.UpperLeft);
        var workshop=Surface(panel.transform,"Preparacion_Herbal",new Vector2(408,680),new Vector2(420,-52),XunjuuInterfaceTheme.Paper);
        Label(workshop.transform,"Titulo","PREPARACIÓN DE HIERBAS",15,new Vector2(360,28),new Vector2(0,298),XunjuuInterfaceTheme.Forest,TextAnchor.MiddleCenter);
        Label(workshop.transform,"Nombre","Infusión de hierbas",25,new Vector2(360,40),new Vector2(0,259),XunjuuInterfaceTheme.Ink,TextAnchor.MiddleCenter).fontStyle=FontStyle.Bold;
        var icon=Surface(workshop.transform,"Infusion",new Vector2(118,118),new Vector2(0,175),Color.white).GetComponent<Image>();icon.sprite=XunjuuMedicinalPlantCatalog.RemedySprite;icon.preserveAspect=true;
        var benefit=Surface(workshop.transform,"Beneficio",new Vector2(352,52),new Vector2(0,82),new Color(.83f,.88f,.79f));
        Label(benefit.transform,"Efecto","+40 de salud · 5 segundos",20,new Vector2(328,44),Vector2.zero,XunjuuInterfaceTheme.Forest,TextAnchor.MiddleCenter).fontStyle=FontStyle.Bold;
        chamomileStock=IngredientRow(workshop.transform,"manzanilla",14);
        mintStock=IngredientRow(workshop.transform,"hierbabuena",-46);
        stock=Label(workshop.transform,"Ingredientes","",18,new Vector2(352,30),new Vector2(0,-99),XunjuuInterfaceTheme.Forest,TextAnchor.MiddleCenter);
        prepare=XunjuuModalStyle.Action(workshop.transform,"Preparar","Preparar infusión",new Vector2(352,46),new Vector2(0,-149),()=>{crafting.TryCraft();Refresh();},new Color(.16f,.47f,.36f));
        use=XunjuuModalStyle.Action(workshop.transform,"Usar","Usar infusión  [H]",new Vector2(352,46),new Vector2(0,-207),()=>{crafting.TryUseRemedy();Refresh();},new Color(.21f,.48f,.57f));
        recipeMessage=Label(workshop.transform,"Mensaje","",16,new Vector2(352,56),new Vector2(0,-266),XunjuuInterfaceTheme.Forest,TextAnchor.UpperLeft);
        Label(workshop.transform,"Advertencia","Receta ficticia del juego.\nNo es una recomendación médica.",13,new Vector2(360,34),new Vector2(0,-319),XunjuuInterfaceTheme.Forest,TextAnchor.MiddleCenter);
        panel.SetActive(false);dim.SetActive(false);
    }
    private bool Matches(XunjuuInventory.Slot slot)
        => filter==0||filter==1&&slot.Kind==XunjuuItemCatalog.ItemKind.MemoryFlower||filter==2&&slot.Kind==XunjuuItemCatalog.ItemKind.MedicinalIngredient||filter==3&&slot.Kind==XunjuuItemCatalog.ItemKind.Preparation;
    private void InventoryChanged(){if(IsPanelOpen)Refresh();}
    private void Refresh()
    {
        if(cells==null)return;
        visibleItems.Clear();
        for(int i=0;i<inventory.Items.Count;i++)if(Matches(inventory.Items[i]))visibleItems.Add(i);
        int count=filter==0?inventory.Capacity:Mathf.Max(5,visibleItems.Count);
        for(int i=0;i<count;i++)Slot(i);
        for(int i=count;i<slotViews.Count;i++)slotViews[i].root.SetActive(false);
        for(int i=0;i<tabs.Count;i++)tabs[i].GetComponent<Image>().color=i==filter?new Color(.30f,.42f,.30f):XunjuuInterfaceTheme.Forest;
        capacity.text=inventory.Items.Count+" / "+inventory.Capacity+" tipos de objetos  ·  Flores de memoria, ingredientes y preparados separados";
        var selected=inventory.SelectedItem;
        portrait.sprite=selected!=null?ItemIcon(selected):null;portrait.enabled=portrait.sprite!=null;
        itemName.text=selected!=null?selected.DisplayName+"  ×"+selected.Amount:"Selecciona un objeto";
        itemCategory.text=selected?.Category??"TU MORRAL";
        itemDescription.text=selected?.Description??"Las flores guardan palabras; las plantas son ingredientes. Solo los preparados recuperan salud.";
        Recipe();
    }
    private void Slot(int index)
    {
        bool occupied=index<visibleItems.Count;
        if(index>=slotViews.Count)
        {
            var view=new SlotView();
            view.root=Surface(cells,"Casilla_"+index,new Vector2(136,132),Vector2.zero,XunjuuModalStyle.Inset);
            view.root.GetComponent<XunjuuRoundedImage>().CornerRadius=12;
            view.button=view.root.AddComponent<Button>();view.button.targetGraphic=view.root.GetComponent<Image>();view.root.GetComponent<Image>().raycastTarget=true;
            view.button.onClick.AddListener(()=>{if(view.itemIndex>=0)inventory.SelectIndex(view.itemIndex);});view.root.AddComponent<XunjuuButtonMotion>();
            view.outline=view.root.AddComponent<Outline>();view.outline.effectDistance=new Vector2(1,-1);
            view.icon=Surface(view.root.transform,"Icono",new Vector2(100,68),new Vector2(0,10),Color.white).GetComponent<Image>();view.icon.preserveAspect=true;
            view.fallback=Label(view.root.transform,"Recurso","◆",32,new Vector2(80,60),new Vector2(0,3),XunjuuInterfaceTheme.Gold,TextAnchor.MiddleCenter);
            view.category=Label(view.root.transform,"Tipo","",11,new Vector2(68,20),new Vector2(-24,52),XunjuuModalStyle.Accent);
            view.quantityPlate=Surface(view.root.transform,"Etiqueta_Cantidad",new Vector2(42,22),new Vector2(43,51),XunjuuModalStyle.Surface).GetComponent<Image>();
            view.quantity=Label(view.quantityPlate.transform,"Cantidad","",14,new Vector2(40,22),Vector2.zero,XunjuuInterfaceTheme.Paper,TextAnchor.MiddleCenter);
            view.title=Label(view.root.transform,"Nombre_Objeto","",15,new Vector2(120,38),new Vector2(0,-44),XunjuuInterfaceTheme.Paper,TextAnchor.MiddleCenter);view.title.fontStyle=FontStyle.Bold;
            view.title.resizeTextForBestFit=true;view.title.resizeTextMinSize=13;view.title.resizeTextMaxSize=15;
            slotViews.Add(view);
        }
        var item=slotViews[index];item.root.SetActive(true);item.button.interactable=occupied;item.itemIndex=occupied?visibleItems[index]:-1;
        var slot=occupied?inventory.Items[item.itemIndex]:null;bool selected=occupied&&item.itemIndex==inventory.SelectedIndex;
        Color accent=slot==null?XunjuuModalStyle.Quiet:slot.Kind==XunjuuItemCatalog.ItemKind.MemoryFlower?new Color(.89f,.65f,.33f):slot.Kind==XunjuuItemCatalog.ItemKind.Preparation?new Color(.49f,.74f,.86f):new Color(.41f,.75f,.55f);
        item.root.GetComponent<Image>().color=occupied?Color.Lerp(XunjuuModalStyle.Inset,accent,selected ? .24f : .08f):new Color(.055f,.105f,.105f);
        item.outline.enabled=selected;item.outline.effectColor=accent;item.icon.sprite=slot!=null?ItemIcon(slot):null;item.icon.enabled=occupied&&item.icon.sprite!=null;
        item.fallback.enabled=occupied&&item.icon.sprite==null;item.category.text=slot==null?"":slot.Kind==XunjuuItemCatalog.ItemKind.MemoryFlower?"MEMORIA":slot.Kind==XunjuuItemCatalog.ItemKind.MedicinalIngredient?"PLANTA":slot.Kind==XunjuuItemCatalog.ItemKind.Preparation?"SALUD":"RECURSO";
        item.category.color=accent;item.quantityPlate.gameObject.SetActive(occupied);item.quantity.text=slot!=null?"×"+slot.Amount:"";item.title.text=slot?.DisplayName??"";
    }
    private static Sprite ItemIcon(XunjuuInventory.Slot slot)
    {
        if(slot.Icon!=null)return slot.Icon;
        var plant=XunjuuMedicinalPlantCatalog.Find(slot.ItemId);
        if(plant!=null)return XunjuuMedicinalPlantCatalog.GetSprite(plant);
        if(slot.Kind==XunjuuItemCatalog.ItemKind.Preparation)return XunjuuMedicinalPlantCatalog.RemedySprite;
        if(slot.Kind==XunjuuItemCatalog.ItemKind.MemoryFlower)return XunjuuPresentationSprites.Load("Sprites/Collectibles/FlorMemoria_v2",1.3f,new Vector2(.5f,0));
        return null;
    }
    private static Text IngredientRow(Transform parent,string id,float y)
    {
        var entry=XunjuuMedicinalPlantCatalog.Find(id);
        var row=Surface(parent,"Ingrediente_"+id,new Vector2(352,52),new Vector2(0,y),new Color(.88f,.90f,.83f));
        var image=Surface(row.transform,"Icono",new Vector2(42,44),new Vector2(-146,0),Color.white).GetComponent<Image>();image.sprite=XunjuuMedicinalPlantCatalog.GetSprite(entry);image.preserveAspect=true;
        Label(row.transform,"Nombre",entry.DisplayName,18,new Vector2(186,40),new Vector2(-27,0),XunjuuInterfaceTheme.Forest);
        return Label(row.transform,"Existencias","",18,new Vector2(76,40),new Vector2(126,0),XunjuuInterfaceTheme.Forest,TextAnchor.MiddleRight);
    }
    private void Recipe()
    {
        if(stock==null)return;
        stock.text="Infusiones disponibles: "+inventory.GetAmount(XunjuuHerbalCrafting.RemedyId);
        chamomileStock.text=inventory.GetAmount("manzanilla")+" / 2";mintStock.text=inventory.GetAmount("hierbabuena")+" / 1";
        chamomileStock.color=inventory.GetAmount("manzanilla")>=2?XunjuuInterfaceTheme.Forest:new Color(.64f,.24f,.16f);
        mintStock.color=inventory.GetAmount("hierbabuena")>=1?XunjuuInterfaceTheme.Forest:new Color(.64f,.24f,.16f);
        recipeMessage.text=crafting.LastMessage;
        prepare.interactable=crafting.CanCraft;
        var player=GetComponent<PlayerController>();
        use.interactable=inventory.GetAmount(XunjuuHerbalCrafting.RemedyId)>0&&!crafting.IsRecovering&&player.GetCurrentHealth()>0&&player.GetCurrentHealth()<player.GetMaxHealth();
    }
    public void Toggle()=>SetVisible(!IsPanelOpen);
    public void SetVisible(bool visible)
    {
        if(panel==null)return;
        if(visible&&!IsPanelOpen){previousScale=Time.timeScale;holdsPause=true;Time.timeScale=0f;Refresh();}
        else if(!visible&&holdsPause){if(Time.timeScale<=0f)Time.timeScale=previousScale;holdsPause=false;}
        panel.SetActive(visible);dim.SetActive(visible);
        open.gameObject.SetActive(!visible);
    }
    private void OnDestroy()
    {
        if(inventory!=null)inventory.Changed-=InventoryChanged;
        if(holdsPause&&Time.timeScale<=0f)Time.timeScale=previousScale;
    }
}
