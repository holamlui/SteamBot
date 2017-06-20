using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;
using System.Configuration;
using System.IO;
using System.Reflection;
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
            user_name = Console.ReadLine();
           
            Console.Write("Password: ");
            user_password = Console.ReadLine();
        
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
            callbackManager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendList);
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
            //.SetPersonaName("⚡ Thunderbolt");           
        }
        static void OnChatMessage(SteamFriends.FriendMsgCallback callback)
        {
            String senderName = steamFriends.GetFriendPersonaName(callback.Sender);
            if (callback.EntryType == EChatEntryType.ChatMsg)
            {
                Console.WriteLine(senderName + " : " +callback.Message);           
                if (callback.Message.Length > 1)
                {
                    if (callback.Message.Remove(1) == "!")//if message starts with !
                    {
                        String command = callback.Message;
                        switch (command)
                        {
                            case "!help":
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!help    : List function of commands");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!hello   : Say hello to me");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!myid    : Show your Steam id");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!payday  : Make me play payday2");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!reset   : Reset status to Online");
                                break;
                            case "!hello":
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, ":steamhappy: Hello! " + senderName);
                                break;
                            case "!myid":
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Your id is: "+  callback.Sender.ConvertToUInt64());
                                break;
                            case "!payday":
                                PlayGame();
                                break;
                            case "!reset":
                                NoGame();
                                break;
                            default:
                                break;
                        }             
                    }
                    else // message not start with "!"
                    {
                        steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Please use !help for help");
                    }
                }
                else //only send one char
                {
                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Please use !help for help");
                }          
            }
        }       
        static void OnFriendList(SteamFriends.FriendsListCallback callback)
        {
            Thread.Sleep(3000);
            foreach(var friend in callback.FriendList)
            {
                if(friend.Relationship == EFriendRelationship.RequestRecipient)
                {
                    steamFriends.AddFriend(friend.SteamID);
                    Console.WriteLine("Accepted friend request of " + steamFriends.GetFriendPersonaName(friend.SteamID));
                    Thread.Sleep(500);
                    steamFriends.SendChatMessage(friend.SteamID, EChatEntryType.ChatMsg, "Nice to meet you! I am a steam bot");
                }
            }
        }
        static void PlayGame()
        {
            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = new GameID(218620), // or game_id = APPID (218620=Payday2),
            });

            // send it off
            // notice here we're sending this message directly using the SteamClient
            steamClient.Send(playGame);

            // delay a little to give steam some time to establish a GC connection to us
            Thread.Sleep(5000);
        }
        static void NoGame()
        {
            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                //game_id = new GameID(218620), // or game_id = APPID (218620=Payday2),
            });

            // send it off
            // notice here we're sending this message directly using the SteamClient
            steamClient.Send(playGame);

            // delay a little to give steam some time to establish a GC connection to us
            Thread.Sleep(5000);
        }
    }
}
