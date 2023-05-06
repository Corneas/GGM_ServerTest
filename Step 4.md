유니티와 서버 연동 #4

목표 : MyPlayer를 이동시키고 다른 유저 정보를 받아와 출력시키고, 이동시키는 것

그동안 NetworkManager가 모든 기능을 수행하고 있었지만 실제 게임에서는 Player라는 객체들이 돌아다닐 것이기 때문에 이를 관리할 클래스가 필요하다

Player : 모든 플레이어가 들고있을 클래스
MyPlayer : 내 플레이어가 들고있을 클래스

**Player.cs**
```cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int PlayerId { get; set; }
}

```

**MyPlayer.cs**
```cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPlayer : Player
{
    NetworkManager _network;

    WaitForSeconds waitSec = new WaitForSeconds(0.25f);

    private void Start()
    {
        StartCoroutine(IESendPacket());
        _network = FindObjectOfType<NetworkManager>();
    }

    private IEnumerator IESendPacket()
    {
        while (true)
        {
            yield return waitSec;

            C_Move movePacket = new C_Move();
            movePacket.posX = Random.Range(-50, 50);
            movePacket.posY = 0;
            movePacket.posZ = Random.Range(-50, 50);

            _network.Send(movePacket.Write());
        }
    }
}
```

이동 패킷은 플레이어와 관련이 있기 때문에 MyPlayer에서 패킷을 보낸다

한 프레임 안에서 최대한 많은 정보를 처리하기 위해 스크립트들 수정!

**PacketQueue.cs**
```cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacketQueue
{
    public static PacketQueue Instance { get; } = new PacketQueue();

    Queue<IPacket> _packetQueue = new Queue<IPacket>();
    object _lock = new object();

    // 로그 출력 명령을 저장해둠
    public void Push(IPacket packet)
    {
        lock (_lock)
        {
            _packetQueue.Enqueue(packet);
        }
    }

    // 저장한 로그 출력 명령을 꺼냄
    public IPacket Pop()
    {
        lock (_lock)
        {
            if (_packetQueue.Count == 0)
                return null;

            return _packetQueue.Dequeue();
        }
    }

    public List<IPacket> PopAll()
    {
        List<IPacket> list = new List<IPacket>();

        lock (_lock)
        {
            while (_packetQueue.Count > 0)
                list.Add(_packetQueue.Dequeue());
        }

        return list;
    }
}
```

**NetworkManager.cs**
```cs
using DummyClient;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
	// 서버 세션 생성
	ServerSession _session = new ServerSession();
	private WaitForSeconds waitSec3f = new WaitForSeconds(3f);

    void Start()
    {
		// DNS (Domain Name System)
		string host = Dns.GetHostName();
		IPHostEntry ipHost = Dns.GetHostEntry(host);
		IPAddress ipAddr = ipHost.AddressList[0];
		IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

		// 커넥터를 이용하여 서버 연동, 서버 세션 생성
		Connector connector = new Connector();

		// 세션 반환, 클라의 개수는 1개
		connector.Connect(endPoint,
			() => { return _session; },
			1);
	}

    void Update()
    {
		//IPacket packet = PacketQueue.Instance.Pop();

		// 패킷 리스트에 있는 내용 가져옴
		List<IPacket> list = PacketQueue.Instance.PopAll();
		foreach(IPacket packet in list)
			PacketManager.Instance.HandlePacket(_session, packet);
			
		//if(packet != null)
		//{
		//	// packet handler 시작
		//	PacketManager.Instance.HandlePacket(_session, packet);
		//}
    }

	public void Send(ArraySegment<byte> sendBuff)
    {
		_session.Send(sendBuff);
    }


}
```

**PacketHandler.cs**
```cs
using DummyClient;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

class PacketHandler
{
    public static void S_BroadcastEnterGameHandler(PacketSession session, IPacket packet)
    {
        S_BroadcastEnterGame pkt = packet as S_BroadcastEnterGame;
        ServerSession serverSession = session as ServerSession;

        PlayerManager.Instance.EnterGame(pkt);
    }

    public static void S_BroadcastLeaveGameHandler(PacketSession session, IPacket packet)
    {
        S_BroadcastLeaveGame pkt = packet as S_BroadcastLeaveGame;
        ServerSession serverSession = session as ServerSession;

        PlayerManager.Instance.LeaveGame(pkt);
    }

    public static void S_PlayerListHandler(PacketSession session, IPacket packet)
    {
        S_PlayerList pkt = packet as S_PlayerList;
        ServerSession serverSession = session as ServerSession;
        PlayerManager.Instance.Add(pkt);
    }

    public static void S_BroadcastMoveHandler(PacketSession session, IPacket packet)
    {
        S_BroadcastMove pkt = packet as S_BroadcastMove;
        ServerSession serverSession = session as ServerSession;

        PlayerManager.Instance.Move(pkt);
    }
}
```

유니티와 서버 연동 #4 마무리
