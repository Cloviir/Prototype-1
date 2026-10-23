using UnityEngine;

// Opponent falls in -> knockout +1 and opponent removed.
// Player falls in  -> lose a life and respawn.
// Requires: BoxCollider with Is Trigger checked.
public class KnockoutZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        Opponent opponent = other.GetComponentInParent<Opponent>();
        if (opponent != null)
        {
            GameManager.Instance.AddKnockout();
            Destroy(opponent.gameObject);
            return;
        }

        if (other.CompareTag("Player"))
        {
            GameManager.Instance.LoseLife();
            other.GetComponent<PlayerController>().Respawn();
        }
    }
}