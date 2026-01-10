using UnityEngine;

public class ChestInteraction : MonoBehaviour
{
    private Animator animator;
    private bool isOpen = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void OpenChest()
    {
        if (isOpen) return;

        isOpen = true;
        animator.SetTrigger("Open");
    }
}
