using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Security.Cryptography;
using System.Collections.Generic;

using MrRobot.inc;
using Newtonsoft.Json;

namespace MrRobot.Connector
{
    public class ByBit
    {
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
