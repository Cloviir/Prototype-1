using System.Collections;
using UnityEngine;

// Places one power-up at a random spot on the arena.
// First one appears after firstSpawnDelay. After it's picked up,
// the next one appears respawnDelay seconds later.
// Each time, it randomly picks between the Power Up Prefab and any Extra Power Up Prefabs.
public class PowerUpSpawner : MonoBehaviour
{
    public GameObject powerUpPrefab;
    [Tooltip("Other power-ups to randomly choose from (e.g. VortexPowerUp).")]
    public GameObject[] extraPowerUpPrefabs;
    public float firstSpawnDelay = 5f;
    public float respawnDelay = 10f;
    [Tooltip("How far from the center it can appear. Keep it inside the arena edge (arena is 12 wide, so edge is 6).")]
    public float spawnRange = 4f;
    public float spawnHeight = 1.2f;
    [Tooltip("How much empty space the power-up needs around it so it won't spawn inside walls or obstacles.")]
    public float clearRadius = 0.6f;

    GameObject current;

    IEnumerator Start()
    {
        if (powerUpPrefab == null)
        {
            Debug.LogWarning("PowerUpSpawner: no Power Up Prefab assigned!");
            yield break;
        }

        Debug.Log($"PowerUpSpawner started. First power-up in {firstSpawnDelay} seconds.");
        yield return new WaitForSeconds(firstSpawnDelay);

        while (GameManager.Instance.State == GameState.Playing)
        {
            SpawnPowerUp();

            // Wait here until the player picks it up (it gets destroyed).
            yield return new WaitUntil(() => current == null);
            Debug.Log($"Power-up collected. Next one in {respawnDelay} seconds.");

            yield return new WaitForSeconds(respawnDelay);
        }
    }

    // Picks the base prefab or one of the extras with equal chance.
    GameObject PickPrefab()
    {
        int extras = extraPowerUpPrefabs != null ? extraPowerUpPrefabs.Length : 0;
        int index = Random.Range(0, extras + 1);
        if (index == 0) return powerUpPrefab;

        GameObject extra = extraPowerUpPrefabs[index - 1];
        return extra != null ? extra : powerUpPrefab;
    }

    void SpawnPowerUp()
    {
        GameObject prefab = PickPrefab();
        Vector3 pos = transform.position + Vector3.up * spawnHeight; // fallback: center

        // Try up to 20 random spots and use the first one that isn't
        // overlapping a wall, obstacle, opponent, or the player.
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 candidate = transform.position + new Vector3(
                Random.Range(-spawnRange, spawnRange),
                spawnHeight,
                Random.Range(-spawnRange, spawnRange));

            if (!Physics.CheckSphere(candidate, clearRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                pos = candidate;
                break;
            }
        }

        current = Instantiate(prefab, pos, Quaternion.identity);
        Debug.Log($"{prefab.name} spawned at {pos}");

        PowerUp pu = current.GetComponent<PowerUp>();
        if (pu != null && pu.type == PowerUpType.Vortex)
            GameManager.Instance.Flash("A VORTEX power-up appeared! Pull them in!");
        else
            GameManager.Instance.Flash("A power-up appeared! Grab it for a stronger shove!");
    }
}