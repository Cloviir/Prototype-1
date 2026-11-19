using UnityEngine;

public enum PowerUpType { ShoveBoost, Vortex }

// A pickup the player grabs by touching it.
//   ShoveBoost: makes the player's shove stronger for a short time.
//   Vortex:     pulls nearby opponents in around the player, then flings them out.
// Requires: a Collider with Is Trigger checked. No Rigidbody needed.
public class PowerUp : MonoBehaviour
{
    public PowerUpType type = PowerUpType.ShoveBoost;
    public float duration = 8f;
    public float spinSpeed = 90f;

    [Header("Shove Boost only")]
    public float shoveMultiplier = 1.3f; // "slightly stronger": 30% more force

    [Header("Vortex only")]
    [Tooltip("Tints the pickup so players can tell it apart from the shove boost.")]
    public Color vortexColor = new Color(0.6f, 0.2f, 1f); // purple

    void Start()
    {
        if (type == PowerUpType.Vortex)
        {
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = vortexColor;
        }
    }

    void Update()
    {
        // Vortex pickups spin faster so they read as "swirly".
        float speed = type == PowerUpType.Vortex ? spinSpeed * 3f : spinSpeed;
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (type == PowerUpType.Vortex)
            player.ActivateVortex(duration);
        else
            player.ActivateShoveBoost(shoveMultiplier, duration);

        Destroy(gameObject);
    }
}