using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SomeComponent : MonoBehaviour
{
    private void Start()
    {
        Hello();
        var result = Sum(new List<int>
        {
            1, 2, 3, 4, 5
        });
        Debug.Log($"Sum={result}");
    }

    private static void Hello()
    {
        Debug.Log("Hello");
    }

    private static int Sum(List<int> v)
    {
        return v.Sum();
    }
}
