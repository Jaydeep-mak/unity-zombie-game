using UnityEngine;

public class Projectile : MonoBehaviour {
    private float speed;
    private int damage;

    public void Setup(float speed, int damage) {
        this.speed = speed;
        this.damage = damage;
    }

    private void Start() {
        // Auto-destruct after 5 seconds to prevent memory leaks if it leaves screen
        Destroy(gameObject, 5f);
    }

    private void Update() {
        // Projectile travels from LEFT to RIGHT smoothly
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // Check collision with a Zombie
        var zombieHealth = other.GetComponent<ZombieHealth>();
        if (zombieHealth != null) {
            zombieHealth.TakeDamage(damage);
            Destroy(gameObject); // Destroy projectile on hit
        }
    }
}