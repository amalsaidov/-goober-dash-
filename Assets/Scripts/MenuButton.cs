using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour
{
    public enum Action
    {
        Play, Settings, SettingsBack,
        PlayAgain, GoToMainMenu,
        Resume, RestartFromPause, MainMenuFromPause,
        // Lobby
        LobbyHost, LobbyJoin, LobbyBack, LobbyStart, LobbyToggleBots,
        // Open lobby from main menu
        Multiplayer
    }

    public Action action;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        switch (action)
        {
            case Action.Play:               MainMenuManager.Instance?.OnPlay();         break;
            case Action.Settings:           MainMenuManager.Instance?.OnSettings();     break;
            case Action.SettingsBack:       MainMenuManager.Instance?.OnSettingsBack(); break;
            case Action.PlayAgain:          MainMenuManager.Instance?.OnPlayAgain();    break;
            case Action.GoToMainMenu:       MainMenuManager.Instance?.OnGoToMainMenu(); break;
            case Action.Resume:             PauseManager.Instance?.Resume();            break;
            case Action.RestartFromPause:   PauseManager.Instance?.RestartGame();       break;
            case Action.MainMenuFromPause:  PauseManager.Instance?.GoToMainMenu();      break;
            case Action.LobbyHost:          LobbyPanelController.Instance?.OnHostClicked();       break;
            case Action.LobbyJoin:          LobbyPanelController.Instance?.OnJoinClicked();       break;
            case Action.LobbyBack:          LobbyPanelController.Instance?.OnBackClicked();       break;
            case Action.LobbyStart:         LobbyPanelController.Instance?.OnStartClicked();      break;
            case Action.LobbyToggleBots:    LobbyPanelController.Instance?.OnToggleBotsClicked(); break;
            case Action.Multiplayer:
                UIManager.Instance?.HideMainMenu();
                UIManager.Instance?.ShowLobbyPanel();
                break;
        }
    }
}
