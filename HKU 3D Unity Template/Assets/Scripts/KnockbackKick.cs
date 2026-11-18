using Mono.Cecil.Cil;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class KnockbackKick : MonoBehaviour
{
    [SerializeField]
    private EffectManager effectManager;

    [SerializeField]
    private ThirdPersonPlayerCharacter playerCharacter;

    [SerializeField]
    private BoxCollider toeCollider;

    [SerializeField]
    private float kickForce = 500f, radius;

    [SerializeField]
    private NavMeshAgent[] enemyNavMeshAgent;

    private void Update()
    {
        var state = playerCharacter.GetState();
        var isSliding = state.Stance == Stance.Slide;

        if (isSliding)
        {
            toeCollider.enabled = true;
        }
        else
        {
            toeCollider.enabled = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            effectManager.Stop(0.1f);
            KnockBack();
            StartCoroutine(NavMeshAgentHandler());
        }
    }

    private void KnockBack()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider nearBy in colliders) 
        {
            Rigidbody rb = nearBy.GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.AddExplosionForce(kickForce, transform.position, radius);
            }
        }
    }

    IEnumerator NavMeshAgentHandler()
    {
        foreach (NavMeshAgent agent in enemyNavMeshAgent)
        {
            agent.enabled = false;
        }

        yield return new WaitForSeconds(5f);

        foreach (NavMeshAgent agent in enemyNavMeshAgent)
        {
            agent.enabled = true;
        }
    }
}
