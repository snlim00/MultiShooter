using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System;
using UniRx;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private const string version = "1.0";

    public Text connectionInfoText;
    public Button joinButton;

    private void Start()
    {
        PhotonNetwork.GameVersion = version;

        if(PhotonNetwork.IsConnected == false)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        joinButton.interactable = false;
        connectionInfoText.text = "Connecting...";


        joinButton.onClick.AddListener(() =>
        {
            ConnectMatchmaking();
        });

        Screen.SetResolution(607, 1080, false);
    }

    public override void OnConnectedToMaster()
    {
        joinButton.interactable = true;
        connectionInfoText.text = "Online : Connected to Master Server";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        joinButton.interactable = false;

        connectionInfoText.text = "Offline : Connection Disabled " + cause.ToString();

        PhotonNetwork.ConnectUsingSettings();
    }

    public void ConnectMatchmaking()
    {
        joinButton.interactable = false;

        if(PhotonNetwork.IsConnected)
        {
            connectionInfoText.text = "Connecting to Random Room...";

            PhotonNetwork.JoinRandomRoom();
        }
        else
        {

            connectionInfoText.text = "Offline : Connection Disabled - Try reconnecting...";

            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        connectionInfoText.text = "There is no empty room, Created new Room.";

        PhotonNetwork.CreateRoom(string.Empty, new RoomOptions{ MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "Connected with Room.";

        PhotonNetwork.LoadLevel("GameScene");
    }
}
