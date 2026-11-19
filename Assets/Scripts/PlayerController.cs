using UnityEngine;

// Player movement + the shove, plus the two power-up effects:
//   Shove Boost: stronger shove for a while.
//   Vortex:  pulls nearby opponents in around the player, then flings them out.
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

    [Header("Vortex (from power-up)")]
    [Tooltip("How far the vortex reaches.")]
    public float vortexRadius = 6f;
    [Tooltip("Pull strength toward the player. Heavier opponents are pulled less.")]
    public float vortexPull = 30f;
    [Tooltip("Sideways force that makes opponents circle the player.")]
    public float vortexSwirl = 15f;
    [Tooltip("Opponents closer than this stop being pulled (prevents jitter).")]
    public float vortexInnerRadius = 1.2f;
    [Tooltip("Outward fling force when the vortex ends.")]
    public float vortexBurst = 22f;
    public Color vortexColor = new Color(0.6f, 0.2f, 1f); // purple

    [Header("Respawn")]
    public Transform respawnPoint; // optional; uses starting position if empty

    Rigidbody rb;
    Vector3 moveInput;
    float nextShoveTime;
    Vector3 startPosition;

    float shoveMultiplier = 1f;
    float boostEndTime;

    bool vortexActive;
    float vortexEndTime;

    Renderer rend;
    Color normalColor;

    public bool IsBoosted => shoveMultiplier > 1f;
    public bool IsVortexActive => vortexActive;

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

        if (vortexActive && Time.time >= vortexEndTime)
            EndVortex();

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

        if (vortexActive)
            PullOpponents();
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

    // ---------- Shove Boost ----------

    // Called by PowerUp.cs when the player picks up a Shove Boost.
    public void ActivateShoveBoost(float multiplier, float duration)
    {
        shoveMultiplier = multiplier;
        boostEndTime = Time.time + duration;
        UpdateColor();
        Debug.Log($"Shove boost x{multiplier} for {duration} seconds");
    }

    void EndShoveBoost()
    {
        shoveMultiplier = 1f;
        UpdateColor();
        Debug.Log("Shove boost ended");
    }

    // ---------- Vortex ----------

    // Called by PowerUp.cs when the player picks up a Vortex.
    public void ActivateVortex(float duration)
    {
        vortexActive = true;
        vortexEndTime = Time.time + duration;
        UpdateColor();
        GameManager.Instance.Flash("VORTEX! Pulling opponents in...");
        Debug.Log($"Vortex active for {duration} seconds");
    }

    // Every physics step: drag opponents toward the player and make them orbit.
    void PullOpponents()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, vortexRadius);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponentInParent<Opponent>() == null || hit.attachedRigidbody == null) continue;

            Vector3 toPlayer = transform.position - hit.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude < vortexInnerRadius) continue;

            Vector3 inward = toPlayer.normalized;
            Vector3 sideways = Vector3.Cross(Vector3.up, inward); // perpendicular = orbit

            // ForceMode.Force respects mass, so heavy opponents resist the pull.
            hit.attachedRigidbody.AddForce(inward * vortexPull + sideways * vortexSwirl, ForceMode.Force);
        }
    }

    // When time runs out: fling everyone in range outward.
    void EndVortex()
    {
        vortexActive = false;
        UpdateColor();

        Collider[] hits = Physics.OverlapSphere(transform.position, vortexRadius);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponentInParent<Opponent>() == null || hit.attachedRigidbody == null) continue;

            Vector3 dir = hit.transform.position - transform.position;
            dir.y = 0f;
            dir.Normalize();

            hit.attachedRigidbody.AddForce((dir + Vector3.up * shoveLift) * vortexBurst, ForceMode.Impulse);
        }
        GameManager.Instance.Flash("Vortex burst!");
        Debug.Log("Vortex burst!");
    }


    // Vortex color wins if both are active, then boost color, then normal.
    void UpdateColor()
    {
        if (vortexActive) rend.material.color = vortexColor;
        else if (IsBoosted) rend.material.color = boostedColor;
        else rend.material.color = normalColor;
    }

    public void Respawn()
    {
        transform.position = respawnPoint != null ? respawnPoint.position : startPosition;
        rb.linearVelocity = Vector3.zero;   // Unity 2022 or older: rb.velocity
        rb.angularVelocity = Vector3.zero;
    }

    // Shows remaining power-up time under the main HUD.
    void OnGUI()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 24 };

        if (IsBoosted)
        {
            style.normal.textColor = boostedColor;
            float left = Mathf.Max(0f, boostEndTime - Time.time);
            GUI.Label(new Rect(20, 125, 400, 40), $"SHOVE BOOST: {left:0.0}s", style);
        }

        if (vortexActive)
        {
            style.normal.textColor = vortexColor;
            float left = Mathf.Max(0f, vortexEndTime - Time.time);
            GUI.Label(new Rect(20, 160, 400, 40), $"VORTEX: {left:0.0}s", style);
        }
    }

    // Shows shove range (yellow) and vortex range (purple) in the Scene view.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shoveRadius);

        Gizmos.color = vortexColor;
        Gizmos.DrawWireSphere(transform.position, vortexRadius);
    }
}