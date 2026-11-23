using UnityEngine;
public class PorchAmbience : MonoBehaviour
{
    public Light porchLight;
    float baseline;
    void Start() { if (porchLight) baseline = porchLight.intensity; }
    void Update() { if (porchLight) porchLight.intensity = baseline * (0.97f + .03f * Mathf.Sin(Time.time * 3.7f)); }
}
