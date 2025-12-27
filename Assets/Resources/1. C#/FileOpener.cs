using UnityEngine;

public class FileOpener : MonoBehaviour
{
    [Header("VIEWER PANELS")]
    public GameObject textViewer;
    public GameObject imageViewer;
    public GameObject audioViewer;

    public static FileOpener Instance;

    void Awake() => Instance = this;

    public void Open(FileItem file){
        switch(file.fileType){
            case FileType.Text:
                OpenText(file);
                break;

            case FileType.Image:
                OpenImage(file);
                break;

            case FileType.Audio:
                OpenAudio(file);
                break;
        }
    }

    void CloseAll(){
        if(textViewer != null) textViewer.SetActive(false);
        if(imageViewer != null) imageViewer.SetActive(false);
        if(audioViewer != null) audioViewer.SetActive(false);
    }

    void OpenText(FileItem file){
        if(textViewer == null) return;

        textViewer.SetActive(true);
        Debug.Log("Opening TEXT file: " + file.fileName);
    }

    void OpenImage(FileItem file){
        if(imageViewer == null) return;

        imageViewer.SetActive(true);
        Debug.Log("Opening IMAGE file: " + file.fileName);
    }

    void OpenAudio(FileItem file){
        if(audioViewer == null) return;

        audioViewer.SetActive(true);
        Debug.Log("Opening AUDIO file: " + file.fileName);
    }
}
