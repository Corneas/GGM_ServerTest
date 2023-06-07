유니티와 서버 연동 #2

서버를 연동하였으니 테스트를 해보자.

**PacketHandler**
```cs
class PacketHandler
{
    public static void S_ChatHandler(PacketSession session, IPacket packet)
    {   
        S_Chat chatPacket = packet as S_Chat;
        ServerSession serverSession = session as ServerSession;

        if (chatPacket.playerId == 1)
        {
            // 플레이어 탐색
            GameObject go = GameObject.Find("Player");
            if (go == null)
            {
                Debug.Log("Player not found");
            }
            else
            {
                Debug.Log("Player found");
            }
        }
    }
}
```
코드를 작성한 대로라면 PacketHandler에서 플레이어를 찾아 로그를 띄워야하지만, 아무일도 일어나지 않는다.
<br>=> 메인 쓰레드의 문제
<br> 현재까지 사용하던 네트워크 코드구조

데이터 송수신을 할 때 소켓에서 비동기적으로 네트워크 통신을 하고 있었다
<br>이후 백그라운드 쓰레드에서 기타 통신을 작업을 하고, PacketHandler에서 유니티 기능을 건들이려고 했지만 유니티에서는 멀티쓰레드를 <strong>지원하지 않는다.</strong>

해결법 : PacketHandler에 있는 함수를 유니티 메인 쓰레드에서 실행하게끔 해준다
<br>=>즉, 특정한 큐를 만들어 패킷을 큐에 넣어둔 다음, 큐에 있는 내용을 유니티 메인 쓰레드에서 꺼내주어 실행시켜주면 가능!

**ClientPacketManager.cs**
```cs
public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
{
  ushort count = 0;

  ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
  count += 2;
  ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
  count += 2;

  Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
  if (_makeFunc.TryGetValue(id, out func))
  {
    IPacket packet = func.Invoke(session, buffer);
    // 옵션이 있다면 실행
    if (onRecvCallback != null)
      onRecvCallback.Invoke(session, packet);
    // 없다면 바로 HandlePacket 실행
    else
      HandlePacket(session, packet);
  }
}

T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
{
  T pkt = new T();
  pkt.Read(buffer);
  return pkt;
}

// MakePacket에 있던 내용을 따로 분리하여 바로 실행하는 것이 아닌, 필요할 때 꺼내서 사용할 수 있게끔 변경
public void HandlePacket(PacketSession session, IPacket packet)
{
  Action<PacketSession, IPacket> action = null;
  if (_handler.TryGetValue(packet.Protocol, out action))
    action.Invoke(session, packet);
}
```
  
  MakePacket에 있던 액션을 따로 분리, 옵션에 따라 큐에 넣을 지 바로 실행할지 선택
  
  
  패킷을 담아줄 패킷 큐 생성
  
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
}
```

이후 받은 내용을 패킷큐에 푸쉬
**ServerSession.cs**
```cs
public override void OnRecvPacket(ArraySegment<byte> buffer)
{
  PacketManager.Instance.OnRecvPacket(this, buffer, (s, p) => PacketQueue.Instance.Push(p));
}
```

모든 준비는 끝! 이제 유니티에서 사용중인 스크립트에서 꺼내기만 하면 된다.

**NetWorkManager.cs**
```cs
    void Update()
    {
      IPacket packet = PacketQueue.Instance.Pop();

      if(packet != null)
      {
        // packet handler 시작
        PacketManager.Instance.HandlePacket(_session, packet);
      }
    }
```

유니티와 서버 연동 #2 마무리
