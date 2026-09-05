using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;

public class CluesManager : MonoBehaviour
{
    public static CluesManager instance;
    public TextMeshProUGUI clueTextBox;
    public string clueToGive;
    public string repetitiveClue;
    public float fadeTimer; 
    public float fadeDuration;
    public float repeatTimer;
    public float repeatDuration;

    void Start()
    {
        instance = this;
    }

    void Update()
    {
        if (fadeTimer >= fadeDuration)
        {
            clueTextBox.text = "";
        }
        else if (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
        }

        if (repetitiveClue != "")
        {
            repeatTimer += Time.deltaTime;
            if (repeatTimer >= repeatDuration)
            {
                clueTextBox.text = repetitiveClue;
                fadeTimer = 0;
                repeatTimer = 0;
            }
        }
    }

    public void GiveClue(string clue, string repeatClue)
    {
        clueToGive = clue;
        clueTextBox.text = clue;
        if (repeatClue != "")
        {
            repetitiveClue = repeatClue;
        }
        fadeTimer = 0;
    }
}
