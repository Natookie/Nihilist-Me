using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private AudioChannel audioChannel;

    private void Start()
    {
        Assert.IsNotNull(slider, "slider is missing");
    }

    public void UpdateAudioVolume()
    {
        AudioManager.Instance.SetVolume(audioChannel, slider.value);
    }
}
