using System.Collections;
using UnityEngine;

// Anima solo la superficie; no desplaza el layout ni avanza el tiempo del mundo.
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class XunjuuPanelReveal : MonoBehaviour
{
    private CanvasGroup group;
    private Coroutine routine;
    private Vector3 rest=Vector3.one;
    private void Awake(){EnsureGroup();rest=transform.localScale;}
    private bool EnsureGroup()
    {
        // Unity distingue un componente nativo ausente de una referencia CLR null.
        // No usar ?? con UnityEngine.Object: puede conservar el wrapper "fake null".
        if(group==null)group=GetComponent<CanvasGroup>();
        if(group==null)group=gameObject.AddComponent<CanvasGroup>();
        return group!=null;
    }
    private void OnEnable(){if(!EnsureGroup())return;if(routine!=null)StopCoroutine(routine);routine=StartCoroutine(Reveal());}
    public void Replay(){if(isActiveAndEnabled)OnEnable();}
    private IEnumerator Reveal()
    {
        if(!EnsureGroup())yield break;
        if(XunjuuPresentationPreferences.ReducedMotion){group.alpha=1;transform.localScale=rest;routine=null;yield break;}
        float elapsed=0;group.alpha=0;transform.localScale=rest*.985f;
        while(elapsed<.22f)
        {
            if(group==null){routine=null;yield break;}
            if(XunjuuPresentationPreferences.ReducedMotion)break;
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.22f));
            group.alpha=t;transform.localScale=rest*Mathf.Lerp(.985f,1,t);yield return null;
        }
        if(group!=null)group.alpha=1;transform.localScale=rest;routine=null;
    }
    private void OnDisable(){if(routine!=null)StopCoroutine(routine);routine=null;transform.localScale=rest;if(group!=null)group.alpha=1;}
}
