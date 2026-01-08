using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetBoundary : MonoBehaviour
{
    void OnCollisionEnter()
    {
        GameManager.Instance.GoalScored();
    }
}
