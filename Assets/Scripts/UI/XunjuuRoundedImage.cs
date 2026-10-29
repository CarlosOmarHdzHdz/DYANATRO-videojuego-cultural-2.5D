using UnityEngine;
using UnityEngine.UI;

// Geometria nativa a cualquier resolucion: sin fondos rasterizados ampliados.
public sealed class XunjuuRoundedImage : Image
{
    [SerializeField] private float cornerRadius=10f;
    public float CornerRadius {get=>cornerRadius;set{cornerRadius=Mathf.Max(0,value);SetVerticesDirty();}}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        if(sprite!=null||type!=Type.Simple){base.OnPopulateMesh(vh);return;}
        vh.Clear();Rect rect=GetPixelAdjustedRect();float radius=Mathf.Min(cornerRadius,Mathf.Min(rect.width,rect.height)*.5f);
        if(Mathf.Min(rect.width,rect.height)<14f)radius=0;
        var vertex=UIVertex.simpleVert;vertex.color=color;vertex.position=rect.center;vertex.uv0=new Vector2(.5f,.5f);vh.AddVert(vertex);
        const int segments=8;int count=0;
        for(int corner=0;corner<4;corner++)
        {
            Vector2 center=new Vector2(corner<2?rect.xMax-radius:rect.xMin+radius,corner==0||corner==3?rect.yMax-radius:rect.yMin+radius);
            for(int i=0;i<=segments;i++)
            {
                float angle=(90f-corner*90f-i*90f/segments)*Mathf.Deg2Rad;
                Vector2 p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                vertex.position=p;vertex.uv0=new Vector2((p.x-rect.xMin)/Mathf.Max(1,rect.width),(p.y-rect.yMin)/Mathf.Max(1,rect.height));vh.AddVert(vertex);count++;
            }
        }
        for(int i=1;i<=count;i++)vh.AddTriangle(0,i,i==count?1:i+1);
    }
}
