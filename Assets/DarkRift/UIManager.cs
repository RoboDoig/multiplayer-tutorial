using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager singleton;

    // UI Elements
    public InputField nameInputField;
    public Button startSessionButton;
    public Button localTestButton;
    public VerticalLayoutGroup connectedPlayersGroup;
    public GameObject connectedPlayerIndicatorPrefab;
    public Button readyButton;

    void Awake() {
        if (singleton != null) {
            Destroy(gameObject);
            return;
        }

        singleton = this;
    }

    void Start() {

    }

    public void PopulateConnectedPlayers(Dictionary<ushort, NetworkEntity> connectedPlayers) {
        ClearConnectedPlayers();
        foreach (KeyValuePair<ushort, NetworkEntity> connectedPlayer in connectedPlayers) {
            GameObject obj = Instantiate(connectedPlayerIndicatorPrefab);
            obj.transform.SetParent(connectedPlayersGroup.transform);
            obj.GetComponentInChildren<Text>().text = connectedPlayer.Value.playerName;
        }
    }

    public void ClearConnectedPlayers() {
        foreach (Transform child in connectedPlayersGroup.transform) {
            Destroy(child.gameObject);
        }
    }

    public void SetInputInteractable(bool interactable) {
        startSessionButton.interactable = interactable;
        localTestButton.interactable = interactable;
        nameInputField.interactable = interactable;
    }

    public void SetLobbyInteractable(bool interactable)
    {
        readyButton.interactable = interactable;
    }

    public void DisplayNetworkMessage(string message) {
        startSessionButton.GetComponentInChildren<Text>().text = message;
    }

    public void CloseUI() {
        this.gameObject.SetActive(false);
    }

    public void OpenUI() {
        this.gameObject.SetActive(true);
    }
}