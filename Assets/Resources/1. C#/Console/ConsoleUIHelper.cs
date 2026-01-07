using Nova;
using UnityEngine;
using System.Collections;

public class ConsoleUIHelper : MonoBehaviour
{
    [Header("UI REFERENCES")]
    [SerializeField] private GameObject headerPrefab;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private GameObject inputPrefab;
    [SerializeField] private Transform textRoot;
    [SerializeField] private UIBlock rootUIBlock;
    [SerializeField] private Color defaultColor = Color.white;

    private TextBlock activeInputText;
    
    public void Initialize(Transform contentRoot, UIBlock rootBlock){
        textRoot = contentRoot;
        rootUIBlock = rootBlock;
    }
    
    public TextBlock AddText(string message, Color? color = null, bool isHeader = false){
        if(string.IsNullOrEmpty(message) || textRoot == null) {
            Debug.LogWarning("Missing textRoot or message");
            return null;
        }
        
        GameObject prefab = (isHeader) ? headerPrefab : messagePrefab;
        if(prefab == null){
            Debug.LogError("Missing prefab");
            return null;
        }
        
        GameObject instance = Instantiate(prefab, textRoot);
        TextBlock textBlock = instance.GetComponentInChildren<TextBlock>();
        
        if(textBlock != null){
            textBlock.Text = message;
            textBlock.Color = color ?? defaultColor;
        }
        
        return textBlock;
    }
    
    public TextBlock CreateInputLine(){
        if(inputPrefab == null || textRoot == null) return null;
        
        GameObject instance = Instantiate(inputPrefab, textRoot);
        TextBlock[] texts = instance.GetComponentsInChildren<TextBlock>();
        
        foreach(var text in texts){
            if(text.name.ToLower().Contains("input")){
                activeInputText = text;
                break;
            }
        }
        
        ScrollToTop();
        return activeInputText;
    }
    
    public void ClearConsole(){
        if(textRoot == null) return;
        
        foreach(Transform child in textRoot) Destroy(child.gameObject);
        activeInputText = null;
    }
    
    public void DestroyInputBox(){
        if(activeInputText == null) return;
        
        Transform parent = activeInputText.transform.parent;
        if(parent != null) Destroy(parent.gameObject);
        activeInputText = null;
    }
    
    public void ScrollToTop(){
        if(rootUIBlock == null) return;
        StartCoroutine(ScrollToTopDelayed());
    }
    
    IEnumerator ScrollToTopDelayed(){
        yield return new WaitForEndOfFrame();
        
        var scroller = rootUIBlock.GetComponentInChildren<Scroller>();
        if(scroller != null) scroller.ScrollToIndex(0, true);
    }
    
    public void UpdateInputText(string text){
        if(activeInputText != null) activeInputText.Text = text;
    }
    
    public void FlashInputText(Color flashColor, float duration = .4f){
        if(activeInputText == null) return;
        StartCoroutine(FlashInputCoroutine(flashColor, duration));
    }
    
    IEnumerator FlashInputCoroutine(Color flashColor, float duration){
        Color originalColor = activeInputText.Color;
        activeInputText.Color = flashColor;
        
        yield return new WaitForSeconds(duration);
        
        if(activeInputText != null) activeInputText.Color = originalColor;
    }
    
    public bool HasActiveInput() => activeInputText != null;
    public TextBlock GetActiveInputText() => activeInputText;
}
