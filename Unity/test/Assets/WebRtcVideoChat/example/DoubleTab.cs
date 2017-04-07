using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class DoubleTab : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent onDoubleTab;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.clickCount == 2)
        {
            if(onDoubleTab != null)
            {
                onDoubleTab.Invoke();
            }
            Debug.Log("double tab");
        }
    }
}
