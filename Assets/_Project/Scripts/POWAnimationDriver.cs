using UnityEngine;

[RequireComponent(typeof(Animator))]
public class POWAnimationDriver : MonoBehaviour
{
    private static readonly int PlayerNearbyHash = Animator.StringToHash("PlayerNearby");
    private static readonly int RescuedHash = Animator.StringToHash("Rescued");
    private static readonly int RunningHash = Animator.StringToHash("Running");

    private Animator animator;
    private POWRescue powRescue;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        powRescue = GetComponent<POWRescue>();
    }

    private void Update()
    {
        if (powRescue == null) return;

        animator.SetBool(PlayerNearbyHash, powRescue.PlayerInside);
        animator.SetBool(RescuedHash, powRescue.Rescued);
        animator.SetBool(RunningHash, powRescue.Rescued);
    }
}
