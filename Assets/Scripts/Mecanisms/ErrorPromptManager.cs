using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ErrorPromptManager : MonoBehaviour
{
    public static ErrorPromptManager EPMinstance;
    public TextMeshProUGUI ErrorText;
    public float timer;

    private void Start()
    {
        EPMinstance = this;
    }

    private void Update()
    {
        if (ErrorText.text != "")
        {
            timer += Time.deltaTime;
            if (timer >= 2)
            {
                ErrorText.text = "";
            }
        }
    }
}
