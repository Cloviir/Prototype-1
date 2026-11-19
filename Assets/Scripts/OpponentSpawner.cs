using System.Collections;
using UnityEngine;

// Wave-based spawner with difficulty scaling.
// Each wave: more opponents, slightly faster, and more medium/heavy opponents.
// Medium opponents start showing up first, then heavy ones a wave later.
// A wave ends when every opponent in it has been spawned and knocked out.
public class OpponentSpawner : MonoBehaviour
{
    public GameObject opponentPrefab;
    public Transform[] spawnPoints;
    public float firstSpawnDelay = 1f;
    [Tooltip("Seconds between individual spawns inside a wave.")]
    public float spawnInterval = 3f;
    [Tooltip("Most opponents allowed on the arena at once.")]
    public int maxActiveOpponents = 3;

    [Header("Waves")]
    [Tooltip("Pause between clearing a wave and starting the next.")]
    public float timeBetweenWaves = 3f;
    [Tooltip("Opponents in wave 1.")]
    public int baseOpponents = 3;
    [Tooltip("Extra opponents added each wave.")]
    public int opponentsPerWave = 2;

    [Header("Medium opponents")]
    [Tooltip("First wave that can have medium opponents.")]
    public int mediumStartWave = 2;
    [Tooltip("Chance (0-1) an opponent is medium, added each wave from the start wave.")]
    public float mediumChancePerWave = 0.25f;
    [Tooltip("Medium chance never goes above this.")]
    public float maxMediumChance = 0.5f;

    [Header("Heavy opponents")]
    [Tooltip("First wave that can have heavy opponents.")]
    public int heavyStartWave = 3;
    [Tooltip("Chance (0-1) an opponent is heavy, added each wave from the start wave.")]
    public float heavyChancePerWave = 0.15f;
    [Tooltip("Heavy chance never goes above this.")]
    public float maxHeavyChance = 0.4f;

    [Header("Difficulty scaling")]
    [Tooltip("Opponent speed increase per wave after wave 1 (0.1 = +10%).")]
    public float speedIncreasePerWave = 0.1f;
    [Tooltip("Max active opponents goes up by 1 every this many waves.")]
    public int wavesPerExtraActive = 2;

    IEnumerator Start()
    {
        if (opponentPrefab == null) { Debug.LogWarning("OpponentSpawner: no Opponent Prefab assigned!"); yield break; }
        if (spawnPoints.Length == 0) { Debug.LogWarning("OpponentSpawner: no spawn points assigned!"); yield break; }

        yield return new WaitForSeconds(firstSpawnDelay);

        int wave = 1;
        while (GameManager.Instance.State == GameState.Playing)
        {
            yield return RunWave(wave);
            if (GameManager.Instance.State != GameState.Playing) break;

            GameManager.Instance.WaveCleared();
            if (GameManager.Instance.State != GameState.Playing) break; // that was the final wave

            yield return new WaitForSeconds(timeBetweenWaves);
            wave++;
        }
        Debug.Log("Spawner stopped: game is no longer Playing.");
    }

    IEnumerator RunWave(int wave)
    {
        int total = baseOpponents + (wave - 1) * opponentsPerWave;
        float mediumChance = TierChance(wave, mediumStartWave, mediumChancePerWave, maxMediumChance);
        float heavyChance = TierChance(wave, heavyStartWave, heavyChancePerWave, maxHeavyChance);
        float speedMultiplier = 1f + (wave - 1) * speedIncreasePerWave;
        int maxActive = maxActiveOpponents + (wavesPerExtraActive > 0 ? (wave - 1) / wavesPerExtraActive : 0);

        GameManager.Instance.StartWave(wave, total);
        Debug.Log($"Wave {wave}: {total} opponents, medium {mediumChance:P0}, heavy {heavyChance:P0}, speed x{speedMultiplier:0.0}, max active {maxActive}");

        int spawned = 0;
        while (spawned < total && GameManager.Instance.State == GameState.Playing)
        {
            if (ActiveOpponents() < maxActive && TrySpawn(PickTier(mediumChance, heavyChance), speedMultiplier))
            {
                spawned++;
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                // Arena is full or the spawn point was blocked; check again shortly.
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Every opponent is out; wait until they've all been knocked off.
        yield return new WaitUntil(() => ActiveOpponents() == 0 || GameManager.Instance.State != GameState.Playing);
    }

    // 0 before the start wave, then grows by perWave each wave, up to max.
    float TierChance(int wave, int startWave, float perWave, float max)
    {
        if (wave < startWave) return 0f;
        return Mathf.Min(max, (wave - startWave + 1) * perWave);
    }

    // One roll: heavy first, then medium, otherwise normal.
    OpponentTier PickTier(float mediumChance, float heavyChance)
    {
        float roll = Random.value;
        if (roll < heavyChance) return OpponentTier.Heavy;
        if (roll < heavyChance + mediumChance) return OpponentTier.Medium;
        return OpponentTier.Normal;
    }

    int ActiveOpponents()
    {
        return FindObjectsByType<Opponent>(FindObjectsSortMode.None).Length;
    }

    // Returns false if the chosen spawn point is occupied, so the spawn isn't counted.
    bool TrySpawn(OpponentTier tier, float speedMultiplier)
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        foreach (Collider c in Physics.OverlapSphere(point.position, 1.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (c.GetComponentInParent<Opponent>() != null || c.CompareTag("Player"))
                return false;
        }

        GameObject obj = Instantiate(opponentPrefab, point.position, Quaternion.identity);
        obj.GetComponent<Opponent>().Setup(tier, speedMultiplier);
        return true;
    }
}