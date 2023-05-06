유니티와 서버 연동 #3

하나의 미니 프로젝트를 만들어 테스트를 해볼 예정. 일반적인 MMO라 생각하고 최대한 비슷하게 만들 것이다

<br>클라이언트에서 접속을 할 때 바로 접속을 시켜주는 것이 아닌, 서버에서 클라를 접속시켜줄 때 필요한 준비를 마쳤을 때 연결을 시켜줄 것

**GameRoom.cs**
```cs
		public void Enter(ClientSession session)
		{
			// 플레이어 추가
			_sessions.Add(session);
			session.Room = this;
		}
```

게임방에 사람이 들어올 경우 그 주변 유저들에게도 해당 유저가 입장했음을 알리며, 해당 유저에게도 주변에 있는 유저의 정보를 전달
<br>해당 기능을 하기 위해선 몇가지 정보가 필요한데, 이를 위해 XML을 수정을 할 것이다

**PDL.xml**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<PDL>
	<packet name="S_BroadcastEnterGame">
		<int name="playerId"/>
		<float name="posX"/>
		<float name="posY"/>
		<float name="posZ"/>
	</packet>
	<packet name="C_LeaveGame">
	</packet>
	<packet name="S_BroadcastLeaveGame">
		<int name="playerId"/>
	</packet>
	<packet name="S_PlayerList">
		<list name="player">
			<bool name="isSelf"/>
			<int name="playerId"/>
			<float name="posX"/>
			<float name="posY"/>
			<float name="posZ"/>
		</list>
	</packet>
	<packet name="C_Move">
		<float name="posX"/>
		<float name="posY"/>
		<float name="posZ"/>
	</packet>
	<packet name="S_BroadcastMove">
		<int name="playerId"/>
		<float name="posX"/>
		<float name="posY"/>
		<float name="posZ"/>
	</packet>
</PDL>
```

- S_~ : 서버에서 사용할 패킷
- C_~ : 클라이언트에서 사용할 패킷
- S_BroadcastEnterGame : 입장한 클라이언트의 정보를 남아있는 유저에게 알릴 패킷
- C_LeaveGame : 클라이언트가 게임을 종료했음을 알리는 패킷
- S_BroadcastLeaveGame : 종료한 클라이언트의 정보를 남아있는 유저에게 알릴 패킷
- isSelf : 여러 유저들 중 자신의 패킷을 구분하기 위한 bool
- C_Move : 클라이언트에서 서버에게 전송할 이동할 위치
- C_BroadcastMove : 유저가 이동했음을 다른 유저에게 알릴 패킷
- playerId : 여러 유저들 중 누가 행동을 하는지 구분하기 위함

위의 정보를 토대로 유저의 입장/퇴장/움직임의 정보를 알릴 것이다
**ClientSession.cs**
```cs
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;

namespace Server
{
	class ClientSession : PacketSession
	{
		public int SessionId { get; set; }
		public GameRoom Room { get; set; }

		public float PosX { get; set; }
		public float PosY { get; set; }
		public float PosZ { get; set; }

		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			Program.Room.Push(() => Program.Room.Enter(this));
		}

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			SessionManager.Instance.Remove(this);
			if (Room != null)
			{
				GameRoom room = Room;
				room.Push(() => room.Leave(this));
				Room = null;
			}

