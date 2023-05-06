유니티와 서버 연동 #1

유니티와 서버연동을 할 때 가져와야 할 스크립트 목록
<br>(NetWork)
- Connector.cs
- Session.cs
- ServerSession.cs
- RecvBuffer.cs
- SendBuffer.cs

(Packet)
- ClientPacketManager.cs
- GenPackets.cs
- PacketHandler.cs
- PacketQueue.cs

스크립트를 이동을 한 후, GenPackets의 Span과 BitConverter.TryWriteBytes를 다른 방식으로 바꿔주어야 함
<br>(현재는 Span과 BitConverter를 유니티에서 지원하지만, 강의 당시에는 지원하지 않았다)</br>

**GenPacket.cs**
```cs
public class C_Move : IPacket
{
	public float posX;
	public float posY;
	public float posZ;

	public ushort Protocol { get { return (ushort)PacketID.C_Move; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
    // BitConverter.ToSingle을 이용하여 float형식으로 값 변환 및 사용
		this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(4096);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Move), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}
```

이후 PacketFormat 수정 후 NetWorkManager.cs 생성

**NetWorkManager.cs**
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

}
```

유니티와 서버 연동 #1 마무리
