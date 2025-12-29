using UnityEngine;
using Nova;

public class TextFileViewer : MonoBehaviour
{
    [Header("UI")]
    public AppGestureHandler root;
    public TextBlock fileNameText;
    public TextBlock contentText;

    public void Show(TextAsset textAsset, string fileName){
        if(textAsset == null) return;

        gameObject.SetActive(true);

        if(fileNameText != null) fileNameText.Text = fileName;
        if(contentText != null) contentText.Text = textAsset.text;

        root.Open();
    }
}
