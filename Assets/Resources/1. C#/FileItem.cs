using UnityEngine;
using Nova;

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
    public FileType fileType;

    [Header("VISUAL")]
    public UIBlock2D rootBlock;
    public UIBlock2D iconBlock;
    public TextBlock nameText;
}
