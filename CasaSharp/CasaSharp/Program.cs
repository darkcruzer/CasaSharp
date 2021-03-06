﻿using CasaSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text; 
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CasaSharp
{
    class Program
    {
        static string usernme,password,broadcast;
        static public Dictionary<string, List> devices = new Dictionary<string, List>();
        static public CasaSharp.Config config = new CasaSharp.Config("./conf.ini");
        static WebServer ws = new WebServer("http://*:" + config.Value("port") + "/", "/");
        static MD5 md5 = new MD5CryptoServiceProvider();
        
        public static string MD5(string TextToHash = null)
        {
            byte[] textToHash = Encoding.Default.GetBytes(TextToHash);
            byte[] result = md5.ComputeHash(textToHash);

            return System.BitConverter.ToString(result).Replace("-", "");
        }

        private static List<List> GetPlugs()
        {
            WebClient _wc = new WebClient();
            string url = "http://icomen.yunext.com/api/device/rf/list?accessKey=Q763W08JZ07V23FR99410B3PC945LT28&username=" + HttpUtility.UrlEncode(usernme) + "&password=" + MD5(password);
            var json = _wc.DownloadString(url);
            RootObject r = JsonConvert.DeserializeObject<RootObject>(json);
            return r.list;
        }

        public static string GetBasisStation()
        {
            WebClient _wc = new WebClient();
            string url = "http://icomen.yunext.com/api/device/wifi/list?accessKey=Q763W08JZ07V23FR99410B3PC945LT28&username=" + HttpUtility.UrlEncode(usernme) + "&password=" + MD5(password);
            var json = _wc.DownloadString(url);
            RootObject r = JsonConvert.DeserializeObject<RootObject>(json);
            string code = r.list[0].companyCode + r.list[0].deviceType + r.list[0].authCode;
            return code;
        }
        static void Main(string[] args)
        {
            ws.Start();
            Console.WriteLine("Webserver running ...");
            Console.WriteLine("Loading Devices...");
            Start();
        }
        
        public static void Start()
        {

            Console.Title = "CasaSharp";
            config = new CasaSharp.Config("./conf.ini");
                usernme = config.Value("email");
                password = config.Value("password");
            broadcast = config.Value("broadcast");
            if (Directory.Exists("./data/"))
            {
                Directory.CreateDirectory("./data/images/");
            }
            var plugs = GetPlugs();
            devices.Clear();
            foreach (var plug in plugs)
            {
                devices.Add(plug.addressCode, plug);
            }
        }

        public static byte[] hex2bin(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }


        public static string EncryptString(byte[] decryptedString)
        {
            using (var provider = new AesManaged())
            {
                provider.Padding = PaddingMode.Zeros;
                provider.BlockSize = 128;
                provider.KeySize = 128;
                provider.IV = Encoding.Default.GetBytes("0123456789abcdef");
                provider.Key = Encoding.Default.GetBytes("0123456789abcdef");
                provider.Mode = CipherMode.CBC;

                var test = provider.CreateEncryptor().TransformFinalBlock(decryptedString, 0, decryptedString.Length);
                return ByteArrayToString(test);
            }
        }

        private static string ByteArrayToString(byte[] arr)
        {
            return System.Text.Encoding.Default.GetString(arr);
        }

        public static void Switch(string mac, string msg)
        {
            for (int i = 0; i < 4; i++)
            {
                var t1 = System.Text.Encoding.Default.GetString(hex2bin("0140" + mac + "10"));
                var t2 = EncryptString(hex2bin(msg));
                var text = t1 + t2;
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                IPAddress serverAddr = IPAddress.Parse(broadcast);

                IPEndPoint endPoint = new IPEndPoint(serverAddr, 8530);

                byte[] send_buffer = Encoding.Default.GetBytes(text);

                sock.SendTo(send_buffer, endPoint);
                Thread.Sleep(500);
            }
            Start();
        }
    }
}
