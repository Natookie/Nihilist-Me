using UnityEngine;
using Nova;

public class ImageFileViewer : MonoBehaviour
{
    [Header("UI")]
    public AppGestureHandler root;
    public UIBlock2D imageBlock;
    public TextBlock titleText;
    public TextBlock resolutionText;
    public TextBlock sizeText;
    public TextBlock aspectText;

    private string resolution;
    private string size;
    private string aspect;

    public void Show(Sprite sprite, string fileName){
        if(sprite == null) return;

        gameObject.SetActive(true);
        imageBlock.SetImage(sprite);
        if(titleText != null) titleText.Text = fileName;

        BuildInfo(sprite);

        if(resolutionText != null) resolutionText.Text = resolution;
        if(sizeText != null) sizeText.Text = size;
        if(aspectText != null) aspectText.Text = aspect;

        root.Open();
    }

    void BuildInfo(Sprite sprite){
        Texture2D tex = sprite.texture;

        int width = tex.width;
        int height = tex.height;

        resolution = $"{width} x {height}";

        float ratio = (float)width / height;
        aspect = ratio.ToString("0.00") + " : 1";

        long bytes = (long)width * height * 4;
        size = FormatBytes(bytes);
    }

    string FormatBytes(long bytes){
        if(bytes < 1024) return bytes + " B";
        if(bytes < 1024 * 1024) return (bytes / 1024f).ToString("0.0") + " KB";
        return (bytes / (1024f * 1024f)).ToString("0.0") + " MB";
    }
}
