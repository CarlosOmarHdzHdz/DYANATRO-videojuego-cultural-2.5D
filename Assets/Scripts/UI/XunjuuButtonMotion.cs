using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Respuesta contenida: no altera anclas, tamaños ni separación entre controles.
[DisallowMultipleComponent]
public sealed class XunjuuButtonMotion : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
{
    private Selectable button;
    private bool hovered,pressed,selected;
    private Vector3 rest;
    private Outline outline;
    private Color outlineColor;
    private bool cachedOutline;
    private void Awake(){button=GetComponent<Selectable>();rest=transform.localScale;}
    private void LateUpdate()
    {
        bool reduced=XunjuuPresentationPreferences.ReducedMotion;
        float size=!reduced&&button!=null&&button.IsInteractable()?(pressed ? .985f : hovered||selected ? 1.012f : 1f):1f;
        transform.localScale=reduced?rest:Vector3.Lerp(transform.localScale,rest*size,1-Mathf.Exp(-Time.unscaledDeltaTime*25f));
        if(!cachedOutline){outline=GetComponent<Outline>();if(outline!=null)outlineColor=outline.effectColor;cachedOutline=true;}
        if(outline!=null)outline.effectColor=selected&&button!=null&&button.IsInteractable()?XunjuuInterfaceTheme.Paper:outlineColor;
    }
    public void OnPointerEnter(PointerEventData e){hovered=true;}
    public void OnPointerExit(PointerEventData e){hovered=pressed=false;}
    public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
    public void OnPointerUp(PointerEventData e){pressed=false;}
    public void OnSelect(BaseEventData e){selected=true;}
    public void OnDeselect(BaseEventData e){selected=pressed=false;}
    private void OnDisable(){hovered=pressed=selected=false;transform.localScale=rest;if(outline!=null)outline.effectColor=outlineColor;}
}
