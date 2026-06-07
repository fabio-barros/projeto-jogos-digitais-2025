using UnityEngine;

public class PhysicsLayerSetup2D : MonoBehaviour
{
    private void Awake()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
    }
}
