using System;
using System.Text;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Collections.Generic;
using static System.Console;

using MySqlConnector;
using Newtonsoft.Json;

using MrRobot.inc;
using MrRobot.Interface;

namespace MrRobot.Connector
{
    public class BYBIT
    {
        public const int ExchangeId = 1;        // ID биржи ByBit

        public BYBIT()
        {
            new Instrument();
        }


        public static string ApiKey
        {
            get => position.Val("5_ApiKey_Text");
            set => position.Set("5_ApiKey_Text", value);
        }
        public static string ApiSecret
        {
            get => position.Val("5_ApiSecret_Text");
            set => position.Set("5_ApiSecret_Text", value);
        }
        public static void ApiKeyChanged(object s, TextChangedEventArgs e) => ApiKey = (s as TextBox).Text;
        public static void ApiSecretChanged(object s, RoutedEventArgs e) => ApiSecret = (s as PasswordBox).Password;


        // Защищённые запросы к бирже
        public static dynamic Api(string query)
        {
            string API_KEY = ApiKey;
            string API_SECRET = ApiSecret;
            string URL = "https://api.bybit.com";
            //string URL = "https://api-testnet.bybit.com";
            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long recvWindow = 5000;

            string signature = "";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(API_SECRET)))
            {
                string msg = $"{timestamp}{API_KEY}{recvWindow}";
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(msg));
                signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-BAPI-API-KEY", API_KEY);
            client.DefaultRequestHeaders.Add("X-BAPI-TIMESTAMP", timestamp.ToString());
            client.DefaultRequestHeaders.Add("X-BAPI-RECV-WINDOW", recvWindow.ToString());
            client.DefaultRequestHeaders.Add("X-BAPI-SIGN", signature);

            var res = client.GetAsync(URL + query).Result;
            string content = res.Content.ReadAsStringAsync().Result;

            if (content.Length == 0)
                return "пусто..";

            dynamic json = JsonConvert.DeserializeObject(content);

            return json;
        }



        public class Instrument
        {
            static List<InstrumentUnit> UnitList { get; set; }
            static Dictionary<int, InstrumentUnit> ID_UNIT { get; set; }
            public Instrument()
            {
                UnitList = new List<InstrumentUnit>();
                ID_UNIT = new Dictionary<int, InstrumentUnit>();

                string sql = "SELECT*" +
                             "FROM`_instrument`" +
                            $"WHERE`exchangeId`={ExchangeId} " +
                             "ORDER BY`quoteCoin`,`baseCoin`";
                mysql.OutMethod += UnitAdd;
                mysql.List(sql);
                mysql.OutMethod -= UnitAdd;
            }
            // Внесение инструмента из базы в список через делегат
            public static void UnitAdd(MySqlDataReader res)
            {
                var unit = new InstrumentUnit(res);
                UnitList.Add(unit);
                ID_UNIT.Add(unit.Id, unit);
            }
        }
    }


    // Данные об инструменте
    public class InstrumentUnit : IIunit, IIunitBYBIT
    {
        public InstrumentUnit(MySqlDataReader res)
        {
            Id = res.GetInt32("id");
            Symbol = res.GetString("symbol");
            BaseCoin = res.GetString("baseCoin");
            QuoteCoin = res.GetString("quoteCoin");
            HistoryBegin = res.GetMySqlDateTime("historyBegin").ToString();
            IsTrading = res.GetInt16("isTrading") == 1;
            BasePrecision = res.GetDouble("basePrecision");
            MinOrderQty = res.GetDouble("minOrderQty");
            TickSize = res.GetDouble("tickSize");
        }


        public string Num { get; set; }             // Порядковый номер для вывода в списке
        public int Id { get; set; }                 // ID инструмента
        public string Name                          //Название инструмента в виде "BTC/USDT"
        {
            get => $"{BaseCoin}/{QuoteCoin}";
            set { }
        }
        public string Symbol { get; set; }          // Название инструмента в виде "BTCUSDT"

        public string HistoryBegin { get; set; }    // Дата начала истории свечных данных
        public int Decimals                         // Количество нулей после запятой
        {
            get => format.Decimals(TickSize);
            set { }
        }           
        public bool IsTrading { get; set; }         // Инструмент торгуется или нет

        public int CdiCount { get; set; }           // Количество скачанных свечных данных
        public SolidColorBrush CdiCountColor        // Скрытие количества, если 0
        {
            get => format.RGB(CdiCount > 0 ? "#777777" : "#FFFFFF");
            set { }
        }



        public string BaseCoin { get; set; }        // Название базовой монеты
        public string QuoteCoin { get; set; }       // Название котировочной монеты
        public double BasePrecision { get; set; }   // Точность базовой монеты
        public double MinOrderQty { get; set; }     // Минимальная сумма ордера
        public double TickSize { get; set; }        // Шаг цены
    }
}



/*
 "retCode": 0,
  "retMsg": "",
  "result": {
    "id": "28844402",
    "note": "StockMarket",
    "apiKey": "ECrLHflpIkeCnuo2oZ",
    "readOnly": 0,
    "secret": "",
    "permissions": {
      "ContractTrade": [
        "Order",
        "Position"
      ],
      "Spot": [
        "SpotTrade"
      ],
      "Wallet": [
        "AccountTransfer",
        "SubMemberTransfer"
      ],
      "Options": [
        "OptionsTrade"
      ],
      "Derivatives": [],
      "CopyTrading": [],
      "BlockTrade": [],
      "Exchange": [
        "ExchangeHistory"
      ],
      "NFT": [],
      "Affiliate": []
    },
    "ips": [
      "*"
    ],
    "type": 1,
    "deadlineDay": 27,
    "expiredAt": "2024-03-02T22:51:42Z",
    "createdAt": "2023-12-02T22:51:42Z",
    "unified": 0,
    "uta": 0,
    "userID": 7362350,
    "inviterID": 0,
    "vipLevel": "No VIP",
    "mktMakerLevel": "0",
    "affiliateID": 0,
    "rsaPublicKey": "",
    "isMaster": true,
    "parentUid": "0",
    "kycLevel": "LEVEL_1",
    "kycRegion": "RUS"
  },
  "retExtInfo": {},
  "time": 1707039874333
 */
