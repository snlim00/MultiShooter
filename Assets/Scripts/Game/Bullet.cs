using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Bullet : MonoBehaviour
{
    public bool isMasterClientLocal => PhotonNetwork.IsMasterClient && photonView.IsMine;

    private PhotonView photonView;
    private Vector2 direction = Vector2.up;
    private float speed = 15f;

    public PlayerObject owner;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void Init(Vector2 dir, PlayerObject owner)
    {
        direction = dir;
        this.owner = owner;
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) { return; }

        var distance = speed * Time.deltaTime;

        transform.localPosition += (Vector3)(direction * distance);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!photonView.IsMine) { return; }

        if (collision.tag != "Player") { return; }

        PlayerObject player = collision.GetComponent<PlayerObject>();

        if(player == owner) { return; }

        player.GetDamage(1);

        //PhotonNetwork.Destroy(this.gameObject);
    }

    private void OnBecameInvisible()
    {
        if (!photonView.IsMine) { return; }

        PhotonNetwork.Destroy(this.gameObject);
    }
}
