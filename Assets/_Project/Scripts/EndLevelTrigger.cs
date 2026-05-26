using UnityEngine;

public class EndLevelTrigger : MonoBehaviour
{
    public GameObject alphaCompletePanel;

    private void Start()
    {
        if (alphaCompletePanel != null) alphaCompletePanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerController2D>() != null)
        {
            if (alphaCompletePanel != null) alphaCompletePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}
