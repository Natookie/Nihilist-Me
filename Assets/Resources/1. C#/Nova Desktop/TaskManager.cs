using UnityEngine;
using Nova;

public class TaskManager : MonoBehaviour
{
    [Header("REFERENCES")]
    public UIBlock2D fillRAM;
    public UIBlock2D fillCPU;
    public UIBlock2D fillGPU;
    public TextBlock allValue;

    void Update(){
        fillRAM.Size.X.Percent = FakeUsage.RAM / 100f;
        fillCPU.Size.X.Percent = FakeUsage.CPU / 100f;
        fillGPU.Size.X.Percent = FakeUsage.GPU / 100f;
        
        allValue.Text = $"{(int)FakeUsage.CPU}% \n{(int)FakeUsage.RAM}%\n{(int)FakeUsage.GPU}%";
    }

    private static class FakeUsage{
        private static float cpuCurrent, ramCurrent, gpuCurrent;
        private static float cpuVel, ramVel, gpuVel;
        
        private static float cpuNoiseOffset = Random.Range(0f, 100f);
        private static float ramNoiseOffset = Random.Range(0f, 100f);
        private static float gpuNoiseOffsetX = Random.Range(0f, 100f);
        private static float gpuNoiseOffsetY = Random.Range(0f, 100f);

        private const float cpuSmooth = .5f;
        private const float ramSmooth = .8f;
        private const float gpuSmooth = .3f;
        
        private const float noiseSpeed = .1f;

        public static float CPU => GetCPU();
        public static float RAM => GetRAM();
        public static float GPU => GetGPU();

        private static float GetCPU(){
            float target = Mathf.Lerp(10f, 90f, Mathf.PerlinNoise(cpuNoiseOffset, Time.time * noiseSpeed * .15f)) + Random.Range(-1f, 1f);
            return cpuCurrent = Mathf.SmoothDamp(cpuCurrent, target, ref cpuVel, cpuSmooth);
        }
        
        private static float GetRAM(){
            float target = Mathf.Lerp(30f, 85f, Mathf.PerlinNoise(Time.time * noiseSpeed * .12f, ramNoiseOffset)) + Random.Range(-.5f, .5f);
            return ramCurrent = Mathf.SmoothDamp(ramCurrent, target, ref ramVel, ramSmooth);
        }
        
        private static float GetGPU(){
            float target = Mathf.Lerp(5f, 95f, Mathf.PerlinNoise(gpuNoiseOffsetX + Time.time * noiseSpeed * .2f, gpuNoiseOffsetY + Time.time * noiseSpeed * .3f)) + Random.Range(-2f, 2f);
            return gpuCurrent = Mathf.SmoothDamp(gpuCurrent, target, ref gpuVel, gpuSmooth);
        }
    }
}