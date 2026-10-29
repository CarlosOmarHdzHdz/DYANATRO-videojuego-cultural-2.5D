using UnityEngine;

// Formas artisticas de Pinus/Quercus; no una identificacion botanica de especie.
[DisallowMultipleComponent]
public sealed class XunjuuRegionalTree : MonoBehaviour
{
    public int Form { get; private set; }
    public Renderer Visual { get; private set; }
    public bool PlacementChecked;
    public Vector3 CheckedPosition;
    public int ReservationVersion;
    public string BotanicalGroup => Form==2?"Quercus spp. / encino":"Pinus spp. / pino";
    public Bounds Bounds => Visual!=null?Visual.bounds:new Bounds(transform.position,Vector3.one);
    public float CrownRadius => Mathf.Max(Bounds.extents.x,Bounds.extents.z);
    public void Configure(int form,Renderer visual){Form=form;Visual=visual;PlacementChecked=false;}
    public static int SelectForm(Vector3 at)
        => Mathf.Abs(Mathf.FloorToInt(at.x*.13f+at.z*.37f))%10<3?2:0;
}
