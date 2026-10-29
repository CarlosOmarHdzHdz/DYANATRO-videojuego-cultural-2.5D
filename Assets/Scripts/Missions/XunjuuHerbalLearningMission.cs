using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Puente entre fauna y combate: reconocer, consultar y preparar antes de continuar.
[DisallowMultipleComponent]
public sealed class XunjuuHerbalLearningMission : MonoBehaviour
{
    public enum LearningStage { Dormant, Gather, Study, Prepare, Recover, Completed }
    private readonly HashSet<string> read = new HashSet<string>();
    private int chamomile, mint;
    private PlayerController player;
    private DyanatroGameDirector director;
    private XunjuuInventory inventory;
    private XunjuuHerbalCrafting crafting;
    private GameObject root, card;
    private Text title, body, hint, actionText;
    private Image progress;
    private Button action;
    private Button chamomileAnswer, mintAnswer;
    private bool identified;
    private string identificationHint;
    private Transform gardenTarget;
    private LearningStage paintedStage = (LearningStage)(-1);
    private string paintedBody;
    public LearningStage Stage { get; private set; }
    public int CardsRead => read.Count;
    public bool IdentificationCorrect => identified;
    public bool PreparationLearned => identified && crafting!=null && crafting.CraftedCount>0;
    public bool TrackGarden { get; private set; } = true;
    public Transform GuideTarget => Stage == LearningStage.Gather && TrackGarden ? gardenTarget : null;

