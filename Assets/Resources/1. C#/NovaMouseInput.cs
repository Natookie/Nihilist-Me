using Nova;
using UnityEngine;
using UnityEngine.InputSystem;

public class NovaMouseInput : MonoBehaviour
{
    public const uint MousePointerControlID = 1;
    public const uint ScrollWhellControlID = 2;

    void Update(){
        if(Mouse.current == null) return;

        Vector2 position = Mouse.current.position.ReadValue();
        Ray mouseRay = Camera.main.ScreenPointToRay(position);
        bool pressed = Mouse.current.leftButton.isPressed;

        Interaction.Update pointUpdate = new Interaction.Update(mouseRay, MousePointerControlID);
        Interaction.Point(pointUpdate, pressed);
    }

}
