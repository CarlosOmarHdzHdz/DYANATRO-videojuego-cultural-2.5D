#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// Validation captures must finish before the inventory, camera or weapon changes state.
public static class XunjuuFrameCapture
{
    public static Task Save(MonoBehaviour runner,string path)
    {
        var done=new TaskCompletionSource<bool>();runner.StartCoroutine(Capture(path,done));return done.Task;
    }
    private static IEnumerator Capture(string path,TaskCompletionSource<bool> done)
    {
        yield return null;yield return new WaitForEndOfFrame();
        Texture2D texture=null;
        try{texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,texture.EncodeToPNG());done.SetResult(true);}
        catch(System.Exception error){done.SetException(error);}
        finally{if(texture!=null)Object.Destroy(texture);}
    }
}
#endif