    private void Awake()
    {
        director = GetComponent<DyanatroGameDirector>();
        XunjuuMedicinalPlant.Harvested += Harvest;
        XunjuuMedicinalPlantCatalog.CardViewed += Read;
    }
    private void Harvest(string id) { if(id=="manzanilla")chamomile++;if(id=="hierbabuena")mint++; }
    private void Read(string id) { if(id=="manzanilla"||id=="hierbabuena")read.Add(id); }
    public void Begin()
    {
        if(Stage!=LearningStage.Dormant)return;
        var progression=FindFirstObjectByType<XunjuuLevel2KillMission>();
        if(progression==null||progression.CurrentPhase!=XunjuuLevel2KillMission.MissionPhase.Herbal)return;
        player=FindFirstObjectByType<PlayerController>();
        if(player==null)return;
        inventory=player.GetComponent<XunjuuInventory>();crafting=player.GetComponent<XunjuuHerbalCrafting>();
        if(inventory==null||crafting==null)return;
        Build();Stage=LearningStage.Gather;
        var target=new GameObject("Guia_Huerto_Aprendizaje");target.transform.SetParent(transform,false);
        Vector3 point=XunjuuSettlementPlan.HerbGarden.center;
        if(XunjuuSettlementPlan.TryGround(point,out float y))point.y=y;
        target.transform.position=point;gardenTarget=target.transform;
        RefreshProgress();
    }
    private void Build()
    {
        root=new GameObject("HUD_Aprendizaje_Plantas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2500;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        GameStateManager.RegisterHud(root);
        card=XunjuuInterfaceTheme.Panel(root.transform,"Cuaderno_Herbolaria",new Vector2(365,286),Vector2.zero,XunjuuInterfaceTheme.Ink);
        var rt=card.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(26,-200);
        XunjuuInterfaceTheme.Frame(card.transform,new Vector2(365,286));
        // Frame children use centred coordinates; pivot at the centre keeps layout predictable.
        rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(208,-398);
        XunjuuInterfaceTheme.Label(card.transform,"Categoria","MISIÓN  /  SABERES DEL HUERTO",13,new Vector2(317,24),new Vector2(0,112),XunjuuInterfaceTheme.Gold,TextAnchor.MiddleLeft);
        title=XunjuuInterfaceTheme.Label(card.transform,"Paso","",23,new Vector2(317,40),new Vector2(0,77),XunjuuInterfaceTheme.Paper,TextAnchor.MiddleLeft);
        body=XunjuuInterfaceTheme.Label(card.transform,"Objetivo","",18,new Vector2(317,74),new Vector2(0,21),XunjuuInterfaceTheme.Paper,TextAnchor.UpperLeft);
        var track=XunjuuInterfaceTheme.Panel(card.transform,"Progreso",new Vector2(317,4),new Vector2(0,-27),new Color(.24f,.34f,.29f));
        var bar=XunjuuInterfaceTheme.Panel(track.transform,"Avance",new Vector2(317,4),Vector2.zero,XunjuuInterfaceTheme.Gold);
        progress=bar.GetComponent<Image>();
        progress.rectTransform.pivot=new Vector2(0,.5f);progress.rectTransform.anchoredPosition=new Vector2(-158.5f,0);
        hint=XunjuuInterfaceTheme.Label(card.transform,"Consejo","",15,new Vector2(317,42),new Vector2(0,-57),XunjuuInterfaceTheme.Muted,TextAnchor.UpperLeft);
        action=XunjuuInterfaceTheme.Button(card.transform,"Accion_Mision","",new Vector2(317,39),new Vector2(0,-105),Act);
        actionText=action.GetComponentInChildren<Text>();
        chamomileAnswer=XunjuuInterfaceTheme.Button(card.transform,"Respuesta_Manzanilla","MANZANILLA",new Vector2(153,39),new Vector2(-82,-105),()=>TryIdentifyPlant("manzanilla"));
        mintAnswer=XunjuuInterfaceTheme.Button(card.transform,"Respuesta_Hierbabuena","HIERBABUENA",new Vector2(153,39),new Vector2(82,-105),()=>TryIdentifyPlant("hierbabuena"));
        chamomileAnswer.gameObject.SetActive(false);mintAnswer.gameObject.SetActive(false);
    }
    private void Update()
    {
        if(root==null)return;
        bool visible=director!=null&&director.IsGameplayHudVisible&&Time.timeScale>0f&&Stage!=LearningStage.Completed;
        if(card.activeSelf!=visible)card.SetActive(visible);
        if(!visible)return;
        RefreshProgress();
        if(Input.GetKeyDown(KeyCode.L)&&Stage==LearningStage.Study)Act();
    }
    public void RefreshProgress()
    {
        if(Stage==LearningStage.Dormant||Stage==LearningStage.Completed||inventory==null||crafting==null)return;
        int c=Mathf.Max(chamomile,inventory.GetAmount("manzanilla")+crafting.CraftedCount*2);
        int m=Mathf.Max(mint,inventory.GetAmount("hierbabuena")+crafting.CraftedCount);
        if(Stage==LearningStage.Gather&&c>=2&&m>=1)Stage=LearningStage.Study;
        if(Stage==LearningStage.Study&&read.Contains("manzanilla")&&read.Contains("hierbabuena")&&identified)Stage=LearningStage.Prepare;
        if(Stage==LearningStage.Prepare&&crafting.CraftedCount>0)
        {
            Stage=LearningStage.Recover;
            FindFirstObjectByType<XunjuuLevel2KillMission>()?.CompleteHerbalPreparation();
        }
        if(Stage==LearningStage.Recover&&crafting.UsedCount>0&&crafting.TotalRecovered>0&&!crafting.IsRecovering)
        {
            Stage=LearningStage.Completed;card.SetActive(false);
            XunjuuMissionNotice.For(director).Show("herbolaria-completa","B’epji","Trabajo completado: reconociste las plantas y aprendiste a recuperar salud.");
            return;
        }
        string text=Stage==LearningStage.Gather?$"Recolecta en el huerto:\nManzanilla {Mathf.Min(c,2)}/2  ·  Hierbabuena {Mathf.Min(m,1)}/1":
            Stage==LearningStage.Study?(read.Count<2?$"Consulta ambas fichas en la ludoteca.\nPlantas revisadas: {read.Count}/2":"¿Qué planta tiene flores blancas y el centro amarillo?"):
            Stage==LearningStage.Prepare?"Abre el morral [I] y combina dos manzanillas con una hierbabuena.":
            player.GetCurrentHealth()>=player.GetMaxHealth()?"Infusión lista. Tu salud está completa: guárdala hasta que recibas daño.":
            crafting.IsRecovering?"La infusión recupera salud poco a poco. Observa la barra de vida.":"Pulsa H para usar tu infusión y observa cómo se recupera tu salud.";
        if(paintedStage==Stage&&paintedBody==text)return;
        paintedStage=Stage;paintedBody=text;body.text=text;
        title.text=Stage==LearningStage.Gather?"01  Reconoce y recolecta":Stage==LearningStage.Study?"02  Aprende a distinguir":Stage==LearningStage.Prepare?"03  Prepara tu infusión":"04  Cuida tu salud";
        hint.text=Stage==LearningStage.Gather?"G  recolectar · Flores blancas: manzanilla. Hojas aromáticas: hierbabuena.":
            Stage==LearningStage.Study?(read.Count<2?"Compara sus rasgos y nombres científicos. L  abrir herbolaria.":
                string.IsNullOrEmpty(identificationHint)?"Elige la planta según sus rasgos. Puedes volver a consultar con L.":identificationHint):
            Stage==LearningStage.Prepare?"Receta exclusiva del videojuego; no es una indicación médica.":"Con vida completa no se gasta. Puedes seguir la historia.";
        actionText.text=Stage==LearningStage.Gather?(TrackGarden?"GUÍA DEL HUERTO ACTIVADA":"SEGUIR EL HUERTO"):
            Stage==LearningStage.Study?"CONSULTAR FICHAS  [L]":"ABRIR MORRAL  [I]";
        progress.rectTransform.localScale=new Vector3(((int)Stage-1)/4f,1,1);
        bool question=Stage==LearningStage.Study&&read.Count==2;
        action.gameObject.SetActive(!question);chamomileAnswer.gameObject.SetActive(question);mintAnswer.gameObject.SetActive(question);
    }
    public bool TryIdentifyPlant(string id)
    {
        if(Stage!=LearningStage.Study||read.Count!=2)return false;
        if(id!="manzanilla")
        {
            identificationHint="La hierbabuena tiene hojas aromáticas. Observa de nuevo las flores.";
            paintedBody=null;RefreshProgress();return false;
        }
        identified=true;paintedBody=null;RefreshProgress();return true;
    }
    private void Act()
    {
        if(Stage==LearningStage.Gather){TrackGarden=true;paintedBody=null;RefreshProgress();return;}
        if(Stage==LearningStage.Study){var browser=FindFirstObjectByType<XunjuuLudotecaBrowser>();if(browser!=null)browser.OpenHerbalism();return;}
        player.GetComponent<XunjuuInventoryUI>()?.SetVisible(true);
    }
    private void OnDestroy()
    {
        XunjuuMedicinalPlant.Harvested-=Harvest;XunjuuMedicinalPlantCatalog.CardViewed-=Read;
        if(root!=null)Destroy(root);if(gardenTarget!=null)Destroy(gardenTarget.gameObject);
    }
}
