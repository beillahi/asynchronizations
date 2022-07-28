using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography;
using System.Xml;

// https://github.com/MD-V/FritzDectCore/blob/0ba3e96dfa0da97dca46992ae0a9de70e75ef059/src/FritzDectCore/Helper/FritzApiHelper.cs

namespace FritzDectCore
{
    class Program
    {

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            if (args.Length > 3)
            {
                FritzApiHelper newFritzApiHelper = new FritzApiHelper();

                string sid;
                sid = await newFritzApiHelper.Login(args[0], args[1]);

                string[] sids;
                sids = await newFritzApiHelper.SwitchList(args[2], args[3]);
            }
        }

        private static class Md5Helper
        {

            public static string GetMd5Hash(MD5 md5Hash, string input)
            {

                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.Unicode.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }

        private class FritzApiHelper
        {

            private static string _FritzUrl = "http://fritz.box";
            private static string _HomeAutoSwitchUrl = "/webservices/homeautoswitch.lua";
            private static string _LoginUrl = "/login_sid.lua";

            private static string _ResponseKeyWord = "response=";

            public async Task<string> Login(string username, string password)
            {
                HttpClient client = new HttpClient();

                string response;
                response = await client.GetStringAsync(_FritzUrl + _LoginUrl);

                XmlDocument xml = new XmlDocument();

                string response1;
                response1 = response;

                xml.LoadXml(response1);

                var challenge = xml.GetElementsByTagName("Challenge").Item(0).InnerText;

                var challengeWithPassword = $"{challenge}-{password}";

                MD5 md5Hash = MD5.Create();

                string hash = Md5Helper.GetMd5Hash(md5Hash, challengeWithPassword);

                var responseChallenge = $"{challenge}-{hash}";

                string loginResponse;
                loginResponse = await client.GetStringAsync(_FritzUrl + _LoginUrl + $"?username={username}&" + _ResponseKeyWord + responseChallenge);

                XmlDocument responseXml = new XmlDocument();

                string loginResponse1;
                loginResponse1 = loginResponse;

                responseXml.LoadXml(loginResponse1);

                var sid = responseXml.GetElementsByTagName("SID").Item(0).InnerText;

                return sid;
            }

            public async Task<bool> TurnOn(string ain, string username, string password)
            {
                var sid = Login(username, password).Result;
                HttpClient client = new HttpClient();

                string switchOnResponse;
                switchOnResponse = await client.GetStringAsync(_FritzUrl + _HomeAutoSwitchUrl + $"?ain={ain}&switchcmd=setswitchon&sid={sid}");

                return true;
            }

            public async Task<bool> TurnOff(string ain, string username, string password)
            {
                var sid = Login(username, password).Result;
                HttpClient client = new HttpClient();

                string switchOnResponse;
                switchOnResponse = await client.GetStringAsync(_FritzUrl + _HomeAutoSwitchUrl + $"?ain={ain}&switchcmd=setswitchoff&sid={sid}");

                return true;
            }

            public async Task<string[]> SwitchList(string username, string password)
            {
                var sid = Login(username, password).Result;

                HttpClient client = new HttpClient();

                string switchOnResponse;
                switchOnResponse = await client.GetStringAsync(_FritzUrl + _HomeAutoSwitchUrl + $"?switchcmd=getswitchlist&sid={sid}");

                string switchOnResponse1 = switchOnResponse;
                return switchOnResponse1.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
        }
    }
}
