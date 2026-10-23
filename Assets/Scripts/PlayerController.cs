using UnityEngine;

// Player movement + the one knockout interaction (a short-range shove).
// Requires: tag "Player", Rigidbody with Freeze Rotation X and Z.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveForce = 25f;

    [Header("Shove")]
    public float shoveForce = 15f;
    public float shoveRadius = 2f;
    public float shoveCooldown = 0.6f;
    [Tooltip("Small upward lift so opponents slide off instead of digging in.")]
    public float shoveLift = 0.2f;

    [Header("Shove Boost (from power-up)")]
    public Color boostedColor = new Color(1f, 0.85f, 0f); // gold

    [Header("Respawn")]
    public Transform respawnPoint; // optional; uses starting position if empty

    Rigidbody rb;
    Vector3 moveInput;
    float nextShoveTime;
    Vector3 startPosition;

    float shoveMultiplier = 1f;
    float boostEndTime;
    Renderer rend;
    Color normalColor;

    public bool IsBoosted => shoveMultiplier > 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        rend = GetComponent<Renderer>();
        normalColor = rend.material.color;
    }

    void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        if (IsBoosted && Time.time >= boostEndTime)
            EndShoveBoost();

        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextShoveTime)
        {
            Shove();
            nextShoveTime = Time.time + shoveCooldown;
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        rb.AddForce(moveInput * moveForce, ForceMode.Acceleration);
    }

    void Shove()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, shoveRadius);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponentInParent<Opponent>() == null || hit.attachedRigidbody == null) continue;

            Vector3 dir = hit.transform.position - transform.position;
            dir.y = 0f;
            dir.Normalize();

            hit.attachedRigidbody.AddForce((dir + Vector3.up * shoveLift) * shoveForce * shoveMultiplier, ForceMode.Impulse);
        }
        Debug.Log(IsBoosted ? "Boosted shove!" : "Shove!");
    }

    // Called by PowerUp.cs when the player picks one up.
    public void ActivateShoveBoost(float multiplier, float duration)
    {
        shoveMultiplier = multiplier;
        boostEndTime = Time.time + duration;
        rend.material.color = boostedColor;
        Debug.Log($"Shove boost x{multiplier} for {duration} seconds");
    }

    void EndShoveBoost()
    {
        shoveMultiplier = 1f;
        rend.material.color = normalColor;
        Debug.Log("Shove boost ended");
    }

    public void Respawn()
    {
        transform.position = respawnPoint != null ? respawnPoint.position : startPosition;
        rb.linearVelocity = Vector3.zero;   // Unity 2022 or older: rb.velocity
        rb.angularVelocity = Vector3.zero;
    }

    // Shows the remaining boost time under the main HUD.
    void OnGUI()
    {
        if (!IsBoosted || GameManager.Instance.State != GameState.Playing) return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 24 };
        style.normal.textColor = boostedColor;
        float left = Mathf.Max(0f, boostEndTime - Time.time);
        GUI.Label(new Rect(20, 125, 400, 40), $"SHOVE BOOST: {left:0.0}s", style);
    }

    // Shows the shove range in the Scene view when the player is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shoveRadius);
    }
}