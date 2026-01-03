using UnityEngine;

public interface IMovable
{
    void Move(float horizontalInput);
    bool IsMoving { get; }
}