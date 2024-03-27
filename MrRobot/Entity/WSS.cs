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
		string uri = "wss://stream.bybit.com/v5/public/linear";
		ClientWebSocket ws;
		WebSocketReceiveResult Rec;		// Результат асинхронного запроса
		public WebSocketState State =>	// Состояние подключения
			ws.State;

		public delegate void RECV(dynamic data);
		public RECV DataNew { get; set; }

		public WSS() =>
			Start();

		// Подключение и запуск WebSocket
		async void Start()
		{
			ws = new ClientWebSocket();
			await ws.ConnectAsync(new Uri(uri), CancellationToken.None);
			await Receive();
		}

		// Ожидание подключения перед подпиской
		async Task ConnWait()
		{
			if (ws.State == WebSocketState.Open)
				return;


			await Task.Run(() =>
			{
				while (ws.State == WebSocketState.Connecting)
					Thread.Sleep(200);
			});
		}




		#region ПОДПИСКА И ОТПИСКА

		public async void Subscribe(string topic)
		{
			await ConnWait();
			await SubTask(topic);
		}
		public async void Unsubscribe(string topic)
		{
			await ConnWait();
			await SubTask(topic, "unsubscribe");
		}

		async Task SubTask(string topic, string sub = "subscribe")
		{
			Write($"{topic}:	{sub}...		");
			var req = JsonConvert.SerializeObject(new
			{
				op = sub,
				args = new string[] { topic }
			});
			await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(req)),
							   WebSocketMessageType.Text,
							   true,
							   CancellationToken.None);
			WriteLine("успешно.");
		}

		#endregion
		


		#region ПОЛУЧЕНИЕ ДАННЫХ

		// Получение сообщений по подпискам
		async Task Receive()
		{
			string RecMsg = "";
			var isConcat = false;
			var buffer = new ArraySegment<byte>(new byte[8192]);

			while (true)
			{
				Rec = await ws.ReceiveAsync(buffer, CancellationToken.None);
				if (IsClosed())
					break;
				if (!IsText())
					break;
				if (buffer.Array == null)
					continue;

				string msg = Encoding.UTF8.GetString(buffer.Array, 0, Rec.Count);

				if (isConcat)
					RecMsg += msg;
				else
					RecMsg = msg;

				try
				{
					dynamic json = JsonConvert.DeserializeObject(RecMsg);
					DataNew(json);
					isConcat = false;
					//WriteLine($"{RecMsg.Length}:	{RecMsg}");
				}
				catch (Exception ex)
				{
					WriteLine();
					WriteLine(ex);
					WriteLine($"msg.{msg.Length}:	{msg}");
					WriteLine($"rec.{RecMsg.Length}:	{RecMsg}");
					WriteLine();
					isConcat = true;
				}
			}
		}

		// Соединение закрылось
		bool IsClosed()
		{
			if (Rec.MessageType != WebSocketMessageType.Close)
				return false;

			WriteLine("### about to close ###");
			return true;
		}

		// Получено сообщение
		bool IsText() =>
			Rec.MessageType == WebSocketMessageType.Text;

		#endregion
	}
}
