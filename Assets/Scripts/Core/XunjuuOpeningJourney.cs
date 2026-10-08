using UnityEngine;
using UnityEngine.UI;

// Additive onboarding/story layer. Existing mission components own all rewards,
// damage, counts and unlocks; this layer only observes actions and explains them.
public sealed class XunjuuOpeningJourney : MonoBehaviour
{
    private PlayerController player;
    private DyanatroGameDirector director;
    private XunjuuLevel2KillMission mission;
    private GameObject panel;
    private Text title,body;
    private Button skip;
    private Vector3 previousPosition;
    private float distance,storyUntil;
    private bool jumped;
    private int stage;
    private XunjuuLevel2KillMission.MissionPhase lastPhase;
    public bool TutorialActive {get;private set;}
    public int TutorialStage=>stage;
    public void Begin(PlayerController actor,Transform canvasRoot)
    {
        player=actor;director=GetComponent<DyanatroGameDirector>();mission=FindFirstObjectByType<XunjuuLevel2KillMission>();
        previousPosition=actor.transform.position;distance=0;stage=0;jumped=false;TutorialActive=true;
        lastPhase=XunjuuLevel2KillMission.MissionPhase.Locked;
        if(panel==null)
        {
            panel=new GameObject("Aprendizaje_y_Relato",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvasRoot,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,0);
            rect.anchoredPosition=new Vector2(28,32);rect.sizeDelta=new Vector2(510,254);
            panel.GetComponent<Image>().color=new Color(.035f,.065f,.055f,.92f);panel.GetComponent<Image>().raycastTarget=false;
            title=Label(panel.transform,new Vector2(20,-16),new Vector2(470,35),22,new Color(1,.82f,.39f));
            body=Label(panel.transform,new Vector2(20,-60),new Vector2(470,142),21,Color.white);
            var button=new GameObject("Omitir_aprendizaje",typeof(RectTransform),typeof(Image),typeof(Button));button.transform.SetParent(panel.transform,false);
            var br=button.GetComponent<RectTransform>();br.anchorMin=br.anchorMax=br.pivot=new Vector2(0,0);br.anchoredPosition=new Vector2(20,12);br.sizeDelta=new Vector2(290,31);
            button.GetComponent<Image>().color=new Color(.22f,.18f,.12f,.95f);skip=button.GetComponent<Button>();skip.onClick.AddListener(FinishTutorial);
            Label(button.transform,Vector2.zero,br.sizeDelta,17,Color.white).text="Ya conozco los controles";
        }
        panel.SetActive(true);skip.gameObject.SetActive(true);ShowStep();
    }
    private static Text Label(Transform parent,Vector2 position,Vector2 size,int fontSize,Color color)
    {
        var go=new GameObject("Texto",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=position;rt.sizeDelta=size;
        var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.color=color;text.raycastTarget=false;text.alignment=TextAnchor.UpperLeft;
        return text;
    }
    private void Update()
    {
        if(player==null || panel==null)return;
        if(!director.IsGameplayHudVisible){panel.SetActive(false);previousPosition=player.transform.position;return;}
        if(TutorialActive)
        {
            panel.SetActive(true);
            Vector3 delta=player.transform.position-previousPosition;previousPosition=player.transform.position;delta.y=0;
            if(Time.timeScale<=0)return;
            if(stage==0 && delta.magnitude<2)distance+=delta.magnitude;
            bool done=stage==0 && distance>=4;
            if(stage==1){jumped|=player.IsJumping();done=jumped && player.IsGrounded() && !player.IsJumping();}
            if(stage==2)done=player.IsAttacking() && !player.HasOrbitalWeapon();
            if(stage==3){var inv=player.GetComponent<XunjuuInventoryUI>();done=inv!=null && inv.IsVisible;}
            if(done){stage++;if(stage>=4)FinishTutorial();else ShowStep();}
            return;
        }
        if(mission!=null && mission.CurrentPhase!=lastPhase)
        {
            lastPhase=mission.CurrentPhase;
            switch(lastPhase)
            {
                case XunjuuLevel2KillMission.MissionPhase.Animals:
                    Story("II · EL SENDERO ALTERADO","Las cinco palabras orientan a Mateo. La fauna está desorientada: acércate con calma y pulsa C para capturar y registrar seis especies en la Ludoteca. No debes atacarlas.");break;
                case XunjuuLevel2KillMission.MissionPhase.Enemies:
                    Story("III · EL ORIGEN DEL DESORDEN","Tras registrar las seis especies, la comunidad confía el Macuahuitl a Mateo. Ahora puede enfrentar a los cinco Dyanatr'o que cortan el camino hacia la milpa.");break;
                case XunjuuLevel2KillMission.MissionPhase.Boss:
                    Story("IV · EL GUARDIÁN DEL PASO","Al vencer a las cinco sombras, Mateo aprende el giro protector. E activa el ataque orbital. Ocelotl custodia el último paso: enfrenta al jefe para reabrir el camino.");break;
                case XunjuuLevel2KillMission.MissionPhase.Completed:
                    Story("V · VOLVER PARA COMPARTIR","El camino queda abierto. Mateo vuelve con las palabras recuperadas y la recompensa. Proteger su hogar también significa compartir lo aprendido: la memoria continúa en la comunidad.");break;
            }
        }
        panel.SetActive(Time.unscaledTime<storyUntil);
    }
    private void ShowStep()
    {
        body.fontSize=21;
        title.text="ANTES DEL CAMINO · "+(stage+1)+" / 4";
        string[] text={
            "Mateo sale al claro sin arma. Antes de buscar las flores, reconoce el terreno: camina cuatro metros con W, A, S y D.",
            "El sendero exige pasos seguros. Pulsa ESPACIO para saltar y espera a volver al suelo.",
            "Practica un golpe al aire con CLIC IZQUIERDO. Aún no tienes arma; primero aprenderás a moverte y a escuchar.",
            "Pulsa I para abrir tu morral. Aquí guardarás las palabras y recompensas del viaje. Pulsa I de nuevo para cerrarlo."};
        body.text=text[Mathf.Clamp(stage,0,3)];
    }
    public void FinishTutorial()
    {
        if(!TutorialActive)return;TutorialActive=false;stage=4;
        skip.gameObject.SetActive(false);
        director.OnOpeningTutorialCompleted();
        Story("I · CINCO PALABRAS PARA VOLVER","La milpa y sus caminos están en peligro. Busca las cinco flores-palabra del entorno: cada una conserva un recuerdo. La comunidad te confiará el macuahuitl cuando reúnas las cinco.");
    }
    private void Story(string heading,string text)
    {title.text=heading;body.text=text;body.fontSize=19;storyUntil=Time.unscaledTime+14;panel.SetActive(true);}
    private void OnDestroy(){if(panel!=null)Destroy(panel);}
}
