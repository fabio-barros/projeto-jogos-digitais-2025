using UnityEngine;

public class WaterZone2D : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController2D controller = other.GetComponent<PlayerController2D>();
        if (controller != null)
            controller.SetSwimming(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController2D controller = other.GetComponent<PlayerController2D>();
        if (controller != null)
            controller.SetSwimming(false);
    }
}
