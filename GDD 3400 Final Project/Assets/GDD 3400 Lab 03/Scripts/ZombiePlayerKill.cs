using UnityEngine;


public class ZombiePlayerKill : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Tell the game manager the player died
        if (SurvivalGameManager.Instance != null)
        {
            SurvivalGameManager.Instance.LoseRound();
        }
    }
}


