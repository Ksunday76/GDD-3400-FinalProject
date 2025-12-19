using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AINavigation : MonoBehaviour
{
    NavMeshAgent _agent;

    void Awake()
    {
        _agent = this.GetComponent<NavMeshAgent>();
    }

    public void SetDestination(Vector3 destination)
    {
        _agent.SetDestination(destination);
    }

    public void SetSpeed(float speed)
    {
        _agent.speed = speed;
    }

    public void Stop()
    {
        _agent.isStopped = true;
    }
}
