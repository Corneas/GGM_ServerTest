JobQueue

더이상 GameRoom에서 뿅 하고 실행시키는 것이 아닌 Task를 만들어 실행시킬 예정이다

lock을 걸어 모든 함수를 제어하면 플레이어의 수가 조금만 늘어도 버벅거릴 수 있기 때문에 lock을 전부 지워주도록 하자
```cs

public GameRoom Add(int mapId)  
{  
    GameRoom gameRoom = new GameRoom();  
    gameRoom.Init(mapId);  
    gameRoom.Push(GameRoom.Init, mapId);  

    lock (_lock)
    {
        gameRoom.RoomId = _roomId;
        _rooms.Add(_roomId, gameRoom);
        _roomId++;
    }

    return gameRoom;
}
```

이렇게 Push를 할 것이지만, GameRoom의 Init은 static이 아니기 때문에 문제가 발생.

방법 1. GameRoom의 Init을 static으로 바꿔준다. <br>
방법 2. 대문자 GameRoom이 아닌 소문자 gameRoom을 넘겨준다. (어떤 room에서 Init을 할 것인지 넘겨줌)

이처럼 GameRoom 내 함수를 사용하는 모든 코드는 다 Push를 이용하여 바꿔줄 것. (FindPlayer, Broadcast 제외)

앞으로는 NullCrash가 일어나는것을 방지하고자 예외처리를 꼼꼼히 해줄 필요가 있음

```cs
public virtual void OnDead(GameObject attacker)
{
	if (Room == null)
	return;

	S_Die diePacket = new S_Die();
	diePacket.ObjectId = Id;
	diePacket.AttackerId = attacker.Id;
	Room.Broadcast(diePacket);

	GameRoom room = Room;
	room.Push(room.LeaveGame, Id);

	Stat.Hp = Stat.MaxHp;
	PosInfo.State = CreatureState.Idle;
	PosInfo.MoveDir = MoveDir.Down;
	PosInfo.PosX = 0;
	PosInfo.PosY = 0;

	room.Push(room.EnterGame, this);
}
```

OnDead의 LeaveGame, EnterGame을 Push로 바꿔줌으로 이제는 더이상 순서가 명확하지 않음. <br>
경우에 따라 LeaveGame이 실행되기 전 좌표가 잡힐 수 있어 엉뚱한 좌표로 이동될 가능성이 생긴것. 

방법 1. push를 하지 않고 바로 실행되게끔 수정 <br>
방법 2. 스탯과 좌표를 지정하는것 또한 Job으로 변경
