using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Playable activities use unscaled time: the world stays paused while reading.
public sealed partial class XunjuuLudotecaBrowser
{
    private GameObject activityRoot;
    private TMP_Text activityStatus;
    private TMP_Text memoryInsight, quizProgress;
    private Button[] tiles;
    private Coroutine[] memoryFlips;
    private Sprite[] memoryImages;
    private int[] memoryDeck;
    private bool[] matched;
    private GameObject memoryBoard, memoryStageOverlay;
    private int memoryStage;
    private const int MemoryPairsPerStage=6;
    private const int MemoryMotifCount=18;
    private const int MemoryStageCount=MemoryMotifCount/MemoryPairsPerStage;
    private int firstTile=-1, secondTile=-1, pairs, attempts;
    private float hideAt;
    private bool activityComplete;
    private Action replayActivity;
    // The 18 framed motifs in the supplied textile image, read left-to-right, top-to-bottom.
    private readonly string[] memoryNames={
        "VENADO","ESTRELLA","ÁRBOL Y AVES","AVES Y FLORES","FLOR","PAREJA",
        "OLLAS Y PLANTA","PASTORA Y OVEJA","ZORRO","MILPA","DOS AVES","GRECAS",
        "JARDÍN FLORAL","FLORES","COLIBRÍ","RÍO","MARIPOSA","MAÍCES"
    };
    private readonly string[] memoryFacts={
        "Observa la forma de las astas y los colores del venado.",
        "La figura combina puntas y colores en un patrón geométrico.",
        "Identifica las aves, las flores y las ramas del árbol.",
        "Dos aves aparecen a ambos lados de una planta florida.",
        "Fíjate en los pétalos, las hojas y el contorno de color.",
        "Observa los detalles de la vestimenta de las dos personas.",
        "Las vasijas y las ramas forman una composición simétrica.",
        "Una persona, un textil y una oveja comparten la escena.",
        "Compara el perfil del animal con las flores que lo rodean.",
        "Reconoce las mazorcas, las hojas y los surcos dibujados.",
        "Busca las diferencias de color entre las dos aves.",
        "Sigue con la vista los caminos del diseño geométrico.",
        "Cuenta las flores y observa cómo se repite el patrón.",
        "Los colores de los pétalos se alternan en esta cuadrícula.",
        "Un colibrí se acerca a una flor de gran tamaño.",
        "Las líneas onduladas representan el movimiento del agua.",
        "Observa la simetría y los colores de sus alas.",
        "Las mazorcas y los pequeños signos repiten una secuencia."
    };
    private const int SearchSize=10;
    private readonly string[] searchWords={"MAZAHUA","TEJIDO","VENADO","BOSQUE","MILPA","FLOR","MAIZ","PAVO"};
    private readonly char[] searchLetters=new char[SearchSize*SearchSize];
    private readonly Dictionary<string,int[]> searchPlacements=new Dictionary<string,int[]>();
    private GameObject[] searchStitches;
    private readonly HashSet<string> foundWords=new HashSet<string>();
    private readonly HashSet<int> foundCells=new HashSet<int>();
    private TMP_Text searchProgress;
    private TMP_Text[] searchTermViews;
    private Image[] quizSteps;
    private Coroutine quizPromptAnimation;
    private int selectedCell=-1, quizCorrectSlot;

    private void EndActivity()
    {
        if(memoryFlips!=null)foreach(Coroutine flip in memoryFlips)if(flip!=null)StopCoroutine(flip);
        memoryFlips=null;
        if(activityRoot!=null){activityRoot.SetActive(false);Destroy(activityRoot);activityRoot=null;}
        firstTile=secondTile=-1;hideAt=0;tiles=null;
        memoryBoard=memoryStageOverlay=null;
        memoryInsight=quizProgress=null;
        quizSteps=null;
        if(quizPromptAnimation!=null){StopCoroutine(quizPromptAnimation);quizPromptAnimation=null;}
        if(miniGameHub!=null)miniGameHub.SetActive(true);
        if(contentOrnaments!=null)contentOrnaments.SetActive(true);
        if(pageTitle!=null)pageTitle.gameObject.SetActive(true);
        if(statusText!=null)statusText.text="Elige una actividad para practicar. Puedes volver a intentarlo cuantas veces quieras.";
    }

