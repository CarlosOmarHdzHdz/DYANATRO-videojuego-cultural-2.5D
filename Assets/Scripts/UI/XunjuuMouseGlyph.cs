using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class XunjuuMouseGlyph : MaskableGraphic
{
    public XunjuuMouseGlyph(){useLegacyMeshGeneration=false;}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var r=GetPixelAdjustedRect();
        Round(mesh,new Rect(r.xMin,r.yMin,r.width,r.height-3),r.width*.47f,new Color(.02f,.045f,.038f));
        var body=new Rect(r.xMin+2,r.yMin+5,r.width-4,r.height-7);Round(mesh,body,body.width*.46f,XunjuuInterfaceTheme.Paper);
        Round(mesh,new Rect(body.xMin+4,body.center.y+1,body.width*.5f-6,body.height*.36f),5,XunjuuInterfaceTheme.Gold);
        Round(mesh,new Rect(body.center.x-1,body.center.y,2,body.height*.44f),0,XunjuuInterfaceTheme.Ink);
        Round(mesh,new Rect(body.xMin+3,body.center.y,body.width-6,1.5f),0,XunjuuInterfaceTheme.Ink);
        Round(mesh,new Rect(body.center.x-2,body.center.y+8,4,9),2,XunjuuInterfaceTheme.Ink);
    }
    private static void Round(VertexHelper mesh,Rect r,float radius,Color color)
    {
        int start=mesh.currentVertCount;var v=UIVertex.simpleVert;v.color=color;v.position=r.center;v.uv0=new Vector2(.5f,.5f);mesh.AddVert(v);
        const int segments=8;int count=0;
        for(int corner=0;corner<4;corner++)for(int i=0;i<=segments;i++)
        {
            var center=new Vector2(corner<2?r.xMax-radius:r.xMin+radius,corner==0||corner==3?r.yMax-radius:r.yMin+radius);
            float a=(90-corner*90-i*90f/segments)*Mathf.Deg2Rad;v.position=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;mesh.AddVert(v);count++;
        }
        for(int i=1;i<=count;i++)mesh.AddTriangle(start,start+i,start+(i==count?1:i+1));
    }
}
