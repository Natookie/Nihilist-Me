using UnityEngine;

public class Slot : MonoBehaviour
{
    public DesktopIcon currentIcon;
    public bool IsEmpty => currentIcon == null;

    public void SetIcon(DesktopIcon icon){
        if(icon != null){
            if(icon.CurrentSlot != null) icon.CurrentSlot.currentIcon = null;
            icon.CurrentSlot = this;
            currentIcon = icon;

            icon.transform.SetParent(transform, false);
            (icon.transform as RectTransform).anchoredPosition = Vector2.zero;

            var group = icon.GetComponent<CanvasGroup>();
            if(group != null) group.alpha = 1f;
        }else currentIcon = null;
    }

    void LateUpdate(){
        if(currentIcon == null && transform.childCount > 0){
            var icon = transform.GetComponentInChildren<DesktopIcon>();
            if(icon != null) SetIcon(icon);
        }
    }
}
