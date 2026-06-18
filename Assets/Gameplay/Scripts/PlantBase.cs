using UnityEngine;

public abstract class PlantBase : MonoBehaviour {
    [Header("Plant Configuration")]
    [SerializeField] protected int damage = 2;
    [SerializeField] protected float attackInterval = 1.5f;
    [SerializeField] protected float attackRange = 12f;
    [SerializeField] protected float projectileSpeed = 5f;
    [SerializeField] protected GameObject projectilePrefab;

    protected float attackTimer = 0f;

    protected virtual void Start() {
        // Offset starting timer randomly slightly so plants don't fire exactly in sync if placed at same time
        attackTimer = Random.Range(0f, 0.5f);
    }

    protected virtual void Update() {
        attackTimer += Time.deltaTime;
        
        if (attackTimer >= attackInterval) {
            GameObject targetZombie = DetectZombieInLane();
            if (targetZombie != null) {
                Attack(targetZombie);
                attackTimer = 0f;
            }
        }
    }

    protected virtual GameObject DetectZombieInLane() {
        // Raycast horizontally to the right to detect any zombies in this lane
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.right, attackRange);
        foreach (var hit in hits) {
            if (hit.collider != null) {
                var zombie = hit.collider.GetComponent<ZombieController>();
                if (zombie != null) {
                    return zombie.gameObject;
                }
            }
        }
        return null;
    }

    protected abstract void Attack(GameObject target);
}