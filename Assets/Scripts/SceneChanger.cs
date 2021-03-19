using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneChanger : MonoBehaviour
{
    public void Menu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void Scene1()
    {
        SceneManager.LoadScene("Simple");
    }
    public void Scene2()
    {
        SceneManager.LoadScene("Complex");
    }
    public void Scene3()
    {
        SceneManager.LoadScene("Challenge");
    }
}