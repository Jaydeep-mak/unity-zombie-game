using UnityEngine;

public class MenuBreatheAnimation : MonoBehaviour {
    public float scaleAmplitude = 0.05f;
    public float scaleSpeed = 2f;
    public float rotationAmplitude = 3f;
    public float rotationSpeed = 1.5f;

    private Vector3 originalScale;
    private float timeOffset;

    void Start() {
        originalScale = transform.localScale;
        timeOffset = Random.Range(0f, 10f);
    }

    void Update() {
        float time = Time.time * scaleSpeed + timeOffset;
        float scaleMultiplier = 1f + Mathf.Sin(time) * scaleAmplitude;
        transform.localScale = originalScale * scaleMultiplier;

        float rotTime = Time.time * rotationSpeed + timeOffset;
        float angle = Mathf.Sin(rotTime) * rotationAmplitude;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
