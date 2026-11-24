using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScecnManager : MonoBehaviour
{
    //クリックしたかどうか
    private bool isClicked = false;
    /// <summary>
    /// クリックしたらゲームシーンに移動する処理
    /// </summary>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0)&&!isClicked)
        {
            SceneManager.LoadScene("MainScene");

            isClicked = true;
        }
    }
}
