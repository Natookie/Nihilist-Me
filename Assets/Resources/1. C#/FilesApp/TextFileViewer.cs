using UnityEngine;
using Nova;
using System.Text;

public class TextFileViewer : MonoBehaviour
{
    [Header("UI")]
    public AppGestureHandler root;
    public TextBlock titleText;
    public TextBlock fileNameText;
    public TextBlock contentText;

    [Header("META")]
    public TextBlock lineText;
    public TextBlock wordText;
    public TextBlock sizeText;

    int lineCount;
    int wordCount;
    string size;

    public void Show(TextAsset textAsset, string fileName){
        if(textAsset == null) return;

        gameObject.SetActive(true);

        if(fileNameText != null) fileNameText.Text = fileName;
        if(contentText != null) contentText.Text = textAsset.text;
        if(titleText != null) titleText.Text = fileName;

        BuildMeta(textAsset);

        if(lineText != null) lineText.Text = lineCount + " lines";
        if(wordText != null) wordText.Text = wordCount + " words";
        if(sizeText != null) sizeText.Text = size;

        root.Open();
    }

    void BuildMeta(TextAsset txt){
        lineCount = txt.text.Split('\n').Length;

        wordCount = txt.text.Split(
            new[] { ' ', '\n', '\t' },
            System.StringSplitOptions.RemoveEmptyEntries
        ).Length;

        int bytes = Encoding.UTF8.GetByteCount(txt.text);
        size = FormatBytes(bytes);
    }

    string FormatBytes(int bytes){
        if(bytes < 1024) return bytes + " B";
        if(bytes < 1024 * 1024) return (bytes / 1024f).ToString("0.0") + " KB";
        return (bytes / (1024f * 1024f)).ToString("0.0") + " MB";
    }
}