    private void BeginActivity(string title, string instruction, Action replay)
    {
        EndActivity(); activityComplete=false; replayActivity=replay;
        miniGameHub.SetActive(false);
        contentOrnaments.SetActive(false);
        pageTitle.gameObject.SetActive(false);
        statusText.text="Actividad: "+title+" · Mundo pausado";
        activityRoot=Panel(miniGameRoot.transform,"Actividad_"+title,new Color(.985f,.970f,.936f,1f));
        SetRect(activityRoot.GetComponent<RectTransform>(),new Vector2(.035f,.06f),new Vector2(.965f,.965f));
        AddOutline(activityRoot,ParchmentShade,1.5f);
        StartCoroutine(AnimateActivityEntrance(activityRoot.AddComponent<CanvasGroup>()));
        GameObject header=Panel(activityRoot.transform,"Encabezado_Academico",ForestDeep);
        SetRect(header.GetComponent<RectTransform>(),new Vector2(.035f,.855f),new Vector2(.965f,.965f));
        AddOutline(header,GoldDark,1.5f);
        TMP_Text overline=Text(header.transform,"DYANATR’O  /  LUDOTECA",16,TextAnchor.MiddleLeft,Gold);
        SetRect(overline.rectTransform,new Vector2(.025f,.08f),new Vector2(.34f,.92f));
        overline.fontStyle=FontStyles.Bold;
        TextBlock(header.transform,title,30,new Vector2(.53f,.5f),new Vector2(440,58),TextAnchor.MiddleCenter,Parchment).fontStyle=FontStyles.Bold;
        Button(header.transform,"VOLVER",new Vector2(.90f,.5f),new Vector2(165,48),EndActivity,TextileGrana);
        var instructionBand=Panel(activityRoot.transform,"Indicaciones",new Color(.985f,.965f,.915f,1f));
        SetRect(instructionBand.GetComponent<RectTransform>(),new Vector2(.035f,.775f),new Vector2(.965f,.845f));
        AddOutline(instructionBand,ParchmentShade,1f);
        activityStatus=TextBlock(instructionBand.transform,instruction,21,new Vector2(.5f,.5f),new Vector2(1130,48),TextAnchor.MiddleCenter,ForestDeep);
    }

