using System.Collections.Generic;
using UnityEngine;

public static class XunjuuPresentationSprites
{
    private static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
    public static Sprite Load(string path, float height, Vector2 pivot)
    {
        if(cache.TryGetValue(path,out var sprite) && sprite!=null)return sprite;
        var texture=Resources.Load<Texture2D>(path);
        if(texture==null){var imported=Resources.Load<Sprite>(path);if(imported!=null)texture=imported.texture;}
        if(texture==null || !texture.isReadable)return null;
        var pixels=texture.GetPixels32();
        int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
        {
            if(pixels[y*texture.width+x].a<48)continue;
            minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);
        }
        if(maxX<minX)return null;
        var rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
        sprite=Sprite.Create(texture,rect,pivot,rect.height/height,0,SpriteMeshType.FullRect);
        sprite.name=path.Substring(path.LastIndexOf('/')+1);cache[path]=sprite;return sprite;
    }
}
