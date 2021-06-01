using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The main <c>SceneChanger</c> class.
/// </summary>
public class SceneChanger : MonoBehaviour
{
    /// <summary>
    /// Changes the scene to <c>Menu</c>.
    /// </summary>
    public void Menu()
    {
        SceneManager.LoadScene("Menu");
    }
    /// <summary>
    /// Changes the scene to <c>Simple</c>
    /// </summary>
    public void Scene1()
    {
        SceneManager.LoadScene("Simple");
    }
    /// <summary>
    /// Changes the scene to <c>Complex</c>
    /// </summary>
    public void Scene2()
    {
        SceneManager.LoadScene("Complex");
    }
    /// <summary>
    /// Changes the scene to <c>Challenge</c>
    /// </summary>
    public void Scene3()
    {
        SceneManager.LoadScene("Challenge");
    }
}