JobTimer

현재 GameRoom에 JobSerializer를 두어 Task들을 보관을 하는 방식을 사용중에 있다.

클라이언트 세션 쪽에서 요청이 와 패킷 핸들러를 통해 GamerRoom의 어떤 함수를 사용 및 JobSerializer에 있는 부분을 꺼내와 사용하지만

만약 방 안에 플레이어가 존재하지 않아 아무런 패킷이 오지 않는다고 한다면?

몬스터나 다른 객체들은 방 내부에 존재하기때문에 실행은 해줘야한다.

=> 서버에서 담당하고 있기 때문에 GameRoom의 Update함수는 항상 실행되어야한다

때문에 Main의 무한 반복문 내에서 GameRoom의 Update를 실행시켜주지만 서버가 아닌 클라이언트인 것처럼 무한반복이 되고있다.

현재는 틱을 계산하여 강제하고 있지만, 몬스터가 몇천, 몇만마리만 되도 틱을 계산하는 것 자체가 부담이 될 것이기 때문에 이를 바꿔주어야 한다.

그동안 사용한 Push의 방식은 Push를 가장 처음에 하게 된다면 실행까지 같이 하는것이었다.

이를 개선하여 Push는 정말 Push만, Flush는 다른곳에서 하는 방법은 없을까?

GameRoom의 Update내에서 Flush를 해줄것이지만 매 틱마다 Update를 돌리지는 않을 것이다.

```cs
System.Timers.Timer()
``` 
라는 기능을 이용해 특정 틱마다 Update를 실행시켜보자.

```cs
static List<System.Timers.Timer> _timers = new List<System.Timers.Timer>();

static void TickRoom(GameRoom room, int tick = 100)
{
		var timer = new System.Timers.Timer();
		timer.Interval = tick;
		// 특정 시간이 지났다면 어떤 이벤트를 실행할 것인지를 결정
		timer.Elapsed += ((s, e) =>  room.Update());
		// 자동으로 리셋
		timer.AutoReset = true;

		// Enabled이 true가 되면 시작
		timer.Enabled = true;

		_timers.Add(timer);
}```

TickRoom을 사용하여 원하는 틱마다 한 번씩 실행시킨다.

또한 타이머를 맞춰준다면 다른 쓰레드에서도 GameRoom의 Update를 사용 가능하다.

장점 => 직관적이다
단점 => 딜레이가 발생. 즉, 반응속도가 조금 더 느리게 된다.
