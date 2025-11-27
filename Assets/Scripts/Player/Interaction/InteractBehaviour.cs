using System.Collections;
using UnityEngine;

public class InteractBehaviour : GenericBehaviour
{
    public string interactButton = "Interact";
    public float interactRange = 2f;
    public LayerMask interactableLayers = ~0;
    private int interactTrigger;
    private bool isInteracting;
    private InteractableTemplate currentTarget;

    void Start()
    {
        behaviourManager.SubscribeBehaviour(this);
        interactTrigger = Animator.StringToHash("Interact");
    }

    void Update()
    {
        LocateInteractable();

        if (!isInteracting &&
            Input.GetButtonDown(interactButton) &&
            currentTarget != null &&
            !behaviourManager.IsOverriding())
        {
            StartCoroutine(PerformInteraction(currentTarget));
        }
    }

    private void LocateInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayers, QueryTriggerInteraction.Ignore);
        InteractableTemplate closest = null;
        float closestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            InteractableTemplate candidate = hits[i].GetComponentInParent<InteractableTemplate>();
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = candidate;
            }
        }

        currentTarget = closest;
    }

    private IEnumerator PerformInteraction(InteractableTemplate target)
    {
        isInteracting = true;
        behaviourManager.LockTempBehaviour(behaviourCode);
        
        AlignTowards(target.transform.position);

        // Coba lakukan interaksi dan periksa apakah berhasil
        if (target.Interact(gameObject))
        {
            // Interaksi berhasil, mainkan animasi
            behaviourManager.GetAnim.SetFloat(speedFloat, 0f);
            behaviourManager.GetAnim.ResetTrigger(interactTrigger);
            behaviourManager.GetAnim.SetTrigger(interactTrigger);

            float wait = Mathf.Max(0f, target.interactDuration);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }
        }
        // Jika interaksi gagal, tidak ada animasi yang dimainkan dan pemain tidak terkunci lama.

        behaviourManager.UnlockTempBehaviour(behaviourCode);
        isInteracting = false;
    }

    private void AlignTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        direction.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        behaviourManager.GetRigidBody.MoveRotation(targetRotation);
        behaviourManager.SetLastDirection(direction);
    }
}