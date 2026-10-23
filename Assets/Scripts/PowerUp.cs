using UnityEngine;

// A pickup that makes the player's shove stronger for a short time.
// Requires: a Collider with Is Trigger checked. No Rigidbody needed.
public class PowerUp : MonoBehaviour
{
    public float shoveMultiplier = 1.3f; // "slightly stronger": 30% more force
    public float duration = 8f;
    public float spinSpeed = 90f;

    void Update()
    {
        // Spin so it stands out as something to grab.
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (!other.CompareTag("Player")) return;

        other.GetComponent<PlayerController>().ActivateShoveBoost(shoveMultiplier, duration);
        Destroy(gameObject);
    }
}