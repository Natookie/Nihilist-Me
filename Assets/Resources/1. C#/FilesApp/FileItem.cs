using UnityEngine;
using Nova;
using System.IO;

public enum FileType
{
    Text,
    Image,
    Audio,
    Unknown
}

public class FileItem : MonoBehaviour
{
    [Header("FILE DATA")]
    public string fileName;
    public string resourceName;
    public string folderName;
    public FileType fileType;

    [Header("VISUAL")]
    public UIBlock2D rootBlock;
    public UIBlock2D iconBlock;
    public TextBlock nameText;

    void Awake(){
        FillVisual();
        FillData();
    }

    void FillVisual(){
        rootBlock = GetComponent<UIBlock2D>();
        if(rootBlock == null){
            Debug.LogWarning($"[FileItem] No UIBlock2D on {name}", this);
            return;
        }

        if(transform.childCount < 2){
            Debug.LogWarning($"[FileItem] Invalid child structure on {name}", this);
            return;
        }

        iconBlock = transform.GetChild(0).GetComponent<UIBlock2D>();
        nameText  = transform.GetChild(1).GetComponent<TextBlock>();

        if(iconBlock == null || nameText == null){
            Debug.LogWarning($"[FileItem] Missing Icon or Name on {name}", this);
            return;
        }
    }

    void FillData(){
        if(nameText == null) return;

        fileName     = nameText.Text;
        resourceName = Path.GetFileNameWithoutExtension(fileName);
        fileType     = DetectFileType(fileName);

        folderName = transform.parent != null
            ? transform.parent.name
            : string.Empty;
    }

    FileType DetectFileType(string file){
        if(string.IsNullOrEmpty(file)) return FileType.Unknown;

        string ext = Path.GetExtension(file).ToLowerInvariant();

        switch(ext){
            case ".txt":  return FileType.Text;
            case ".png":
            case ".jpg":
            case ".jpeg": return FileType.Image;
            case ".mp3":
            case ".wav":
            case ".ogg":  return FileType.Audio;
            default:      return FileType.Unknown;
        }
    }
}
