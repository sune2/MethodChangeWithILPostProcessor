using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ChangeUtility
{
    public static void Hello()
    {
        Debug.Log("Hello World");
    }

    public static int Sum(List<int> v)
    {
        return v.Select(x => x * x).Sum();
    }
}
