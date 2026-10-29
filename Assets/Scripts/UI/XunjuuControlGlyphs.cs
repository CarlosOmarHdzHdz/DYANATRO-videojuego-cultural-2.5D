using UnityEngine;
using UnityEngine.UI;

// Teclas con relieve y ratón vectorial: nítidos sin cargar atlas ni texturas nuevas.
public static class XunjuuControlGlyphs
{
    public static GameObject Add(Transform parent,string code,Vector2 position)
    {
        var root=new GameObject("Glifo_Control_"+code,typeof(RectTransform));root.transform.SetParent(parent,false);
        XunjuuInterfaceTheme.Rect(root,new Vector2(148,68),position);
        if(code=="WASD")
        {
            Key(root.transform,"W",new Vector2(0,19),new Vector2(30,28),18);
            Key(root.transform,"A",new Vector2(-34,-13),new Vector2(30,28),18);
            Key(root.transform,"S",new Vector2(0,-13),new Vector2(30,28),18);
            Key(root.transform,"D",new Vector2(34,-13),new Vector2(30,28),18);
        }
        else if(code=="CLIC IZQ.")
        {
            var mouse=new GameObject("Raton_Boton_Izquierdo",typeof(RectTransform),typeof(XunjuuMouseGlyph));mouse.transform.SetParent(root.transform,false);
            XunjuuInterfaceTheme.Rect(mouse,new Vector2(46,62),new Vector2(-23,0));mouse.GetComponent<Graphic>().raycastTarget=false;
            XunjuuInterfaceTheme.Label(root.transform,"Etiqueta_Raton","IZQ.",15,new Vector2(52,26),new Vector2(34,0),XunjuuInterfaceTheme.Paper);
        }
        else Key(root.transform,code,Vector2.zero,code=="ESPACIO"?new Vector2(132,36):code=="ESC"?new Vector2(66,42):new Vector2(44,42),code=="ESPACIO"?16:20);
        return root;
    }
    private static void Key(Transform parent,string code,Vector2 position,Vector2 size,int font)
    {
        var baseKey=XunjuuInterfaceTheme.Panel(parent,"Base_Tecla_"+code,size,position+Vector2.down*3,new Color(.02f,.045f,.038f));
        baseKey.GetComponent<XunjuuRoundedImage>().CornerRadius=4;
        var key=XunjuuInterfaceTheme.Panel(parent,"Tecla_"+code,size-new Vector2(0,4),position+Vector2.up*2,XunjuuInterfaceTheme.Paper);
        key.GetComponent<XunjuuRoundedImage>().CornerRadius=4;
        var edge=key.AddComponent<Outline>();edge.effectColor=new Color(.67f,.65f,.52f);edge.effectDistance=new Vector2(1,-1);
        XunjuuInterfaceTheme.Label(key.transform,"Etiqueta_Tecla",code,font,size-new Vector2(6,4),Vector2.zero,XunjuuInterfaceTheme.Ink).fontStyle=FontStyle.Bold;
        XunjuuInterfaceTheme.Panel(key.transform,"Luz_Tecla",new Vector2(size.x-12,1),new Vector2(0,size.y*.5f-7),Color.white);
    }
}
