using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using UniRx;
using DG.Tweening;

public enum GameState
{
    Waiting,
    Ready,
    Playing,
    Over,
}

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance => FindObjectOfType<GameManager>();

    [SerializeField] private Image myHpImg;
    [SerializeField] private Image enemyHpImg;
    [SerializeField] private Text infoTitle;

    [SerializeField] private List<Transform> spawnPositions;
    [SerializeField] private GameObject playerPref;

    private PlayerObject myPlayer;

    public GameState gameState { get; private set; } = GameState.Waiting;

    public event Action<GameState> onChangeGameState;

    private Sequence countdownSeq = null;

    private void Start()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        var spawnPosIdx = PhotonNetwork.IsMasterClient ? 0 : 1;
        var spawnPosition = spawnPositions[spawnPosIdx];

        PlayerObject player = PhotonNetwork.Instantiate(playerPref.name, spawnPosition.position, spawnPosition.rotation).GetComponent<PlayerObject>();
        player.name = "MyPlayer";

        myPlayer = player;
    }

    public void RefreshHpData(float remainHpRatio, int actorNumber)
    {
        if (!PhotonNetwork.InRoom || gameState != GameState.Playing) { return; }

        RefreshHpUI(remainHpRatio, actorNumber);

        if(remainHpRatio <= 0)
        {
            bool deadPlayerIsMe = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

            GameOver(!deadPlayerIsMe);
        }
    }

    private void RefreshHpUI(float remainHpRatio, int actorNumber)
    {
        Image hpImg = null;

        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            hpImg = myHpImg;
        }
        else
        {
            hpImg = enemyHpImg;
        }

        if (hpImg != null)
        {
            hpImg.fillAmount = remainHpRatio;
        }
    }

    private void ResetHpUI()
    {
        myHpImg.fillAmount = 1;
        enemyHpImg.fillAmount = 1;
    }

    private Sequence CreateTimerSequence(int time, float interval, Action<int> onEveryTick, Action onLastTick, Action onTimerEnd)
    {
        Sequence seq = DOTween.Sequence();

        for(int i = 0; i < time; ++i)
        {
            int remainTime = time - i;
            seq.AppendCallback(() => onEveryTick(remainTime));
            seq.AppendInterval(interval);
        }

        seq.AppendCallback(() => onLastTick());
        seq.AppendInterval(interval);
        seq.AppendCallback(() => onTimerEnd());

        return seq;
    }

    [PunRPC]
    private void GameReady()
    {
        gameState = GameState.Ready;

        myPlayer.OnGameReady();

        countdownSeq = CreateTimerSequence(3, 1, 
        onEveryTick : (remainTime) =>
        {
            infoTitle.text = remainTime.ToString();
        },
        onLastTick : () =>
        {
            infoTitle.text = "START!";
        },
        onTimerEnd : () =>
        {
            infoTitle.text = string.Empty;
            GameStart();
        });

        countdownSeq.Play();
    }

    private void GameStart()
    {
        gameState = GameState.Playing;

        myPlayer.OnGameStart();
    }

    [PunRPC]
    private void GameStop()
    {
        gameState = GameState.Waiting;

        countdownSeq?.Kill();

        myPlayer.OnGameStop();

        ResetHpUI();

        infoTitle.text = "Waiting For Other Player...";
    }

    private void GameOver(bool isWin)
    {
        gameState = GameState.Over;

        myPlayer.OnGameOver(isWin);

        if(isWin)
        {
            OnWin();
        }
        else
        {
            OnLose();    
        }

        Observable.Timer(TimeSpan.FromSeconds(3))
            .Subscribe(_ =>
            {
                PhotonNetwork.LeaveRoom();
            });
    }

    private void OnWin()
    {
        infoTitle.text = "You WIN!\n\nleave the game in 3 seconds";
    }

    private void OnLose()
    {
        infoTitle.text = "You LOSE...\n\nleave the game in 3 seconds";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("OnPlyaerEnteredRoom");
        if (!PhotonNetwork.IsMasterClient) { return; }

        if(PhotonNetwork.PlayerList.Length >= 2)
        {
            photonView.RPC(nameof(GameReady), RpcTarget.All);
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) { return; }

        photonView.RPC(nameof(GameStop), RpcTarget.All);

        if(PhotonNetwork.PlayerList.Length < 2)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }
}
