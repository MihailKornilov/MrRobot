using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using static System.Console;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MrRobot.Entity
{
	public class WSS
	{
		ClientWebSocket ws;
		string uri = "wss://stream.bybit.com/v5/public/linear";

		public WSS()
		{
			ws = new ClientWebSocket();
			Start();
		}

		async void Start()
		{
			await ws.ConnectAsync(new Uri(uri), CancellationToken.None);
			await Subscribe("publicTrade.BOMEUSDT");
			await Subscribe("publicTrade.BTCUSDT");
			await Receive();
		}


		async Task Subscribe(string topic)
		{
			Write($"{topic}:	подписка...		");
			var req = JsonConvert.SerializeObject(new
			{
				op = "subscribe",
				args = new string[] { topic }
			});
			await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(req)),
							   WebSocketMessageType.Text,
							   true,
							   CancellationToken.None);
			WriteLine("успешно.");
		}

		string MsgReceived;
		async Task Receive()
		{
			var buffer = new ArraySegment<byte>(new byte[8192]);
			MsgReceived = "";
			while (true)
			{
				var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
				if (IsClosed(result))
					break;
				if (!IsText(result))
					break;
				if (buffer.Array == null)
					continue;

				var msg = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
				WriteLine(msg);
				if (!IsMsgCorrect(msg))
					continue;

				if(MsgReceived.Length > 8000)
					WriteLine($"{MsgReceived.Length}:	{MsgReceived}");

				try
				{
					var data = JsonConvert.DeserializeObject<JObject>(MsgReceived);
				}
				catch
				{
					WriteLine();
					WriteLine("-------- Ошибка JSON");
					WriteLine($"{MsgReceived.Length}:	{MsgReceived}");
					WriteLine();
				}
				//if (data != null)
				//	WriteLine(data.ToString());

				//15:56:48:491    582:	{ "topic":"publicTrade.BTCUSDT","type":"snapshot","ts":1710680208837,"data":[{ "T":1710680208834,"s":"BTCUSDT","S":"Buy","v":"0.001","p":"66500.10","L":"PlusTick","i":"97d1e4a3-2daa-5231-b9b8-90758c0d0198","BT":false}]}
				//						{ "topic":"publicTrade.BTCUSDT","type":"snapshot","ts":1710680208837,"data":[{ "T":1710680208834,"s":"BTCUSDT","S":"Sell","v":"0.100","p":"66500.00","L":"MinusTick","i":"3383768b-7865-54b0-9b00-0a64093b5374","BT":false},{ "T":1710680208834,"s":"BTCUSDT","S":"Sell","v":"0.025","p":"66500.00","L":"ZeroMinusTick","i":"cc0c1142-e8e9-56f3-a500-8318694c33dc","BT":false}]}

				//18:46:49:697	446:	{"topic":"publicTrade.BTCUSDT","type":"snapshot","ts":1710690409303,"data":[{"T":1710690409296,"s":"BTCUSDT","S":"Sell","v":"0.001","p":"68000.00","L":"ZeroMinusTick","i":"226924f4-f21b-536e-8a5d-ab82e317f057","BT":false}]}
				//						{"topic":"publicTrade.BTCUSDT","type":"snapshot","ts":1710690409303,"data":[{"T":1710690409296,"s":"BTCUSDT","S":"Sell","v":"0.001","p":"68000.00","L":"ZeroMinusTick","i":"6ffb325f-eb46-5160-a2da-d0458b2f9437","BT":false}]}

				// 17:46:39:851	1443:	{"topic":"publicTrade.BOMEUSDT","type":"snapshot","ts":1710686800030,"data":[{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"12600","p":"0.015439","L":"ZeroPlusTick","i":"bda5e617-5b3a-5bf9-bbc8-c0d2634667db","BT":false},{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"62200","p":"0.015440","L":"PlusTick","i":"7cf61dbd-600d-55b4-9f93-e019d56c132d","BT":false},{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"400","p":"0.015440","L":"ZeroPlusTick","i":"b6a89b16-9be8-5e1e-bdaa-80c11124be88","BT":false},{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"100","p":"0.015440","L":"ZeroPlusTick","i":"9fe426ab-d180-5a24-8449-6f377b737fd4","BT":false},{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"8800","p":"0.015440","L":"ZeroPlusTick","i":"e8705eef-0bf6-5b46-be3e-e077df95c3d7","BT":false},{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"7800","p":"0.015440","L":"ZeroPlusTick","i":"0706a145-52a0-514a-a647-3a7acfc38af6","BT":false}]}
				//						{"topic":"publicTrade.BOMEUSDT","type":"snapshot","ts":1710686800030,"data":[{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"87400","p":"0.015440","L":"ZeroPlusTick","i":"ab3d7267-bb8a-59f8-a2d6-e22260972310","BT":false},{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"34000","p":"0.015441","L":"PlusTick","i":"7cccbef0-8c1c-58c5-8a41-645d40d35da0","BT":false},{"T":1710686800028,"s":"BOMEUSDT","S":"Buy","v":"12500","p":"0.015442","L":"PlusTick","i":"4d170c88-1c28-5c26-8525-85b31df5e965","BT":false}]}



				MsgReceived = "";
			}
		}

		bool IsClosed(WebSocketReceiveResult result)
		{
			if (result.MessageType != WebSocketMessageType.Close)
				return false;

			WriteLine("### about to close ###");
			return true;
		}

		bool IsText(WebSocketReceiveResult result) =>
			result.MessageType == WebSocketMessageType.Text;

		bool IsMsgCorrect(string msg)
		{
			if (msg.Contains("success"))
				return false;

			MsgReceived += msg;
			return msg.Contains("}]}");
		}
	}
}
