using UnityEngine;

public class PlayerInteractTPS : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // podepnij Main Camera
    public Transform player;           // podepnij Player (root)

    [Header("Interact")]
    public float interactDistance = 3f;
    public LayerMask interactLayer;    // zaznacz Interactable

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (cam == null) cam = Camera.main;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactLayer))
        {
            float dist = Vector3.Distance(player.position, hit.collider.transform.position);
            if (dist > interactDistance) return;

            var chest = hit.collider.GetComponentInParent<ChestInteraction>();
            if (chest != null)
            {
                chest.OpenChest();
            }
        }
    }
}
