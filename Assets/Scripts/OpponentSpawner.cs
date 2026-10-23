using System.Collections;
using UnityEngine;

// Activates opponents over time at random spawn points while the game is Playing.
public class OpponentSpawner : MonoBehaviour
{
    public GameObject opponentPrefab;
    public Transform[] spawnPoints;
    public float firstSpawnDelay = 1f;
    public float spawnInterval = 3f;
    public int maxActiveOpponents = 3;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (GameManager.Instance.State == GameState.Playing)
        {
            int active = FindObjectsByType<Opponent>(FindObjectsSortMode.None).Length;
            if (active < maxActiveOpponents)
                Spawn();

            yield return new WaitForSeconds(spawnInterval);
        }
        Debug.Log("Spawner stopped: game is no longer Playing.");
    }

    void Spawn()
    {
        if (spawnPoints.Length == 0) { Debug.LogWarning("No spawn points assigned!"); return; }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Skip this spawn if an opponent or the player is already standing there.
        if (Physics.CheckSphere(point.position, 1.2f, ~0, QueryTriggerInteraction.Ignore)
            && Physics.OverlapSphere(point.position, 1.2f).Length > 0)
        {
            foreach (Collider c in Physics.OverlapSphere(point.position, 1.2f))
            {
                if (c.GetComponentInParent<Opponent>() != null || c.CompareTag("Player"))
                    return;
            }
        }

        Instantiate(opponentPrefab, point.position, Quaternion.identity);
    }
}