using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private UI_InputReader uI_InputReader;
    [SerializeField] private Button[] pauseButtons;
    private bool isActive = false;

    void Awake()
    {
        uI_InputReader = GetComponent<UI_InputReader>();
        uI_InputReader.OnPausePress += OnPause;
        container.SetActive(isActive);

        foreach(Button button in pauseButtons)
        {
            button.onClick.AddListener(() => OnButtonPress(button.name));
        }
    }

    void OnDisable()
    {
        uI_InputReader.OnPausePress -= OnPause;
        foreach (Button button in pauseButtons)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    public void TogglePause()
    {
        OnPause();
    }

    void OnPause()
    {
         bool currentMatchState = 
            MatchManager.Instance.State == 
            MatchState.Playing;
        
        if(!currentMatchState) return;
        isActive = !isActive;
        UpdateUI();
    }

    void UpdateUI()
    {
        container.SetActive(isActive);
        Time.timeScale = isActive ? 0f : 1f;
    }

    void OnButtonPress(string buttonName)
    {
        Debug.Log(buttonName);
        switch (buttonName)
        {
            case "Resume":
            OnPause();
            break;
            
            case "Exit":
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Time.timeScale = 1f;
            break;

            default: 
            Debug.Log("Unrecognized Button");
            break;
        }
    }
}
