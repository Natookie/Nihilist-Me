using UnityEngine;

public class PanelNavigator : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;
    private GameObject currentPanel;

    void Awake(){
        if(rootPanel == null) return;
        rootPanel.SetActive(true);

        foreach (Transform child in rootPanel.transform){
            if(child.gameObject.activeSelf && currentPanel == null) currentPanel = child.gameObject;
            else child.gameObject.SetActive(false);
        }
    }

    public void OpenPanel(GameObject panel){
        if(panel == null) return;
        if(panel == currentPanel) return;

        if(!rootPanel.activeSelf) rootPanel.SetActive(true);
        if(currentPanel != null) currentPanel.SetActive(false);

        panel.SetActive(true);
        currentPanel = panel;
    }

    public void CloseCurrentPanel(GameObject child){
        currentPanel.SetActive(false);
        currentPanel = child;
    }
}
