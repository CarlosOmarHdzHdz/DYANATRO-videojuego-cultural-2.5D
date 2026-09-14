using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public static class XunjuuMotionValidation
{
    private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    private static void Check(bool ok,string message){if(!ok)throw new Exception("Motion QA: "+message);}
    public static async Task CheckOpeningAndScene(PlayerController player,DyanatroGameDirector director,XunjuuCompleteSpriteAnimator poses)
    {
        var opening=director.GetComponent<XunjuuOpeningJourney>();
        Check(poses.HasBalancedWalk,"Balanced walking sheet did not load.");
        Check(poses.HasSideWalkCorrection,"Side/back walking correction did not load.");
        var corrected=(Sprite[])typeof(XunjuuCompleteSpriteAnimator).GetField("balancedWalk",Fields).GetValue(poses);
        for(int row=0;row<8;row++)for(int frame=0;frame<4;frame++)
        {
            bool changed=row==2||row==4||row==5;
            Check(corrected[row*4+frame].texture.name==(changed?"MateoWalkSides_4x3":"MateoWalkBalanced_4x8"),"Walk correction changed an unrelated view.");
            if(changed)Check(Mathf.Approximately(corrected[row*4+frame].pixelsPerUnit,corrected[row*4].pixelsPerUnit),"Walk scale varies by frame.");
        }
        Check(opening!=null && opening.TutorialActive,"Opening tutorial did not start.");
        director.CollectMazahuaWord("qa-tutorial-blocked","No grant");
        Check(!player.HasOrbitalWeapon(),"Tutorial granted a weapon.");
        await XunjuuFrameCapture.Save(player,"output/visual-review/tutorial-after.png");
        Vector3 start=player.transform.position;var rigid=player.GetComponent<Rigidbody>();
        bool wasEnabled=player.enabled,wasKinematic=rigid.isKinematic;
        float before=poses.TravelledDistance;
        try
        {
            player.enabled=false;rigid.isKinematic=true;
            var walkFrames=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<22 && opening.TutorialStage==0;i++)
            {
                player.transform.position+=Vector3.right*.25f;await Task.Delay(60);
                if(player.GetComponent<SpriteRenderer>().sprite.name.StartsWith("Mateo_Balanced_"))
                {
                    walkFrames.Add(poses.ActiveFrame);
                    if(i==7)await XunjuuFrameCapture.Save(player,"output/visual-review/balanced-walk-after.png");
                }
            }
            Check(walkFrames.Count==4,"Walk did not visit all four phases.");
            Check(opening.TutorialStage==1,"Walking did not advance tutorial.");
            Check(poses.TravelledDistance-before>3,"Walk cycle did not follow displacement.");
            await Task.Delay(150);Check(poses.ActiveFrame==0,"Walk did not stop with displacement.");
        }
        finally{rigid.position=start;player.transform.position=start;rigid.isKinematic=wasKinematic;player.enabled=wasEnabled;Physics.SyncTransforms();}
        await Task.Delay(350);
        typeof(PlayerController).GetMethod("Jump",Fields).Invoke(player,null);
        await Task.Delay(2600);Check(opening.TutorialStage==2,"Landing did not advance tutorial.");
        typeof(PlayerController).GetMethod("Attack",Fields).Invoke(player,null);
        await Task.Delay(180);Check(opening.TutorialStage==3,"Unarmed attack did not advance tutorial.");
        var inventory=player.GetComponent<XunjuuInventoryUI>();inventory.SetVisible(true);
        await Task.Delay(180);Check(!opening.TutorialActive,"Inventory did not finish tutorial.");inventory.SetVisible(false);
        await Task.Delay(650);
        await CheckSideWalkInScene(player,poses);
        var policy=director.GetComponent<XunjuuSceneVisualPolicy>();policy.Refresh();
        foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            if(XunjuuSceneVisualPolicy.IsSoftDecoration(c.transform))Check(!c.enabled || c.isTrigger,"Solid decorative collider: "+c.name);
        int playerOrder=player.GetComponent<SpriteRenderer>().sortingOrder;
        foreach(var sprite in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            if(XunjuuSceneVisualPolicy.IsSoftDecoration(sprite.transform))Check(sprite.sortingOrder<playerOrder,"Flora sorting above player: "+sprite.name);
        var health=UnityEngine.Object.FindFirstObjectByType<BarraVidaFrames>();Check(health!=null,"Health bar missing.");
        var background=health.GetComponent<Image>();Check(background==null || background==health.imagenBarra || !background.enabled,"Health rectangle still enabled.");
        player.SetHealth(50);Check(health.imagenBarra.sprite.name=="MazahuaHealth_08","Half-health artwork mismatch.");
        await XunjuuFrameCapture.Save(player,"output/visual-review/health-50-no-background.png");
        health.EstablecerVida(0);Check(health.imagenBarra.sprite.name=="MazahuaHealth_14","Empty health artwork mismatch.");
        player.SetHealth(100);Check(health.imagenBarra.sprite.name=="MazahuaHealth_00","Full health artwork mismatch.");
        File.WriteAllText("output/visual-review/motion-checks.txt","PASS: tutorial move/jump/land/unarmed attack/inventory; tutorial blocks flower collection; displacement-driven walk and stopped idle; soft decoration trigger policy; flora behind player; health background removed.\n");
    }

    private static async Task CheckSideWalkInScene(PlayerController player,XunjuuCompleteSpriteAnimator poses)
    {
        var rigid=player.GetComponent<Rigidbody>();var renderer=player.GetComponent<SpriteRenderer>();
        var directionField=typeof(PlayerController).GetField("moveDirection",Fields);
        var animate=typeof(XunjuuCompleteSpriteAnimator).GetMethod("AnimateDirectional",Fields);
        Vector3 start=player.transform.position,savedDirection=player.GetMoveDirection();
        bool wasEnabled=player.enabled,wasKinematic=rigid.isKinematic;
        var report=new System.Text.StringBuilder();
        float savedTimeScale=Time.timeScale;
        float savedFov=Camera.main.fieldOfView;
        Vector3 savedCameraPosition=Camera.main.transform.position,cameraOffset=savedCameraPosition-start;
        Quaternion savedCameraRotation=Camera.main.transform.rotation;
        var gameDirector=UnityEngine.Object.FindFirstObjectByType<DyanatroGameDirector>();
        bool directorEnabled=gameDirector.enabled;
        float rootClearance=start.y-WalkGroundHeight(start);
        try
        {
            player.enabled=false;rigid.isKinematic=true;
            gameDirector.enabled=false;Camera.main.fieldOfView=36;
            for(int facing=0;facing<8;facing++)
            {
                float angle=facing*45*Mathf.Deg2Rad;
                Vector3 right=Camera.main.transform.right,forward=Camera.main.transform.forward;
                right.y=0;forward.y=0;
                Vector3 direction=right.normalized*-Mathf.Sin(angle)+forward.normalized*-Mathf.Cos(angle);
                directionField.SetValue(player,direction);
                var phases=new System.Collections.Generic.HashSet<int>();
                for(int step=0;step<8;step++)
                {
                    player.transform.position+=direction*(.45f*Mathf.Max(.1f,Mathf.Abs(player.transform.lossyScale.x)));
                    Vector3 position=player.transform.position;position.y=WalkGroundHeight(position)+rootClearance;player.transform.position=position;
                    rigid.position=player.transform.position;Physics.SyncTransforms();
                    Camera.main.transform.position=player.transform.position+cameraOffset;
                    Camera.main.transform.LookAt(player.GetComponent<Collider>().bounds.center);
                    animate.Invoke(poses,null);
                    Check(poses.FacingIndex==facing,"Side-walk facing mismatch.");
                    Check(renderer.sprite.name.StartsWith("Mateo_Balanced_"),"Moving actor did not use walk sprite.");
                    bool corrected=facing>=2&&facing<=6;
                    Check(renderer.sprite.texture.name==(corrected?"MateoWalkSides_4x3":"MateoWalkBalanced_4x8"),"Wrong runtime walking atlas.");
                    Check(renderer.flipX==(facing==1||facing==3||facing==6),"Walking reflection mismatch.");
                    phases.Add(poses.ActiveFrame);
                    if(corrected && step<2)
                    {
                        Time.timeScale=0;
                        await XunjuuFrameCapture.Save(player,"output/visual-review/walk-side-"+facing+"-"+step+".png");
                        Time.timeScale=savedTimeScale;
                    }
                    await Task.Delay(40);
                }
                Check(phases.Count==4,"Direction "+facing+" did not play all four walk phases.");
                await Task.Delay(150);animate.Invoke(poses,null);Check(poses.ActiveFrame==0,"Direction "+facing+" failed to return to idle.");
                report.AppendLine("PASS direction "+facing+": correct atlas, reflection, four phases, stop/idle.");
            }
            File.WriteAllText("output/visual-review/walk-sides-runtime.txt",report+DateTime.Now.ToString("O"));
        }
        finally
        {
            Time.timeScale=savedTimeScale;directionField.SetValue(player,savedDirection);
            Camera.main.fieldOfView=savedFov;
            Camera.main.transform.SetPositionAndRotation(savedCameraPosition,savedCameraRotation);gameDirector.enabled=directorEnabled;
            rigid.position=start;player.transform.position=start;rigid.isKinematic=wasKinematic;player.enabled=wasEnabled;Physics.SyncTransforms();
        }
    }

    private static float WalkGroundHeight(Vector3 p)
    {
        foreach(var terrain in Terrain.activeTerrains)
        {
            Vector3 local=p-terrain.transform.position,size=terrain.terrainData.size;
            if(local.x>=0&&local.z>=0&&local.x<=size.x&&local.z<=size.z)
                return terrain.SampleHeight(p)+terrain.transform.position.y;
        }
        return p.y;
    }

    public static async Task CheckAnimals(PlayerController player)
    {
        Vector3 playerStart=player.transform.position;
        GameObject animalObject=null,wall=null;
        try
        {
            var prefab=Resources.Load<GameObject>("Prefabs/Animals/Pato");Check(prefab!=null,"Duck prefab missing.");
            Vector3 p=playerStart+new Vector3(32,0,16);
            foreach(var terrain in Terrain.activeTerrains)
            {Vector3 local=p-terrain.transform.position,size=terrain.terrainData.size;if(local.x>=0&&local.z>=0&&local.x<=size.x&&local.z<=size.z){p.y=terrain.SampleHeight(p)+terrain.transform.position.y+.2f;break;}}
            animalObject=UnityEngine.Object.Instantiate(prefab,p,Quaternion.identity);
            var animal=animalObject.GetComponent<Animal>();Check(animal!=null,"Animal controller missing.");
            await Task.Delay(400);var start=animal.transform.position;
            Vector3 originalTerritory=animal.TerritoryCenter;
            await Task.Delay(2500);Check(Vector3.Distance(start,animal.transform.position)>.25f,"Animal froze while wandering.");
            Vector3 threatPosition=animal.transform.position+Vector3.left*2;
            player.GetComponent<Rigidbody>().position=threatPosition;player.transform.position=threatPosition;Physics.SyncTransforms();
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA_Navigation_Wall";
            wall.transform.position=animal.transform.position+new Vector3(2,1,0);wall.transform.localScale=new Vector3(.6f,2,3);
            await Task.Delay(800);Check(animal.State==Animal.BehaviourState.Flee,"Animal did not flee proximity. State="+animal.State+" distance="+Vector3.Distance(player.transform.position,animal.transform.position)+" LOS="+typeof(Animal).GetField("seesPlayer",Fields).GetValue(animal));
            start=animal.transform.position;
            await Task.Delay(2000);
            if(Vector3.Distance(start,animal.transform.position)<=.3f)
            {
                string diagnostic="state="+animal.State+" position="+animal.transform.position+" speed="+animal.ActualSpeed+" enabled="+animal.enabled+" radius="+typeof(Animal).GetField("hullRadius",Fields).GetValue(animal);
                foreach(var c in Physics.OverlapSphere(animal.transform.position+Vector3.up*.6f,2f,~0,QueryTriggerInteraction.Ignore)) diagnostic+=" nearby="+c.name+":"+c.bounds;
                File.WriteAllText("output/visual-review/animal-diagnostic.txt",diagnostic);
                Check(false,"Fleeing animal remained stuck against obstacle. "+diagnostic);
            }
            player.GetComponent<Rigidbody>().position=playerStart;player.transform.position=playerStart;Physics.SyncTransforms();
            await Task.Delay(5000);Check(animal.State!=Animal.BehaviourState.Flee,"Animal failed to release flee memory.");
            Check(Vector3.Distance(originalTerritory,animal.TerritoryCenter)<.01f,"Flee recovery moved the mission territory.");
            UnityEngine.Object.Destroy(wall);wall=null;
            for(int sample=0;sample<40;sample++)
            {
                Vector3 outward=animal.transform.position-originalTerritory;outward.y=0;
                if(outward.sqrMagnitude<.1f)outward=Vector3.right;
                Vector3 threat=animal.transform.position-outward.normalized*2f;
                player.GetComponent<Rigidbody>().position=threat;player.transform.position=threat;Physics.SyncTransforms();
                await Task.Delay(200);
                Vector3 fromTerritory=animal.transform.position-originalTerritory;fromTerritory.y=0;
                Check(fromTerritory.magnitude<animal.TerritoryRadius*1.8f,"Repeated pursuit pushed the animal beyond its territory.");
            }
            File.AppendAllText("output/visual-review/motion-checks.txt","PASS: duck wander displacement; proximity flee; escape with solid wall; recovery after threat leaves. "+DateTime.Now.ToString("O")+"\n");
            File.AppendAllText("output/visual-review/motion-checks.txt","PASS: territory anchor survives flee recovery; repeated pursuit stays within soft territory boundary.\n");
        }
        finally{player.GetComponent<Rigidbody>().position=playerStart;player.transform.position=playerStart;Physics.SyncTransforms();if(animalObject!=null)UnityEngine.Object.Destroy(animalObject);if(wall!=null)UnityEngine.Object.Destroy(wall);}
    }

    public static async Task CheckStoryProgression(PlayerController player,DyanatroGameDirector director)
    {
        var mission=UnityEngine.Object.FindFirstObjectByType<XunjuuLevel2KillMission>();
        var opening=director.GetComponent<XunjuuOpeningJourney>();
        var title=(Text)typeof(XunjuuOpeningJourney).GetField("title",Fields).GetValue(opening);
        Check(mission.CurrentPhase==XunjuuLevel2KillMission.MissionPhase.Animals && title.text.StartsWith("II"),"Animal chapter missing.");
        var animals=UnityEngine.Object.FindObjectsByType<XunjuuAnimalHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.name.Contains("_Mision2_")).ToArray();
        Check(animals.Length==6,"Story expects six mission animals.");
        foreach(var animal in animals)mission.RegisterAnimalDefeat(animal.gameObject);
        await Task.Delay(250);
        Check(mission.IsEnemyPhase && title.text.StartsWith("III") && !player.HasOrbitalAttackUnlocked(),"Enemy chapter/unlock sequence mismatch.");
        var enemies=UnityEngine.Object.FindObjectsByType<EnemyFireBreath>(FindObjectsSortMode.None).Select(e=>e.gameObject)
            .Concat(UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Select(e=>e.gameObject))
            .Where(e=>e.name.StartsWith("Enemigo_")).Distinct().ToArray();
        Check(enemies.Length==5,"Story expects five active enemies.");
        foreach(var enemy in enemies.Take(4))mission.RegisterEnemyDefeat(enemy);
        Check(!player.HasOrbitalAttackUnlocked(),"Orbital granted before fifth enemy.");
        mission.RegisterEnemyDefeat(enemies[4]);await Task.Delay(300);
        Check(mission.CurrentPhase==XunjuuLevel2KillMission.MissionPhase.Boss && title.text.StartsWith("IV") && player.HasOrbitalAttackUnlocked(),"Boss chapter/unlock missing.");
        var boss=UnityEngine.Object.FindFirstObjectByType<XunjuuBossNivel1>();Check(boss!=null,"Boss missing.");
        boss.TakeDamage(999999);await Task.Delay(400);
        Check(mission.Completed && title.text.StartsWith("V") && UnityEngine.Object.FindFirstObjectByType<XunjuuRewardPlaceholder>()!=null,"Ending or reward missing.");
        await XunjuuFrameCapture.Save(player,"output/visual-review/story-ending-after.png");
        File.AppendAllText("output/visual-review/motion-checks.txt","PASS: mission event simulation: 6 animals -> 5 enemies -> orbital unlock -> Ocelotl defeat/reward; corresponding story chapters II-V; full/half/empty health artwork.\n");
    }

    public static async Task CaptureRoof(PlayerController player,DyanatroGameDirector director)
    {
        var dressing=UnityEngine.Object.FindFirstObjectByType<XunjuuMeadowDressing>();
        var camera=Camera.main;var rigid=player.GetComponent<Rigidbody>();
        Vector3 position=player.transform.position,cameraPosition=camera.transform.position;
        Quaternion rotation=camera.transform.rotation;bool control=player.enabled,drive=director.enabled,kinematic=rigid.isKinematic;
        try
        {
            director.enabled=false;player.enabled=false;rigid.isKinematic=true;
            Bounds house=dressing.Buildings[0];Vector3 behind=house.center+Vector3.forward*(house.extents.z+2);
            foreach(var terrain in Terrain.activeTerrains)
            {Vector3 local=behind-terrain.transform.position,size=terrain.terrainData.size;if(local.x>=0&&local.z>=0&&local.x<=size.x&&local.z<=size.z){behind.y=terrain.SampleHeight(behind)+terrain.transform.position.y+1;break;}}
            rigid.position=behind;player.transform.position=behind;
            camera.transform.position=behind+new Vector3(0,10,-18);camera.transform.LookAt(behind+Vector3.up*.5f);
            Physics.SyncTransforms();await Task.Delay(200);
            await XunjuuFrameCapture.Save(player,"output/visual-review/roof-feather-after.png");
        }
        finally
        {rigid.position=position;player.transform.position=position;rigid.isKinematic=kinematic;player.enabled=control;director.enabled=drive;camera.transform.SetPositionAndRotation(cameraPosition,rotation);Physics.SyncTransforms();}
    }
}
