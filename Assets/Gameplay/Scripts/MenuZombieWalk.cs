using UnityEngine;

public class MenuZombieWalk : MonoBehaviour {
    public float speed = 0.5f;
    public float walkRangeX = 3f;
    
    private Vector3 startPos;
    private bool movingLeft = true;
    private Vector3 originalScale;

    void Start() {
        startPos = transform.position;
        originalScale = transform.localScale;
    }

    void Update() {
        float direction = movingLeft ? -1f : 1f;
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        // Turn around
        if (movingLeft && transform.position.x <= startPos.x - walkRangeX) {
            movingLeft = false;
            // Face Right (flip localScale X)
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
        } else if (!movingLeft && transform.position.x >= startPos.x) {
            movingLeft = true;
            // Face Left (restore localScale X)
            transform.localScale = originalScale;
        }
    }
}
