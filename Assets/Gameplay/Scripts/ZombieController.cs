using UnityEngine;

public class ZombieController : MonoBehaviour {
    public float speed = 1.5f;
    
    private Vector3 originalScale;
    private float wobbleTimer = 0f;

    private ZombieHealth zombieHealth;
    private float attackTimer = 0f;
    private float attackInterval = 1f;

    private void Start() {
        originalScale = transform.localScale;
        // Offset starting timer randomly so zombies don't swing in perfect sync
        wobbleTimer = Random.Range(0f, 10f);
        zombieHealth = GetComponent<ZombieHealth>();
    }

    private void Update() {
        // Check if there is a non-trap plant in front of us (raycast left)
        PlantBase targetPlant = null;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.left, 0.45f);
        foreach (var hit in hits) {
            if (hit.collider != null && hit.collider.gameObject != gameObject) {
                var plant = hit.collider.GetComponent<PlantBase>();
                if (plant != null && !plant.IsExpiring && !(plant is TrapPlantBase)) {
                    targetPlant = plant;
                    break;
                }
            }
        }

        bool isBlocked = targetPlant != null;

        if (!isBlocked) {
            // Move zombie left
            transform.Translate(Vector2.left * speed * Time.deltaTime);
            // Procedural Walk/Run Animation: wobble frequency scale with speed
            wobbleTimer += Time.deltaTime * speed * 4.5f;
        } else {
            // Slower wiggling animation while attacking
            wobbleTimer += Time.deltaTime * 2.5f;

            // Attack the plant periodically
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval) {
                int damage = zombieHealth != null ? zombieHealth.baseDamage : 1;
                targetPlant.TakeDamage(damage);
                attackTimer = 0f;
            }
        }

        float bounce = Mathf.Sin(wobbleTimer) * 0.05f;
        float tilt = Mathf.Cos(wobbleTimer) * 5f;

        // Apply scale bounce (Y dimension) and swing tilt (Z rotation)
        transform.localScale = new Vector3(originalScale.x, originalScale.y * (1f + bounce), originalScale.z);
        transform.rotation = Quaternion.Euler(0f, 0f, tilt);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Boundary")) {
            Destroy(gameObject);
        }
    }
}