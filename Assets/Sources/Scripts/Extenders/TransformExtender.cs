using UnityEngine;

public static class TransformExtender
{

    /// <summary>
    /// Уничтожение всех детей трансформа
    /// </summary>
    public static void DestroyAllChildren(this Transform target)
    {
        for (int i = 0; i < target.childCount; i++)
        {
            MonoBehaviour.Destroy(target.GetChild(i).gameObject);
        }
    }
    public static void DestroyAllChildren(this GameObject target)
    {
        target.transform.DestroyAllChildren();
    }
}
