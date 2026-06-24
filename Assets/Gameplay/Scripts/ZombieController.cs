using UnityEngine;

public class ZombieController : MonoBehaviour {
    public float speed = 1.5f;
    
    private Vector3 originalScale;
    private float wobbleTimer = 0f;

    private Animator animator;

    private void Start() {
        originalScale = transform.localScale;
        // Offset starting timer randomly so zombies don't swing in perfect sync
        wobbleTimer = Random.Range(0f, 10f);
        animator = GetComponentInChildren<Animator>();
    }

    private void Update() {
        // Move zombie left
        transform.Translate(Vector2.left * speed * Time.deltaTime);

        // If playing Hurt animation, let it complete
        bool isHurt = false;
        if (animator != null) {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Hurt") && stateInfo.normalizedTime < 1.0f) {
                isHurt = true;
            }
        }

        if (animator != null) {
            animator.speed = speed / 1.5f; // Scale walking animation speed relative to base speed (1.5)
            if (!isHurt) animator.Play("Walk");
        } else {
            // Procedural Walk/Run Animation: wobble frequency scale with speed
            wobbleTimer += Time.deltaTime * speed * 4.5f;

            float bounce = Mathf.Sin(wobbleTimer) * 0.05f;
            float tilt = Mathf.Cos(wobbleTimer) * 5f;

            // Apply scale bounce (Y dimension) and swing tilt (Z rotation)
            transform.localScale = new Vector3(originalScale.x, originalScale.y * (1f + bounce), originalScale.z);
            transform.rotation = Quaternion.Euler(0f, 0f, tilt);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Boundary")) {
            Destroy(gameObject);
        }
    }
}