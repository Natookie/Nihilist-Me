using UnityEngine;
using System.IO;

public class FileOpener : MonoBehaviour
{
    public static FileOpener Instance;

    [Header("VIEWERS")]
    public TextFileViewer textViewer;
    public ImageFileViewer imageViewer;
    public AudioFileViewer audioViewer;

    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Open(FileItem file){
        if(file == null) return;

        CloseAll();

        string folder = GetFolderByType(file.fileType);
        if(string.IsNullOrEmpty(folder)){
            Debug.LogWarning($"[FileOpener] Unsupported file: {file.fileName}");
            return;
        }

        string resourceName = Path.GetFileNameWithoutExtension(file.fileName);
        string path = $"FILES APP CONTENT/{folder}/{resourceName}";

        switch(file.fileType){
            case FileType.Image: OpenImage(path, file.fileName); break;
            case FileType.Text: OpenText(path, file.fileName); break;
            case FileType.Audio: OpenAudio(path, file.fileName); break;
        }
    }

    void OpenImage(string path, string displayName){
        Sprite img = Resources.Load<Sprite>(path);
        if(img == null){
            Debug.LogWarning($"[FileOpener] Image not found: {path}");
            return;
        }
        imageViewer.Show(img, displayName);
    }

    void OpenText(string path, string displayName){
        TextAsset txt = Resources.Load<TextAsset>(path);
        if(txt == null){
            Debug.LogWarning($"[FileOpener] Text not found: {path}");
            return;
        }
        textViewer.Show(txt, displayName);
    }

    void OpenAudio(string path, string displayName){
        AudioClip clip = Resources.Load<AudioClip>(path);
        if(clip == null){
            Debug.LogWarning($"[FileOpener] Audio not found: {path}");
            return;
        }
        audioViewer.Show(clip, displayName);
    }

    string GetFolderByType(FileType type){
        switch(type){
            case FileType.Image: return "Ultra Realistic Handdrawn image";
            case FileType.Audio: return "Ultra Realistic Handmade audio";
            case FileType.Text: return "Ultra Realistic Handtyped text";
            default: return null;
        }
    }

    void CloseAll(){
        if(textViewer != null) textViewer.gameObject.SetActive(false);
        if(imageViewer != null) imageViewer.gameObject.SetActive(false);
        if(audioViewer != null) audioViewer.gameObject.SetActive(false);
    }
}
