using UnityEngine;

public enum OpponentTier { Normal, Medium, Heavy }

// The Opponent Prefab's behavior: push toward the player every physics step.
// The spawner calls Setup() right after spawning to apply the tier and wave difficulty.
// Requires: Rigidbody with Freeze Rotation X and Z.
[RequireComponent(typeof(Rigidbody))]
public class Opponent : MonoBehaviour
{
    public float moveForce = 12f;

    [Header("Medium version")]
    public float mediumMassMultiplier = 1.6f;
    public float mediumScale = 1.15f;
    public Color mediumColor = new Color(1f, 0.5f, 0f); // orange

    [Header("Heavy version")]
    public float heavyMassMultiplier = 2.5f;
    public float heavyScale = 1.3f;
    public Color heavyColor = new Color(0.4f, 0f, 0f); // dark red

    public OpponentTier Tier { get; private set; } = OpponentTier.Normal;

    Rigidbody rb;
    Transform player;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    // Tougher tiers are heavier, bigger, and a different color so they're harder
    // to shove (and resist the vortex) and the player can tell them apart.
    public void Setup(OpponentTier tier, float speedMultiplier)
    {
        moveForce *= speedMultiplier;
        Tier = tier;

        if (tier == OpponentTier.Medium)
            ApplyTier(mediumMassMultiplier, mediumScale, mediumColor);
        else if (tier == OpponentTier.Heavy)
            ApplyTier(heavyMassMultiplier, heavyScale, heavyColor);
    }

    void ApplyTier(float massMultiplier, float scale, Color color)
    {
        rb.mass *= massMultiplier;
        transform.localScale *= scale;
        GetComponent<Renderer>().material.color = color;
    }

    void FixedUpdate()
    {
        if (player == null || GameManager.Instance.State != GameState.Playing) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        rb.AddForce(dir.normalized * moveForce, ForceMode.Acceleration);
    }
}