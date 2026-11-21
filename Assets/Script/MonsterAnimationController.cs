using UnityEngine;

public class MonsterAnimationController : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void WakeUp()
    {
        animator.SetTrigger("Up");
    }

    public void SpotPlayer()
    {
        animator.SetTrigger("Spotted");
        animator.SetBool("IsRunning", true);
    }

    public void StopRunning()
    {
        animator.SetBool("IsRunning", false);
    }

    public void Despawn()
    {
        animator.SetTrigger("Despawn");
    }
}