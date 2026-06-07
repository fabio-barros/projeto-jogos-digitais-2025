using UnityEngine;

public class EnemyDefinitionBinding2D : MonoBehaviour
{
    public EnemyDefinition2D definition;
    public bool applyOnAwake = true;

    private void Awake()
    {
        if (applyOnAwake)
            Apply();
    }

    [ContextMenu("Apply Enemy Definition")]
    public void Apply()
    {
        if (definition != null)
            definition.ApplyTo(gameObject);
    }
}
