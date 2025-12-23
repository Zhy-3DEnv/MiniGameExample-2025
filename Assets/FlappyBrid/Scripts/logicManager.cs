using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class logicManager : MonoBehaviour
{

    public int playerScore;
    public Text scoreTex;
    public GameObject gameOverScene;
    [ContextMenu("Increase Score")]
    public void addScore(int scoreToAdd)//增加分数的代码块
    {
        playerScore = playerScore + scoreToAdd;
        scoreTex.text = playerScore.ToString();
    }
    public void restartGame()

    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void gameOver()
    {
        gameOverScene.SetActive(true);
    }
}