			Console.WriteLine($"OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}
	}
}
```

**GameRoom.cs**
```cs
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
	class GameRoom : IJobQueue
	{
		List<ClientSession> _sessions = new List<ClientSession>();
		JobQueue _jobQueue = new JobQueue();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

		public void Push(Action job)
		{
			_jobQueue.Push(job);
		}

		public void Flush()
		{
			// N ^ 2
			foreach (ClientSession s in _sessions)
				s.Send(_pendingList);

			Console.WriteLine($"Flushed {_pendingList.Count} items");
			_pendingList.Clear();
		}

		public void Broadcast(ArraySegment<byte> segment)
		{
			//S_Chat packet = new S_Chat();
			//packet.playerId = session.SessionId;
			//packet.chat =  $"{chat} I am {packet.playerId}";
			//ArraySegment<byte> segment = packet.Write();

			_pendingList.Add(segment);			
		}

		public void Enter(ClientSession session)
		{
			// 플레이어 추가
			_sessions.Add(session);
			session.Room = this;

			// 새로 들어온 플레이어에게 모든 플레이어 목록 전송
			S_PlayerList players = new S_PlayerList();
			foreach(ClientSession s in _sessions){
				players.players.Add(new S_PlayerList.Player()
				{
					isSelf = (s == session),
					playerId = s.SessionId,
					posX = s.PosX,
					posY = s.PosY,
					posZ = s.PosZ,
				});
            }
			session.Send(players.Write());

			// 새로운 플레이어가 입장했음을 모두에게 알림
			S_BroadcastEnterGame enter = new S_BroadcastEnterGame();
			enter.playerId = session.SessionId;
			enter.posX = 0;
			enter.posY = 0;
			enter.posZ = 0;
			Broadcast(enter.Write());
		}

		public void Leave(ClientSession session)
		{
			// 플레이어 제거
			_sessions.Remove(session);

			// 제거를 모두에게 알림
			S_BroadcastLeaveGame leave = new S_BroadcastLeaveGame();
			leave.playerId = session.SessionId;
			Broadcast(leave.Write());
		}
		
		public void Move(ClientSession session, C_Move packet)
        {
			// 좌표 변경
			session.PosX = packet.posX;
			session.PosY = packet.posY;
			session.PosZ = packet.posZ;

			// 모두에게 알림
			S_BroadcastMove move = new S_BroadcastMove();
			move.playerId = session.SessionId;
			move.posX = session.PosX;
			move.posY = session.PosY;
			move.posZ = session.PosZ;
			Broadcast(move.Write());
        }
	}
}
```

**PacketHandler.cs**
```cs
using System;
using System.Collections.Generic;
using System.Text;
using Server;
using ServerCore;

class PacketHandler
{
    public static void C_LeaveGameHandler(PacketSession session, IPacket packet)
    {
        ClientSession clientSession = session as ClientSession;
        if (clientSession.Room == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.Leave(clientSession));
    }

    public static void C_MoveHandler(PacketSession session, IPacket packet)
    {
        C_Move movePacket = packet as C_Move;
        ClientSession clientSession = session as ClientSession;

        if (clientSession.Room == null)
            return;
        System.Console.WriteLine($"{movePacket.posX}, {movePacket.posY}, {movePacket.posZ}");
        GameRoom room = clientSession.Room;
        room.Push(() => room.Move(clientSession, movePacket));
    }
}
```

이대로 빌드하면 오류가 나게 됩니다.
<br>DummyClient에서는 그저 이동하는 시뮬레이션을 할 것이고 실제론 유니티 내에서 작업을 해줄 것이기 때문에 빌드 통과를 위한 코드 수정
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

**DummyClient/SessionManager.cs**
```cs
using System;
using System.Collections.Generic;
using System.Text;

namespace DummyClient
{
	class SessionManager
	{
		static SessionManager _session = new SessionManager();
		public static SessionManager Instance { get { return _session; } }

		List<ServerSession> _sessions = new List<ServerSession>();
		object _lock = new object();
		Random _rand = new Random();

		//public void SendForEach()
		//{
		//	lock (_lock)
		//	{
		//		foreach (ServerSession session in _sessions)
		//		{
		//			C_Chat chatPacket = new C_Chat();
		//			chatPacket.chat = $"Hello Server !";
		//			ArraySegment<byte> segment = chatPacket.Write();

		//			session.Send(segment);
		//		}
		//	}
		//}

// 이동패킷 전송
		public void SendForEach()
		{
			lock (_lock)
			{
				foreach (ServerSession session in _sessions)
				{
        -50부터 50까지 사이의 랜덤한 값을 이동시켜줌
					C_Move movePacket = new C_Move();
					movePacket.posX = _rand.Next(-50, 50);
					movePacket.posY = 1;
					movePacket.posZ = _rand.Next(-50, 50);

					session.Send(movePacket.Write());
				}
			}
		}

		public ServerSession Generate()
		{
			lock (_lock)
			{
				ServerSession session = new ServerSession();
				_sessions.Add(session);
				return session;
			}
		}
	}
}
```

유니티와 서버 연동 #3 마무리
