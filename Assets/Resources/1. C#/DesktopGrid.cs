using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DesktopGrid : MonoBehaviour
{
    public List<Slot> Slots { get; private set; } = new List<Slot>();
    void Awake(){
        Slots = GetComponentsInChildren<Slot>(includeInactive: false).ToList();
    }
}
