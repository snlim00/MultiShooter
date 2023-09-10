using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class PlayerObject : MonoBehaviour
{
    private GameManager gameMgr;

    [SerializeField] private GameObject bulletPref;

    public PhotonView photonView;
    private Rigidbody2D rigidBody;
    private SpriteRenderer spriteRenderer;

    public bool canMove = true;
    public float speed = 30f;

    public int maxHp = 3;
    public int currentHp = 3;
    public float hpRatio
    {
        get { return (float)currentHp / maxHp; }
    }

    private float shootDelay = 0.5f;
    private bool isShootable = true;

    private void Awake()
    {
        Debug.Log("Awake");

        photonView = GetComponent<PhotonView>();
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        Debug.Log("Start");

        gameMgr = GameManager.Instance;

        if(photonView.IsMine)
        {
            spriteRenderer.color = Color.blue;
        }
        else
        {
            spriteRenderer.color = Color.red;
        }

        if(!PhotonNetwork.IsMasterClient)
        {
            Camera.main.transform.rotation = new(0, 0, 180, 0);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) { return; }

        MoveInput();

        if(Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
        {
            ShootBullet();
        }

        if(Input.GetKeyDown(KeyCode.Escape) && gameMgr.gameState != GameState.Over)
        {
            PhotonNetwork.LeaveRoom();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    private void MoveInput()
    {
        if(canMove == false) { return; }

        float direction = PhotonNetwork.IsMasterClient ? 1 : -1; //마스터가 아닌 경우 카메라가 반전되며 조작도 반전되므로 방향에 -1을 곱하는 방식으로 조작 방향을 동일하게 함.

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rigidBody.MovePosition(transform.position + (Vector3)(Vector2.left * direction * speed * Time.deltaTime));
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            rigidBody.MovePosition(transform.position + (Vector3)(Vector2.right * direction * speed * Time.deltaTime));
        }

        if(Input.GetMouseButton(0))
        {
            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            pos.y = transform.position.y;

            rigidBody.MovePosition(pos);
        }

        var viewportPos = Camera.main.WorldToViewportPoint(transform.position);

        if(viewportPos.x > 1) { viewportPos.x = 1; }
        if(viewportPos.x < 0) { viewportPos.x = 0; }

        transform.position = Camera.main.ViewportToWorldPoint(viewportPos);
    }

    private void ShootBullet()
    {
        if(isShootable == false) { return; }

        Bullet bullet = PhotonNetwork.Instantiate(bulletPref.name, transform.position, Quaternion.identity).GetComponent<Bullet>();

        if (PhotonNetwork.IsMasterClient)
        {
            bullet.Init(Vector2.up, this);
        }
        else
        {
            bullet.Init(Vector2.down, this);
        }

        isShootable = false;

        Observable.Timer(TimeSpan.FromSeconds(shootDelay))
            .Subscribe(_ => isShootable = true);
    }

    public void GetDamage(int damage)
    {
        if (gameMgr.gameState != GameState.Playing) { return; }

        photonView.RPC(nameof(GetDamageRPC), RpcTarget.All, damage);
    }

    [PunRPC]
    private void GetDamageRPC(int damage)
    {
        if (gameMgr.gameState != GameState.Playing) { return; }

        currentHp -= damage;

        if (currentHp < 0)
        {
            currentHp = 0;
        }

        gameMgr.RefreshHpData(hpRatio, photonView.Owner.ActorNumber);
    }

    public void OnGameReady()
    {
        var pos = transform.position;
        pos.x = 0;

        transform.position = pos;

        isShootable = false;
    }

    public void OnGameStart()
    {
        currentHp = maxHp;

        gameMgr.RefreshHpData(hpRatio, PhotonNetwork.LocalPlayer.ActorNumber);

        isShootable = true;
    }

    public void OnGameStop() 
    {
        currentHp = maxHp;
    }

    public void OnGameOver(bool isWin)
    {
        isShootable = false;

        if(isWin)
        {
            OnWin();
        }
        else
        {
            OnLose();
        }
    }

    public void OnWin()
    {

    }

    public void OnLose()
    {
        canMove = false;

        Color color = spriteRenderer.color;
        color.a = 0;
        spriteRenderer.color = color;
    }
}
