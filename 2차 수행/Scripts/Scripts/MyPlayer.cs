using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPlayer : Player
{
	C_Move movePacket = new C_Move();
	NetworkManager _network;

	ArraySegment<byte> moveSegment;
	ArraySegment<byte> playerListSegment;

	private MeshRenderer meshRenderer = null;

	private float h;
	private float v;

	void Start()
    {
		//StartCoroutine("CoSendPacket");
		StartCoroutine(IEMove());
		meshRenderer = GetComponent<MeshRenderer>();
		_network = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
		ChangeColor();
	}

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
			PrintPlayerList();
        }
    }

    private IEnumerator IEMove()
    {
        while (true)
        {
			yield return new WaitForSeconds(0.25f);

			if (moveSegment == null)
			{
				Debug.Log("moveSegment is null");
			}
			else
			{
				movePacket.Read(moveSegment);
			}

			h = Input.GetAxisRaw("Horizontal");
			v = Input.GetAxisRaw("Vertical");

			movePacket.posX += h;
			movePacket.posY = 0;
			movePacket.posZ += v;
			moveSegment = movePacket.Write();
			_network.Send(moveSegment);
		}

    }

	private void PrintPlayerList()
    {

    }

	private void ChangeColor()
    {
		meshRenderer.material = Resources.Load<Material>("MyPlayer");
    }

    IEnumerator CoSendPacket()
	{
		while (true)
		{
			yield return new WaitForSeconds(0.25f);

			C_Move movePacket = new C_Move();
			movePacket.posX = UnityEngine.Random.Range(-50, 50);
			movePacket.posY = 0;
			movePacket.posZ = UnityEngine.Random.Range(-50, 50);
			_network.Send(movePacket.Write());
		}
	}
}
