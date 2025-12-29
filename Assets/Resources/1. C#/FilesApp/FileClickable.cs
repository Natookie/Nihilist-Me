using UnityEngine;
using Nova;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(FileItem))]
public class FileClickable : MonoBehaviour
{
    FileItem file;
    UIBlock2D block;

    void Awake(){
        file = GetComponent<FileItem>();
        block = GetComponent<UIBlock2D>();
    }

    void Start(){
        if(block != null) block.AddGestureHandler<Gesture.OnPress>(OnPress);
    }

    void OnPress(Gesture.OnPress evt){
        if(file == null) return;
        if(FileOpener.Instance != null) FileOpener.Instance.Open(file);
    }
}

