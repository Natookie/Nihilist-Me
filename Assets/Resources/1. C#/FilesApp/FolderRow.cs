using UnityEngine;
using Nova;

public class FolderRow : MonoBehaviour
{
    public GameObject targetPanel;
    private PanelNavigator navigator;

    void Awake(){
        navigator = GetComponentInParent<PanelNavigator>();
    }

    void Start(){
        UIBlock2D block = GetComponent<UIBlock2D>();
        if(block == null) return;

        block.AddGestureHandler<Gesture.OnPress>(OnPress);
    }

    void OnPress(Gesture.OnPress evt){
        if(navigator == null || targetPanel == null) return;

        navigator.OpenPanel(targetPanel);
        
        // ✅ Notify FilesApp via singleton
        if (FilesApp.Instance != null){
            FilesApp.Instance.OnExternalPanelOpen(targetPanel);
        }
    }
}