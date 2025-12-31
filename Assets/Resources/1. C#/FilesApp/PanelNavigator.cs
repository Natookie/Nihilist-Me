using UnityEngine;

public class PanelNavigator : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;
    private GameObject currentPanel;
    private GameObject lastChildPanel;

    void Awake(){
        if(rootPanel == null) return;

        rootPanel.SetActive(true);
        foreach(Transform child in rootPanel.transform){
            if(child.gameObject.activeSelf && currentPanel == null) currentPanel = child.gameObject;
        }
    }

    public void OpenPanel(GameObject panel){
        if(panel == null || panel == currentPanel) return;

        if(!rootPanel.activeSelf) rootPanel.SetActive(true);
        if(currentPanel != null){
            ClosePanel(currentPanel);
            lastChildPanel = currentPanel;
        }

        panel.SetActive(true);
        currentPanel = panel;
    }

    public void CloseCurrentPanel(GameObject targetPanel){
        if(currentPanel != null) ClosePanel(currentPanel);
        if(lastChildPanel != null) ClosePanel(lastChildPanel);
        targetPanel.SetActive(true);
        currentPanel = targetPanel;
    }

    void ClosePanel(GameObject panel){
        if(panel == null) return;
        FilesApp.Instance?.OnPanelClosed(panel);
        panel.SetActive(false);
    }
}