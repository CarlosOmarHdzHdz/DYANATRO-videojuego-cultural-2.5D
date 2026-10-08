using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class XunjuuFaunaNavigationValidation
{
    private static void Set(object target,string field,object value) =>
        typeof(Animal).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);

    public static async Task Run()
    {
        var root=new GameObject("QA_Navigation");
        var actors=new List<Animal>();
        var starts=new List<Vector3>();
        var goals=new List<Vector3>();
        var obstacles=new List<Collider>();
        try
        {
            int index=0;
            foreach(var entry in XunjuuFaunaCatalog.Entries)
            {
                Vector3 start=new Vector3(10000+index*20,500,10000);
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.SetParent(root.transform);floor.transform.position=start+new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(18,1,40);
                var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name="QA_SolidLog";
                obstacle.transform.SetParent(root.transform);
                obstacle.transform.position=start+new Vector3(0,.2f,1.8f);
                obstacle.transform.localScale=new Vector3(2.6f,.4f,.6f);
                obstacles.Add(obstacle.GetComponent<Collider>());
                foreach(string label in new[]{"QA_Bush","QA_Flower"})
                {
                    var soft=GameObject.CreatePrimitive(PrimitiveType.Cube);soft.name=label;
                    soft.transform.SetParent(root.transform);
                    soft.transform.position=start+new Vector3(0,.45f,label=="QA_Bush"?3.2f:4.5f);
                    soft.transform.localScale=new Vector3(8,.9f,.45f);
                }
                var go=new GameObject("QA_"+entry.Id,typeof(BoxCollider),typeof(Rigidbody),typeof(Animal));
                go.transform.SetParent(root.transform);go.transform.position=start+Vector3.up*.08f;
                go.GetComponent<Rigidbody>().position=go.transform.position;
                go.transform.localScale=Vector3.one*entry.WorldScale;
                var box=go.GetComponent<BoxCollider>();box.center=entry.ColliderCenter;box.size=entry.ColliderSize;
                var actor=go.GetComponent<Animal>();actor.ConfigureSpecies(entry.Species);actor.canFlee=false;
                actors.Add(actor);starts.Add(start);goals.Add(start+Vector3.forward*6);
                index++;
            }
            Physics.SyncTransforms();
            await Task.Delay(200);
            Time.timeScale=1;
            Physics.SyncTransforms();
            var clear=typeof(Animal).GetMethod("IsDirectionClear",BindingFlags.NonPublic|BindingFlags.Instance);
            for(int i=0;i<actors.Count;i++)
            {
                Set(actors[i],"player",null);
                typeof(Animal).GetMethod("CancelWaiting",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(actors[i],null);
                Set(actors[i],"nextRestTime",Time.time+60);
                if(actors[i].WalkSpeed>1.11f)throw new Exception("Paseo demasiado rapido");
                // Put the log inside the stationary probe range, then restore it.
                Vector3 original=obstacles[i].transform.position;
                Vector3 current=actors[i].transform.position;
                obstacles[i].transform.position=new Vector3(current.x,starts[i].y+.2f,current.z+.6f);
                Physics.SyncTransforms();
                if((bool)clear.Invoke(actors[i],new object[]{Vector3.forward}))throw new Exception("No detecta tronco bajo: "+actors[i].name+" bounds="+actors[i].GetComponent<Collider>().bounds+" obstacle="+obstacles[i].bounds);
                obstacles[i].transform.position=original;
            }
            Physics.SyncTransforms();
            float[] travel=new float[actors.Count];Vector3[] last=starts.ToArray();
            for(int step=0;step<100;step++)
            {
                for(int i=0;i<actors.Count;i++)
                {
                    Set(actors[i],"wanderTarget",goals[i]);Set(actors[i],"nextWanderRetargetTime",Time.time+10);
                    Set(actors[i],"isAlert",false);
                    Vector3 p=actors[i].transform.position;p.y=last[i].y;travel[i]+=Vector3.Distance(p,last[i]);last[i]=p;
                    if(actors[i].ActualSpeed>1.5f)throw new Exception("Velocidad de paseo fuera de rango");
                }
                await Task.Delay(100);
            }
            for(int i=0;i<actors.Count;i++)
            {
                if(travel[i]<2 || actors[i].transform.position.z<starts[i].z+2.5f)
                {
                    var a=actors[i];var b=a.GetComponent<Collider>().bounds;
                    foreach(var h in Physics.BoxCastAll(b.center,b.extents-new Vector3(.02f,.08f,.02f),Vector3.right,Quaternion.identity,.65f,~0,QueryTriggerInteraction.Ignore))
                        Debug.Log("[NAV HIT] "+h.collider.name+" bounds="+h.collider.bounds+" distance="+h.distance);
                    throw new Exception("No rodea el tronco: "+a.name+" recorrido="+travel[i]+" pos="+a.transform.position+" state="+a.State+" time="+Time.timeScale+" right="+clear.Invoke(a,new object[]{Vector3.right})+" body="+b+" target="+typeof(Animal).GetField("wanderTarget",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(a));
                }
                Debug.Log("[FAUNA NAV PASS] "+actors[i].name+" rodea tronco; recorrido="+travel[i].ToString("F2"));
            }
            var pursuers=new List<Transform>();
            for(int i=0;i<actors.Count;i++)
            {
                var a=actors[i];var pursuer=new GameObject("QA_Pursuer").transform;
                pursuer.SetParent(root.transform);pursuer.position=a.transform.position-Vector3.forward*2.5f;
                pursuers.Add(pursuer);
                Set(a,"player",pursuer);Set(a,"homePosition",a.transform.position-Vector3.forward*40);
                Set(a,"escapeUntil",0f);Set(a,"avoidanceUntil",0f);
                a.canFlee=true;
            }
            await Task.Delay(2500);
            for(int i=0;i<actors.Count;i++)
            {
                if(actors[i].transform.position.z-pursuers[i].position.z<3.2f)
                    throw new Exception("Huida vuelve hacia perseguidor fuera del territorio: "+actors[i].name);
            }
            Debug.Log("[FAUNA FLEE PASS] Seis especies se alejan del perseguidor fuera de su territorio.");
        }
        finally {UnityEngine.Object.Destroy(root);}
    }
}
