using UnityEngine;

public class GlobalManagerGroup : MonoBehaviour
{
    private static GlobalManagerGroup Instance;

    void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
