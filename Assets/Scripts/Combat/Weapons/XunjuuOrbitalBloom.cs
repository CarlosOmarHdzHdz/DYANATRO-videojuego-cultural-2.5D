using UnityEngine;

// A fantasy embroidered flower accompanies the existing special attack's real damage point.
[DisallowMultipleComponent]
public sealed class XunjuuOrbitalBloom : MonoBehaviour
{
    private OrbitalWeapon weapon;
    private GameObject flower;
    private TrailRenderer trail;
    private Material trailMaterial;
    private bool wasOrbiting;
    private void Awake()
    {
        weapon=GetComponent<OrbitalWeapon>();
        flower=XunjuuArtMeshes.Create("BordadoOrbital",transform,"Flor_Bordada_Orbital");
        flower.SetActive(false);
        trail=flower.AddComponent<TrailRenderer>();
        trail.time=.16f;trail.minVertexDistance=.06f;trail.widthMultiplier=.13f;
        trail.startColor=new Color(1,.66f,.26f,.85f);trail.endColor=new Color(.75f,.12f,.28f,0);
        trail.numCornerVertices=3;trail.emitting=false;
        trailMaterial=new Material(Shader.Find("Sprites/Default"));trail.sharedMaterial=trailMaterial;
    }
    public void Refresh(bool orbiting,Transform player,float angle)
    {
        if(flower==null)return;
        if(orbiting && player!=null)
        {
            flower.SetActive(true);
            // Compensate for the equipped weapon and mirrored player scale.
            Vector3 scale=transform.lossyScale;
            flower.transform.localScale=new Vector3(.95f/Mathf.Max(.001f,Mathf.Abs(scale.x)),.95f/Mathf.Max(.001f,Mathf.Abs(scale.y)),.95f/Mathf.Max(.001f,Mathf.Abs(scale.z)));
            Camera camera=Camera.main;
            flower.transform.rotation=(camera!=null?camera.transform.rotation:Quaternion.identity)*Quaternion.Euler(0,0,-angle*1.4f);
            flower.transform.position=transform.position;
            if(!wasOrbiting)trail.Clear();trail.emitting=true;
            trail.sortingOrder=500;
        }
        else {trail.emitting=false;trail.Clear();flower.SetActive(false);}
        wasOrbiting=orbiting;
    }
    private void OnDisable(){if(trail!=null){trail.emitting=false;trail.Clear();}if(flower!=null)flower.SetActive(false);wasOrbiting=false;}
    private void OnDestroy(){if(trailMaterial!=null)Destroy(trailMaterial);}
}
