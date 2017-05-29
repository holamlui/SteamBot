using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.Configuration;
using System.IO;
using System.Threading;

namespace SteamBot
{
    class Program
    {
        static string user_name, user_password;

        static string authcode;
        static string twofactor;

        static SteamClient steamClient;
        static CallbackManager callbackManager;
        static SteamUser steamUser;
        static SteamFriends steamFriends;
        static bool isRunning;
        static void Main(string[] args)
        {
            Console.Title = "Steam Bot";
            Console.WriteLine("Ctrl + C to quit the program");

            Console.Write("Username: ");
            //user_name = Console.ReadLine();
            user_name = "";
            Console.Write("Password: ");
            //user_password = Console.ReadLine();
            user_password = "";
            SteamLogIn();
        }
        static void SteamLogIn()
        {
            steamClient = new SteamClient();
            callbackManager = new CallbackManager(steamClient);
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();

            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);

            callbackManager.Subscribe<SteamFriends.FriendMsgCallback>(OnChatMessage);
          
            /*new Callback<SteamClient.ConnectedCallback>(OnConnected, callbackManager);
            new Callback<SteamUser.LoggedOnCallback>(OnLoggedOn, callbackManager);
            new Callback<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback, callbackManager);

            new Callback<SteamUser.AccountInfoCallback>(OnAccountInfo, callbackManager);
            new Callback<SteamFriends.FriendMsgCallback>(OnChatMessage, callbackManager);
            new Callback<SteamClient.DisconnectedCallback>(OnDisconnected, callbackManager);
            */
            isRunning = true;

            steamClient.Connect();

            while (isRunning)
            {
                callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            //Console.ReadKey();
        }
        static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to connect to steam! {0}", callback.Result);
                isRunning = false;
                return;
            }
            Console.WriteLine("Connected to Steam. \nLogging in '{0}'...\n",user_name);
            byte[] sentryHash = null;

            if (File.Exists("sentry.bin"))
            {
                byte[] sentryFile = File.ReadAllBytes("sentry.bin");

                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }

            steamUser.LogOn(new SteamUser.LogOnDetails
            { Username = user_name,
              Password = user_password,
              AuthCode = authcode,
              TwoFactorCode = twofactor,
              SentryFileHash = sentryHash,
            });
        }
        static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("\n{0} Disconnected From Steam, Reconnecting In 5 Seconds...\n", user_name);
            Thread.Sleep(TimeSpan.FromSeconds(5));

            steamClient.Connect();
        }
        static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                Console.WriteLine("Please Enter In Your Two Factor Auth Code.\n");
                twofactor = Console.ReadLine();
                return;
            }
            if (callback.Result == EResult.AccountLogonDenied)
            {
                Console.WriteLine("Account is steam guard Protected.");

                Console.Write("Please Enter In The Auth Code Sent To The Email At {0}: ", callback.EmailDomain);

                authcode = Console.ReadLine();

                return;
            }
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to connect to Steam account: {0}", callback.Result);
                isRunning = false;
                return;
            }

            Console.WriteLine("Sucessfully log in {0}", callback.Result);
        }
        static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }
        static void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("Updating Sentry File...");

            byte[] sentryHash = CryptoHelper.SHAHash(callback.Data);

            File.WriteAllBytes("sentry.bin", callback.Data);

            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,
                FileName = callback.FileName,
                BytesWritten = callback.BytesToWrite,
                FileSize = callback.Data.Length,
                Offset = callback.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = callback.OneTimePassword,
                SentryFileHash = sentryHash,
            });
            Console.WriteLine("Done.");
        }
        static void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            steamFriends.SetPersonaState(EPersonaState.Online);
            Console.WriteLine("Set PersonaState to Online");
        }
        static void OnChatMessage(SteamFriends.FriendMsgCallback callback)
        {
            String senderName = steamFriends.GetFriendPersonaName(callback.Sender);
            Console.WriteLine("Sender's id :{0} , display name : {1}", callback.Sender, senderName);
            if (callback.EntryType == EChatEntryType.ChatMsg)
                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Hello! "+ senderName);
        }
        
    }
}
