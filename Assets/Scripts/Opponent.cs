using UnityEngine;

// The Opponent Prefab's behavior: push toward the player every physics step.
// Requires: Rigidbody with Freeze Rotation X and Z.
[RequireComponent(typeof(Rigidbody))]
public class Opponent : MonoBehaviour
{
    public float moveForce = 12f;

    [Header("Heavy version (focused variation)")]
    public float heavyScale = 1.3f;
    public Color heavyColor = new Color(0.4f, 0f, 0f); // dark red

    Rigidbody rb;
    Transform player;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        // After enough knockouts, new opponents spawn heavier, bigger, and darker
        // so they're harder to shove and the player can tell them apart.
        if (GameManager.Instance.HeavyOpponentsActive)
        {
            rb.mass *= GameManager.Instance.heavyMassMultiplier;
            transform.localScale *= heavyScale;
            GetComponent<Renderer>().material.color = heavyColor;
        }
    }

    void FixedUpdate()
    {
        if (player == null || GameManager.Instance.State != GameState.Playing) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        rb.AddForce(dir.normalized * moveForce, ForceMode.Acceleration);
    }
}