    private void CompleteActivity(string name, string result)
    {
        if(activityComplete)return;
        activityComplete=true;
        var overlay=Panel(activityRoot.transform,"Minijuego_Completado",Parchment);
        SetRect(overlay.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
        AddOutline(overlay,Gold,4);
        AddTextileFrame(overlay.transform);
        TextBlock(overlay.transform,"¡EXCELENTE!\nFelicidades, completaste el minijuego",35,new Vector2(.5f,.80f),new Vector2(1100,100),TextAnchor.MiddleCenter,ForestGreen).fontStyle=FontStyles.Bold;
        Sprite sun=LoadPhoto("Ludoteca/Minigames/sol_felicidades_mazahua");
        GameObject sunObject=Panel(overlay.transform,"Sol_De_Felicidades",Color.white);
        SetRect(sunObject.GetComponent<RectTransform>(),new Vector2(.39f,.34f),new Vector2(.61f,.68f));
        Image sunImage=sunObject.GetComponent<Image>();sunImage.sprite=sun;sunImage.preserveAspect=true;sunImage.raycastTarget=false;
        if(sun==null)sunObject.SetActive(false);
        TextBlock(overlay.transform,name+" · "+result,22,new Vector2(.5f,.28f),new Vector2(1000,48),TextAnchor.MiddleCenter,GoldDark);
        Button(overlay.transform,"VOLVER A MINIJUEGOS",new Vector2(.29f,.13f),new Vector2(390,65),EndActivity,ForestGreen);
        Button(overlay.transform,"JUGAR DE NUEVO",new Vector2(.72f,.13f),new Vector2(340,65),()=>replayActivity(),ForestGreen);
        StartCoroutine(AnimateCompletion(overlay.AddComponent<CanvasGroup>(),sunObject.GetComponent<RectTransform>()));
        statusText.text="Completado: "+name;
    }

    private System.Collections.IEnumerator AnimateActivityEntrance(CanvasGroup group)
    {
        float elapsed=0f;
        while(elapsed<.22f && group!=null)
        {
            elapsed+=Time.unscaledDeltaTime;
            group.alpha=Mathf.Lerp(.65f,1f,Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/.22f)));
            yield return null;
        }
        if(group!=null)group.alpha=1f;
    }

    private System.Collections.IEnumerator AnimateCompletion(CanvasGroup group,RectTransform sun)
    {
        float elapsed=0f;
        while(elapsed<.32f && group!=null)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/.32f));
            group.alpha=Mathf.Lerp(.55f,1f,t);
            if(sun!=null)sun.localScale=Vector3.one*(t<.75f?Mathf.Lerp(.72f,1.07f,t/.75f):Mathf.Lerp(1.07f,1f,(t-.75f)/.25f));
            yield return null;
        }
        if(group!=null)group.alpha=1f;
        if(sun!=null)sun.localScale=Vector3.one;
    }

    private void StartMemory()
    {
        BeginActivity("MEMORAMA","Encuentra las parejas textiles. Voltea dos tarjetas en cada turno.",StartMemory);
        pairs=attempts=memoryStage=0;
        memoryInsight=TextBlock(activityRoot.transform,"OBSERVA LAS IMÁGENES Y ENCUENTRA CADA PAREJA",17,
            new Vector2(.5f,.74f),new Vector2(1160,34),TextAnchor.MiddleCenter,TextileBlue);
        memoryInsight.fontStyle=FontStyles.Bold;
        memoryImages=new Sprite[MemoryMotifCount];
        for(int i=0;i<memoryImages.Length;i++)memoryImages[i]=MemoryMotifSprite(i);
        BuildMemoryStage();
    }

    private Sprite MemoryMotifSprite(int index)
    {
        if(index<0 || index>=MemoryMotifCount)return null;
        string key="Ludoteca/Minigames/memorama_motivos_textiles#"+index;
        if(photoSprites.TryGetValue(key,out Sprite cached))return cached;
        Texture2D atlas=Resources.Load<Texture2D>("Ludoteca/Minigames/memorama_motivos_textiles");
        if(atlas==null)return null;
        // Square cells include a little linen at the sides, without cutting the woven border.
        float scaleX=atlas.width/1024f, scaleY=atlas.height/559f;
        int column=index%6, row=index/6;
        Rect cell=new Rect((69+151*column)*scaleX,(559-(40+160*row+145))*scaleY,145*scaleX,145*scaleY);
        Sprite sprite=Sprite.Create(atlas,cell,new Vector2(.5f,.5f),100f);
        sprite.name="Motivo_Textil_"+index;
        photoSprites[key]=sprite;
        return sprite;
    }

    private void BuildMemoryStage()
    {
        if(memoryFlips!=null)foreach(Coroutine flip in memoryFlips)if(flip!=null)StopCoroutine(flip);
        if(memoryBoard!=null){memoryBoard.SetActive(false);Destroy(memoryBoard);}
        if(memoryStageOverlay!=null){memoryStageOverlay.SetActive(false);Destroy(memoryStageOverlay);memoryStageOverlay=null;}
        firstTile=secondTile=-1;hideAt=0;
        int cardCount=MemoryPairsPerStage*2;
        memoryDeck=new int[cardCount];matched=new bool[cardCount];tiles=new Button[cardCount];memoryFlips=new Coroutine[cardCount];
        memoryBoard=new GameObject("Tablero_Memorama",typeof(RectTransform));
        memoryBoard.transform.SetParent(activityRoot.transform,false);
        SetRect(memoryBoard.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
        GameObject boardSurface=Panel(memoryBoard.transform,"Superficie_De_Juego",new Color(.953f,.931f,.879f,1f));
        SetRect(boardSurface.GetComponent<RectTransform>(),new Vector2(.19f,.05f),new Vector2(.81f,.70f));
        AddOutline(boardSurface,ParchmentShade,1.5f);
        boardSurface.GetComponent<Image>().raycastTarget=false;
        for(int i=0;i<cardCount;i++)memoryDeck[i]=memoryStage*MemoryPairsPerStage+i/2;
        for(int i=cardCount-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);int value=memoryDeck[i];memoryDeck[i]=memoryDeck[j];memoryDeck[j]=value;}
        for(int i=0;i<cardCount;i++)
        {
            int index=i;
            tiles[i]=Button(memoryBoard.transform,"",new Vector2(.305f+(i%4)*.13f,.595f-(i/4)*.225f),new Vector2(136,136),()=>PickMemory(index),Parchment);
            BuildMemoryCard(tiles[i],memoryDeck[i]);
        }
        activityStatus.text="Etapa "+(memoryStage+1)+" / "+MemoryStageCount+" · Parejas: "+pairs+" / "+MemoryMotifCount+" · Selecciona dos tarjetas.";
        memoryInsight.text=MemoryMotifCount+" MOTIVOS TEXTILES · "+MemoryStageCount+" ETAPAS · SIN LÍMITE DE TIEMPO";
    }

    private void CompleteMemoryStage()
    {
        if(pairs==MemoryMotifCount)
        {
            CompleteActivity("Memorama",MemoryMotifCount+" parejas encontradas en "+attempts+" intentos.");
            return;
        }
        memoryStageOverlay=Panel(activityRoot.transform,"Etapa_Completada",Parchment);
        SetRect(memoryStageOverlay.GetComponent<RectTransform>(),new Vector2(.22f,.27f),new Vector2(.78f,.68f));
        AddOutline(memoryStageOverlay,Gold,3f);
        TextBlock(memoryStageOverlay.transform,"¡ETAPA "+(memoryStage+1)+" COMPLETADA!",28,
            new Vector2(.5f,.76f),new Vector2(500,50),TextAnchor.MiddleCenter,ForestDeep).fontStyle=FontStyles.Bold;
        TextBlock(memoryStageOverlay.transform,"Ya descubriste "+pairs+" de los 18 motivos. Continúa para conocer más diseños.",20,
            new Vector2(.5f,.51f),new Vector2(500,72),TextAnchor.MiddleCenter,GoldDark);
        Button(memoryStageOverlay.transform,"CONTINUAR",new Vector2(.5f,.21f),new Vector2(250,52),AdvanceMemoryStage,ForestGreen);
        StartCoroutine(AnimateActivityEntrance(memoryStageOverlay.AddComponent<CanvasGroup>()));
    }

    private void AdvanceMemoryStage()
    {
        if(memoryStageOverlay==null || memoryStage>=MemoryStageCount-1 || pairs!=(memoryStage+1)*MemoryPairsPerStage)return;
        memoryStageOverlay.SetActive(false);
        memoryStage++;
        BuildMemoryStage();
    }

    private void PickMemory(int index)
    {
        if(activityComplete || secondTile>=0 || matched[index] || firstTile==index)return;
        SetMemoryFace(index,true);
        int value=memoryDeck[index];
        if(firstTile<0){firstTile=index;memoryInsight.text="OBSERVA EL MOTIVO Y BUSCA SU PAREJA";return;}
        attempts++;secondTile=index;
        if(memoryDeck[firstTile]==memoryDeck[index])
        {
            matched[firstTile]=matched[index]=true;
            tiles[firstTile].interactable=tiles[index].interactable=false;
            Color discoveredFrame=new Color(.61f,.78f,.65f,1f);
            tiles[firstTile].transform.Find("Marco_Frontal").GetComponent<Image>().color=discoveredFrame;
            tiles[index].transform.Find("Marco_Frontal").GetComponent<Image>().color=discoveredFrame;
            pairs++;firstTile=secondTile=-1;
            activityStatus.text="Etapa "+(memoryStage+1)+" / "+MemoryStageCount+" · Parejas: "+pairs+" / "+MemoryMotifCount+" · Intentos: "+attempts;
            memoryInsight.text="PAREJA DESCUBIERTA  ·  "+memoryNames[value]+"  ·  "+memoryFacts[value];
            if(pairs==(memoryStage+1)*MemoryPairsPerStage)CompleteMemoryStage();
        }
        else
        {
            activityStatus.text="No coinciden. Observa y vuelve a intentarlo.";
            memoryInsight.text="COMPARA LAS FORMAS Y LOS COLORES DE LOS DOS MOTIVOS";
            hideAt=Time.unscaledTime+1.2f;
        }
    }

    private void TickMemory()
    {
        if(secondTile<0 || activityRoot==null || !activityRoot.activeInHierarchy || !IsOpen || Time.unscaledTime<hideAt)return;
        foreach(int index in new[]{firstTile,secondTile})SetMemoryFace(index,false);
        firstTile=secondTile=-1;
        activityStatus.text="Etapa "+(memoryStage+1)+" / "+MemoryStageCount+" · Parejas: "+pairs+" / "+MemoryMotifCount+" · Selecciona dos tarjetas.";
    }

    private void GenerateSearchGrid()
    {
        Vector2Int[] directions={new Vector2Int(1,0),new Vector2Int(-1,0),new Vector2Int(0,1),new Vector2Int(0,-1),
            new Vector2Int(1,1),new Vector2Int(-1,-1),new Vector2Int(1,-1),new Vector2Int(-1,1)};
        bool complete=false;
        for(int boardAttempt=0;boardAttempt<12 && !complete;boardAttempt++)
        {
            Array.Clear(searchLetters,0,searchLetters.Length);
            searchPlacements.Clear();
            complete=true;
            foreach(string word in searchWords)
            {
                bool placed=false;
                for(int attempt=0;attempt<400 && !placed;attempt++)
                {
                    int x=UnityEngine.Random.Range(0,SearchSize),y=UnityEngine.Random.Range(0,SearchSize);
                    Vector2Int direction=directions[UnityEngine.Random.Range(0,directions.Length)];
                    int lastX=x+direction.x*(word.Length-1),lastY=y+direction.y*(word.Length-1);
                    if(lastX<0 || lastX>=SearchSize || lastY<0 || lastY>=SearchSize)continue;
                    int[] path=new int[word.Length];bool fits=true;
                    for(int letter=0;letter<word.Length;letter++)
                    {
                        int cell=(y+direction.y*letter)*SearchSize+x+direction.x*letter;
                        path[letter]=cell;
                        if(searchLetters[cell]!='\0' && searchLetters[cell]!=word[letter]){fits=false;break;}
                    }
                    if(!fits)continue;
                    for(int letter=0;letter<word.Length;letter++)searchLetters[path[letter]]=word[letter];
                    searchPlacements[word]=path;
                    placed=true;
                }
                if(!placed){complete=false;break;}
            }
        }
        if(!complete)
        {
            // Guaranteed playable fallback, still shuffled and mirrored on every run.
            Array.Clear(searchLetters,0,searchLetters.Length);searchPlacements.Clear();
            int[] rows={0,1,2,3,4,5,6,7,8,9};
            for(int i=rows.Length-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);int temp=rows[i];rows[i]=rows[j];rows[j]=temp;}
            for(int i=0;i<searchWords.Length;i++)
            {
                string word=searchWords[i];int[] path=new int[word.Length];
                int start=UnityEngine.Random.Range(0,SearchSize-word.Length+1);
                bool reverse=UnityEngine.Random.Range(0,2)==1;
                for(int letter=0;letter<word.Length;letter++)
                {
                    int column=start+(reverse?word.Length-1-letter:letter);
                    path[letter]=rows[i]*SearchSize+column;
                    searchLetters[path[letter]]=word[letter];
                }
                searchPlacements[word]=path;
            }
        }
        const string filler="AEIOUMNRSTLPBCDFGVJZ";
        for(int i=0;i<searchLetters.Length;i++)
            if(searchLetters[i]=='\0')searchLetters[i]=filler[UnityEngine.Random.Range(0,filler.Length)];
    }

    private void StartWordSearch()
    {
        BeginActivity("SOPA DE LETRAS","Selecciona la primera y la última letra de cada término; puede leerse en ambos sentidos.",StartWordSearch);
        foundWords.Clear();foundCells.Clear();selectedCell=-1;
        GenerateSearchGrid();
        tiles=new Button[SearchSize*SearchSize];searchStitches=new GameObject[tiles.Length];
        GameObject grid=Panel(activityRoot.transform,"Cuadricula_Academica",new Color(.956f,.938f,.895f,1f));
        SetRect(grid.GetComponent<RectTransform>(),new Vector2(.055f,.135f),new Vector2(.655f,.76f));
        AddOutline(grid,TextileBlue,2f);
        TextBlock(grid.transform,"TABLERO  ·  10 FILAS × 10 COLUMNAS",18,new Vector2(.5f,.95f),new Vector2(620,30),TextAnchor.MiddleCenter,GoldDark).fontStyle=FontStyles.Bold;
        for(int i=0;i<tiles.Length;i++)
        {
            int index=i;
            int column=i%SearchSize,row=i/SearchSize;
            tiles[i]=Button(grid.transform,searchLetters[i].ToString(),new Vector2(.13f+column*.082f,.82f-row*.073f),new Vector2(53,34),()=>PickLetter(index),Parchment);
            TMP_Text letter=tiles[i].GetComponentInChildren<TMP_Text>();letter.fontSize=21;letter.fontStyle=FontStyles.Bold;letter.color=ForestDeep;
            AddOutline(tiles[i].gameObject,new Color(.55f,.50f,.43f,.65f),1f);
        }
        for(int column=0;column<SearchSize;column++)TextBlock(grid.transform,(column+1).ToString(),15,new Vector2(.13f+column*.082f,.90f),new Vector2(40,24),TextAnchor.MiddleCenter,TextileBlue).fontStyle=FontStyles.Bold;
        for(int row=0;row<SearchSize;row++)TextBlock(grid.transform,((char)('A'+row)).ToString(),15,new Vector2(.055f,.82f-row*.073f),new Vector2(28,24),TextAnchor.MiddleCenter,TextileBlue).fontStyle=FontStyles.Bold;

        GameObject guide=Panel(activityRoot.transform,"Guia_Didactica",new Color(.985f,.97f,.91f,1f));
        SetRect(guide.GetComponent<RectTransform>(),new Vector2(.68f,.135f),new Vector2(.945f,.76f));
        AddOutline(guide,GoldDark,1.5f);
        TextBlock(guide.transform,"GUÍA DE BÚSQUEDA",20,new Vector2(.5f,.91f),new Vector2(290,42),TextAnchor.MiddleCenter,GoldDark).fontStyle=FontStyles.Bold;
        TextBlock(guide.transform,"Reconoce vocabulario del territorio y la cultura.",16,new Vector2(.5f,.78f),new Vector2(290,54),TextAnchor.MiddleCenter,ForestDeep);
        searchProgress=TextBlock(guide.transform,"AVANCE · 0 / "+searchWords.Length,18,new Vector2(.5f,.66f),new Vector2(280,32),TextAnchor.MiddleCenter,TextileBlue);
        searchProgress.fontStyle=FontStyles.Bold;
        searchTermViews=new TMP_Text[searchWords.Length];
        for(int word=0;word<searchWords.Length;word++)
        {
            float y=.575f-word*.062f;
            GameObject term=Panel(guide.transform,"Termino_"+searchWords[word],Parchment);
            SetRect(term.GetComponent<RectTransform>(),new Vector2(.10f,y-.025f),new Vector2(.90f,y+.025f));
            AddOutline(term,ParchmentShade,1f);
            searchTermViews[word]=Text(term.transform,"□  "+searchWords[word],16,TextAnchor.MiddleLeft,GoldDark);
            searchTermViews[word].rectTransform.offsetMin=new Vector2(12,0);
        }
        TextBlock(guide.transform,"Toca la letra inicial y después la final.",14,new Vector2(.5f,.045f),new Vector2(280,38),TextAnchor.MiddleCenter,ForestDeep);
        RefreshWordSearchPresentation();
    }

    private void PickLetter(int index)
    {
        if(activityComplete)return;
        if(selectedCell<0){selectedCell=index;RefreshWordSearchPresentation();activityStatus.text="Inicio seleccionado. Ahora elige la letra final del término.";return;}
        int start=selectedCell;selectedCell=-1;
        int dx=index%SearchSize-start%SearchSize,dy=index/SearchSize-start/SearchSize;
        var cells=new List<int>();string word="";
        if((dx!=0 || dy!=0) && (dx==0 || dy==0 || Mathf.Abs(dx)==Mathf.Abs(dy)))
        {
            int steps=Mathf.Max(Mathf.Abs(dx),Mathf.Abs(dy));
            for(int i=0;i<=steps;i++)
            {
                int cell=(start/SearchSize+Math.Sign(dy)*i)*SearchSize+start%SearchSize+Math.Sign(dx)*i;
                cells.Add(cell);word+=searchLetters[cell];
            }
        }
        char[] reverse=word.ToCharArray();Array.Reverse(reverse);
        string answer=Array.Find(searchWords,w=>w==word || w==new string(reverse));
        if(answer!=null && foundWords.Add(answer))
        {
            foreach(int cell in cells)foundCells.Add(cell);
            activityStatus.text="¡Encontraste "+answer+"! · "+foundWords.Count+" / "+searchWords.Length;
        }
        else activityStatus.text=answer!=null?"Ya encontraste esa palabra. Busca otra.":"Esa selección no corresponde a una palabra. Inténtalo otra vez.";
        RefreshWordSearchPresentation();
        if(foundWords.Count==searchWords.Length)CompleteActivity("Sopa de letras","Encontraste las "+searchWords.Length+" palabras.");
    }

    private GameObject CreateSearchStitch(int index)
    {
        GameObject stitch=new GameObject("Bordado_De_Seleccion",typeof(RectTransform));
        stitch.transform.SetParent(tiles[index].transform,false);
        SetRect(stitch.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
        foreach(float y in new[]{.10f,.90f})
        {
            GameObject thread=Panel(stitch.transform,"Hilo_Bordado",TextileGrana);
            SetRect(thread.GetComponent<RectTransform>(),new Vector2(.12f,y-.018f),new Vector2(.88f,y+.018f));
            thread.GetComponent<Image>().raycastTarget=false;
        }
        CreateDiamond(stitch.transform,new Vector2(.15f,.15f),7f,Gold,TextileBlue);
        CreateDiamond(stitch.transform,new Vector2(.85f,.15f),7f,Gold,TextileBlue);
        CreateDiamond(stitch.transform,new Vector2(.15f,.85f),7f,Gold,TextileBlue);
        CreateDiamond(stitch.transform,new Vector2(.85f,.85f),7f,Gold,TextileBlue);
        stitch.SetActive(false);
        return stitch;
    }

    private System.Collections.IEnumerator AnimateStitch(RectTransform rect)
    {
        float elapsed=0f;
        while(elapsed<.20f && rect!=null)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/.20f));
            rect.localScale=Vector3.one*(.72f+.28f*t);
            yield return null;
        }
        if(rect!=null)rect.localScale=Vector3.one;
    }

    private void RefreshWordSearchPresentation()
    {
        if(tiles==null)return;
        for(int i=0;i<tiles.Length;i++)
        {
            Color color=foundCells.Contains(i)?TextileBlue:i==selectedCell?Gold:Parchment;
            tiles[i].GetComponent<Image>().color=color;
            tiles[i].GetComponentInChildren<TMP_Text>().color=foundCells.Contains(i)?Color.white:ForestDeep;
            bool embroidered=foundCells.Contains(i) || i==selectedCell;
            if(embroidered && searchStitches[i]==null)searchStitches[i]=CreateSearchStitch(i);
            if(searchStitches[i]!=null)
            {
                bool wasVisible=searchStitches[i].activeSelf;
                searchStitches[i].SetActive(embroidered);
                if(embroidered && !wasVisible)StartCoroutine(AnimateStitch(searchStitches[i].GetComponent<RectTransform>()));
            }
        }
        if(searchProgress!=null)searchProgress.text="AVANCE · "+foundWords.Count+" / "+searchWords.Length;
        if(searchTermViews!=null)for(int i=0;i<searchTermViews.Length;i++)
        {
            bool found=foundWords.Contains(searchWords[i]);
            searchTermViews[i].text=(found?"✓  ":"□  ")+searchWords[i];
            searchTermViews[i].color=found?ForestGreen:GoldDark;
            searchTermViews[i].fontStyle=found?FontStyles.Bold:FontStyles.Normal;
        }
    }

    private void StartQuiz()
    {
        BeginActivity("QUIZ","Relaciona las palabras del vocabulario demostrativo con su significado.",StartQuiz);
        miniGameIndex=miniGameScore=0;
        GameObject questionCard=Panel(activityRoot.transform,"Ficha_De_Pregunta",new Color(.958f,.938f,.895f,1f));
        SetRect(questionCard.GetComponent<RectTransform>(),new Vector2(.15f,.135f),new Vector2(.85f,.76f));
        AddOutline(questionCard,TextileBlue,2f);
        questionCard.GetComponent<Image>().raycastTarget=false;
        TextBlock(activityRoot.transform,"OBSERVA LA PALABRA Y ELIGE SU SIGNIFICADO",17,
            new Vector2(.5f,.725f),new Vector2(920,34),TextAnchor.MiddleCenter,GoldDark).fontStyle=FontStyles.Bold;
        quizProgress=TextBlock(activityRoot.transform,"",20,new Vector2(.5f,.665f),new Vector2(900,36),TextAnchor.MiddleCenter,TextileBlue);
        quizProgress.fontStyle=FontStyles.Bold;
        quizSteps=new Image[miniGameTerms.Length];
        for(int i=0;i<quizSteps.Length;i++)
        {
            GameObject step=Panel(activityRoot.transform,"Paso_"+(i+1),ParchmentShade);
            RectTransform rect=step.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=new Vector2(.38f+i*.06f,.615f);
            rect.sizeDelta=new Vector2(55,5);
            rect.anchoredPosition=Vector2.zero;
            quizSteps[i]=step.GetComponent<Image>();
            quizSteps[i].raycastTarget=false;
        }
        miniGameText=TextBlock(activityRoot.transform,"",36,new Vector2(.5f,.545f),new Vector2(950,82),TextAnchor.MiddleCenter,GoldDark);
        miniGameAnswerButtons=new Button[3];
        for(int i=0;i<3;i++)
        {
            int slot=i;
            miniGameAnswerButtons[i]=Button(activityRoot.transform,"Respuesta",new Vector2(.5f,.435f-i*.12f),new Vector2(760,72),()=>AnswerQuiz(slot),Parchment);
            AddOutline(miniGameAnswerButtons[i].gameObject,ParchmentShade,1.5f);
            TMP_Text answer=miniGameAnswerButtons[i].GetComponentInChildren<TMP_Text>();
            answer.color=ForestDeep;
            answer.alignment=TextAlignmentOptions.MidlineLeft;
            answer.rectTransform.offsetMin=new Vector2(34f,0f);
        }
        ShowQuizQuestion();
    }

    private void ShowQuizQuestion()
    {
        string[] data=miniGameTerms[miniGameIndex].Split('|');
        quizCorrectSlot=UnityEngine.Random.Range(0,3);
        quizProgress.text="PREGUNTA "+(miniGameIndex+1)+" / "+miniGameTerms.Length+"  ·  VOCABULARIO DEMOSTRATIVO";
        miniGameText.text="¿Qué significa «"+data[0]+"»?";
        if(quizSteps!=null)for(int i=0;i<quizSteps.Length;i++)
            quizSteps[i].color=i<miniGameIndex?ForestGreen:i==miniGameIndex?TextileBlue:ParchmentShade;
        for(int i=0;i<3;i++)
        {
            miniGameAnswerButtons[i].GetComponent<Image>().color=Parchment;
            TMP_Text answer=miniGameAnswerButtons[i].GetComponentInChildren<TMP_Text>();
            answer.color=ForestDeep;
            answer.text=((char)('A'+i))+"    "+data[1+(i-quizCorrectSlot+3)%3];
        }
        if(quizPromptAnimation!=null)StopCoroutine(quizPromptAnimation);
        quizPromptAnimation=StartCoroutine(AnimateQuizPrompt(miniGameText.rectTransform));
    }

    private System.Collections.IEnumerator AnimateQuizPrompt(RectTransform prompt)
    {
        float elapsed=0f;
        while(elapsed<.22f && prompt!=null)
        {
            elapsed+=Time.unscaledDeltaTime;
            prompt.localScale=Vector3.one*Mathf.Lerp(.93f,1f,Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/.22f)));
            yield return null;
        }
        if(prompt!=null)prompt.localScale=Vector3.one;
        quizPromptAnimation=null;
    }

    private void AnswerQuiz(int slot)
    {
        if(activityComplete)return;
        if(slot!=quizCorrectSlot)
        {
            miniGameAnswerButtons[slot].GetComponent<Image>().color=TextileGrana;
            miniGameAnswerButtons[slot].GetComponentInChildren<TMP_Text>().color=Color.white;
            activityStatus.text="Todavía no. Revisa las opciones y vuelve a intentarlo.";
            StartCoroutine(AnimateWrongAnswer(miniGameAnswerButtons[slot].GetComponent<RectTransform>()));
            return;
        }
        miniGameScore++;miniGameIndex++;
        if(miniGameIndex==miniGameTerms.Length){CompleteActivity("Quiz",miniGameScore+" significados identificados correctamente.");return;}
        activityStatus.text="¡Correcto! Continúa con la siguiente palabra.";
        ShowQuizQuestion();
    }

    private System.Collections.IEnumerator AnimateWrongAnswer(RectTransform answer)
    {
        if(answer==null)yield break;
        Vector2 origin=answer.anchoredPosition;
        float elapsed=0f;
        while(elapsed<.24f && answer!=null)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/.24f);
            answer.anchoredPosition=origin+Vector2.right*(Mathf.Sin(t*Mathf.PI*6f)*(1f-t)*9f);
            yield return null;
        }
        if(answer!=null)answer.anchoredPosition=origin;
    }

    private void BuildMemoryCard(Button card,int value)
    {
        card.GetComponent<Image>().color=new Color(.98f,.96f,.91f,1f);
        ColorBlock colors=card.colors;
        colors.disabledColor=Color.white; // Matched motifs stay vivid instead of fading.
        card.colors=colors;
        TMP_Text generatedLabel=card.GetComponentInChildren<TMP_Text>();
        if(generatedLabel!=null){generatedLabel.gameObject.SetActive(false);Destroy(generatedLabel.gameObject);}
        Shadow shadow=card.gameObject.AddComponent<Shadow>();
        shadow.effectColor=new Color(.22f,.15f,.10f,.19f);
        shadow.effectDistance=new Vector2(0f,-3f);
        GameObject frame=Panel(card.transform,"Marco_Frontal",ParchmentShade);
        SetRect(frame.GetComponent<RectTransform>(),new Vector2(.055f,.055f),new Vector2(.945f,.945f));
        frame.GetComponent<Image>().raycastTarget=false;
        GameObject art=Panel(card.transform,"Imagen",Color.white);
        SetRect(art.GetComponent<RectTransform>(),new Vector2(.09f,.09f),new Vector2(.91f,.91f));
        Image image=art.GetComponent<Image>();image.sprite=memoryImages[value];image.preserveAspect=true;image.raycastTarget=false;
        if(image.sprite==null)art.SetActive(false);
        GameObject back=Panel(card.transform,"Reverso_Textil",ForestDeep);
        SetRect(back.GetComponent<RectTransform>(),new Vector2(.05f,.05f),new Vector2(.95f,.95f));
        AddOutline(back,Gold,2f);
        CreateRule(back.transform,.15f,GoldDark);
        CreateRule(back.transform,.85f,GoldDark);
        CreateDiamond(back.transform,new Vector2(.5f,.5f),43f,Parchment,TextileBlue);
        CreateDiamond(back.transform,new Vector2(.5f,.5f),19f,Gold,TextileGrana);
        foreach(Vector2 corner in new[]{new Vector2(.20f,.20f),new Vector2(.80f,.20f),new Vector2(.20f,.80f),new Vector2(.80f,.80f)})
            CreateDiamond(back.transform,corner,8f,Gold,Parchment);
        frame.SetActive(false);
        art.SetActive(false);
    }

    private void SetMemoryFace(int index,bool visible)
    {
        if(memoryFlips[index]!=null)StopCoroutine(memoryFlips[index]);
        memoryFlips[index]=StartCoroutine(AnimateMemoryFlip(index,visible));
    }

    private System.Collections.IEnumerator AnimateMemoryFlip(int index,bool visible)
    {
        RectTransform rect=tiles[index].GetComponent<RectTransform>();
        yield return AnimateGalleryWidth(rect,1f,0f,.13f);
        ApplyMemoryFace(index,visible);
        yield return AnimateGalleryWidth(rect,0f,1f,.13f);
        rect.localScale=Vector3.one;
        memoryFlips[index]=null;
    }

    private void ApplyMemoryFace(int index,bool visible)
    {
        Transform card=tiles[index].transform;
        card.Find("Reverso_Textil").gameObject.SetActive(!visible);
        card.Find("Marco_Frontal").gameObject.SetActive(visible);
        Transform art=card.Find("Imagen");
        art.gameObject.SetActive(visible && art.GetComponent<Image>().sprite!=null);
    }

    private void AddTextileFrame(Transform parent)
    {
        for(int index=0;index<12;index++)
        {
            float position=.06f+index*.08f;Color color=index%3==0?TextileGrana:index%3==1?TextileBlue:Gold;
            CreateDiamond(parent,new Vector2(position,.06f),12f,color,Parchment);
            CreateDiamond(parent,new Vector2(position,.94f),12f,color,Parchment);
        }
    }
}
