using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class XunjuuLudotecaBrowser
{
    private GameObject BuildLearningDashboard(Transform parent)
    {
        var root=new GameObject("Ruta_De_Aprendizaje",typeof(RectTransform));root.transform.SetParent(parent,false);
        SetRect(root.GetComponent<RectTransform>(),new Vector2(.045f,.065f),new Vector2(.955f,.81f));
        var hero=Panel(root.transform,"Bienvenida_Archivo",ForestDeep);
        SetRect(hero.GetComponent<RectTransform>(),new Vector2(0,.62f),Vector2.one);
        TextBlock(hero.transform,"TU RUTA DE APRENDIZAJE",17,new Vector2(.38f,.86f),new Vector2(850,28),TextAnchor.MiddleLeft,Gold).fontStyle=FontStyles.Bold;
        TextBlock(hero.transform,"Un territorio por descubrir",34,new Vector2(.38f,.57f),new Vector2(850,78),TextAnchor.MiddleLeft,Parchment).fontStyle=FontStyles.Bold;
        TextBlock(hero.transform,"Explora las imágenes, reconoce la naturaleza y practica\ncon las palabras y los motivos textiles del prototipo.",20,new Vector2(.38f,.22f),new Vector2(850,60),TextAnchor.MiddleLeft,ParchmentShade);
        CreateCardIcon(hero.transform,MemoryMotifSprite(4),new Vector2(.87f,.62f),new Vector2(130,130));
        TextBlock(hero.transform,"OBSERVAR · RELACIONAR\nRECORDAR",14,new Vector2(.87f,.20f),new Vector2(230,42),TextAnchor.MiddleCenter,ParchmentShade);

        var gallery=DashboardCard(root.transform,"01 / MIRAR","Galería multimedia","Imágenes y sus créditos de origen.",.18f,TextileBlue);
        var photoPlate=Panel(gallery.transform,"Vista_Galeria",Parchment);
        SetRect(photoPlate.GetComponent<RectTransform>(),new Vector2(.07f,.43f),new Vector2(.93f,.86f));
        var photo=new GameObject("Fotografia",typeof(RectTransform),typeof(Image));photo.transform.SetParent(photoPlate.transform,false);
        SetRect(photo.GetComponent<RectTransform>(),new Vector2(.03f,.16f),new Vector2(.97f,.98f));
        var image=photo.GetComponent<Image>();image.sprite=LoadPhoto("Ludoteca/Gallery/vestimenta_mazahua");image.preserveAspect=true;image.raycastTarget=false;
        TextBlock(photoPlate.transform,"Amantlali · CC BY-SA 4.0",12,new Vector2(.5f,.065f),new Vector2(310,16),TextAnchor.MiddleCenter,GoldDark);
        Button(gallery.transform,"ABRIR GALERÍA",new Vector2(.5f,.105f),new Vector2(330,46),()=>ShowPage(Page.Gallery),TextileBlue);

        var nature=DashboardCard(root.transform,"02 / DESCUBRIR","Fauna y herbolaria","Consulta las fichas que vas reuniendo.",.5f,ForestGreen);
        CreateCardIcon(nature.transform,NavigationIcon(Page.Fauna),new Vector2(.30f,.65f),new Vector2(136,146));
        CreateCardIcon(nature.transform,NavigationIcon(Page.Plants),new Vector2(.70f,.65f),new Vector2(136,146));
        Button(nature.transform,"FAUNA",new Vector2(.27f,.105f),new Vector2(158,46),()=>ShowPage(Page.Fauna),ForestGreen);
        Button(nature.transform,"PLANTAS",new Vector2(.73f,.105f),new Vector2(158,46),()=>ShowPage(Page.Plants),ForestGreen);

        var play=DashboardCard(root.transform,"03 / PRACTICAR","Aprender jugando","18 motivos · tres actividades",.82f,TextileGrana);
        CreateCardIcon(play.transform,MemoryMotifSprite(0),new Vector2(.31f,.65f),new Vector2(124,148));
        CreateCardIcon(play.transform,MemoryMotifSprite(4),new Vector2(.69f,.65f),new Vector2(124,148));
        Button(play.transform,"ELEGIR MINIJUEGO",new Vector2(.5f,.105f),new Vector2(330,46),()=>ShowPage(Page.MiniGame),TextileGrana);
        return root;
    }

    private GameObject DashboardCard(Transform parent,string step,string title,string description,float x,Color accent)
    {
        var card=Panel(parent,"Ruta_"+step,new Color(.995f,.987f,.96f,1));
        var rect=card.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(x,.29f);rect.sizeDelta=new Vector2(390,370);rect.anchoredPosition=Vector2.zero;
        AddOutline(card,ParchmentShade,1);
        var rule=Panel(card.transform,"Acento",accent);SetRect(rule.GetComponent<RectTransform>(),new Vector2(.05f,.988f),new Vector2(.95f,.996f));
        TextBlock(card.transform,step,16,new Vector2(.5f,.94f),new Vector2(350,28),TextAnchor.MiddleCenter,accent).fontStyle=FontStyles.Bold;
        TextBlock(card.transform,title,24,new Vector2(.5f,.36f),new Vector2(350,32),TextAnchor.MiddleCenter,ForestDeep).fontStyle=FontStyles.Bold;
        TextBlock(card.transform,description,17,new Vector2(.5f,.255f),new Vector2(350,34),TextAnchor.MiddleCenter,GoldDark);
        return card;
    }
}
