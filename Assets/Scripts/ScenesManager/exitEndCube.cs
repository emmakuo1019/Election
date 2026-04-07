using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class exitEndCube : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        SceneManager.LoadScene("endGamePanel");
    }
}
