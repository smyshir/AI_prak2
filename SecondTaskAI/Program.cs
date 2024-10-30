﻿using SecondTaskAI.Model;
using SecondTaskAI.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Utils;

namespace SecondTaskAI
{
    class Program
    {
        private const string _path = @"";

        private const bool skipEmptyFriends = true;

        public static string[] friendlvl = { "lvl0.txt", "lvl1.txt", "lvl2.txt" };
        private static List<string> friendlvlFound = new List<string>();
        private static List<string> friendlvlNotFound = new List<string>();

        static bool skip = false;
        static List<VkApiUser> users = new List<VkApiUser>();
        static IDictionary<string, string> parameters = new Dictionary<string, string>();
        private static async Task<List<VkApiUser>> LoadUser(string PathUserFile)
        {
            List<string> list = await txtHelper.ReadFileLinesAsync(PathUserFile);
            List<VkApiUser> users = new List<VkApiUser>();
            int i = -1;
            foreach (var item in list)
            {
                if (item.First() == '#')
                {
                    users.Add(new VkApiUser(item.Replace("#", "")));
                    i++;
                    continue;
                }
                else
                    users[i].friends.Add(new VkApiUser(item));
            }
            return users;
        }
        private static void WriteUserInFile(VkApiUser user, string Path)
        {
            List<string> list = new List<string>();
            txtHelper.WriteFileLines(Path, new List<string>() { "#" + user.me }, true);
            list.Clear();
            foreach (var f in user.friends)
                list.Add(f.me);
            txtHelper.WriteFileLines(Path, list, true);
        }
        private static void WriteUserInFile(List<VkApiUser> users, string Path)
        {
            foreach (var user in users)
                txtHelper.WriteFileLines(Path, new List<string>() { "#" + user.me }, true);
        }
        private static void WriteEdgesInFile(List<VkApiUser> users, string Path)
        {
            char delimiter = CsvHelper.delimiter;
            List<string> Edges = new List<string>();
            foreach (VkApiUser user in users)
            {
                Edges.Clear();
                foreach (VkApiUser friend in user.friends)
                    Edges.Add($"{user.me}{delimiter}{friend.me}");
                txtHelper.WriteFileLines(Path, Edges, true);
            }
        }
        
        private static List<string> GetUSerFromCSV(string path, string columnName = "Ваш ID в VK")
        {
            List<string> ListUser = CsvHelper.ReadCsvFile(path, columnName);
            ListUser = ListUser.Select(item => CorrectorVkId(item)).ToList();
            return ListUser;
        }
        private static async Task Main()
        {
            VkApi api = await Authorization();

            List<long> idVkUser = api.Users.Get(GetUSerFromCSV($@"{_path}\dataF.csv","ID")).Select(n => n.Id).ToList();
            foreach (long nameUserVk in idVkUser)
                users.Add(new VkApiUser(nameUserVk.ToString()));

            if (idVkUser.Count != 0 && !skip) await SenderMessage.SendMessageAsync($"Пользователи успешно получены, их количество {idVkUser.Count}");
            else
            {
                await SenderMessage.SendErrorMessageAsync($"Пользователи не найдены!");
                return;
            }
            await SenderMessage.SendMessageAsync($"Проверка на наличие уровней друзей");

            foreach (string item in friendlvl)
            {
                if (File.Exists($@"{_path}/{item}"))
                {
                    await SenderMessage.SendMessageAsync($"Файл уже был создан {item}", ConsoleColor.Green);
                    friendlvlFound.Add(item);
                }   
                else
                {
                    await SenderMessage.SendErrorMessageAsync($"Файл не найден {item}");
                    friendlvlNotFound.Add(item);
                }
                    
            }

            if(friendlvlNotFound.Count != 0)
            {
                switch (friendlvlNotFound.First())
                {
                    case "lvl0.txt":
                        {
                            WriteUserInFile(users, $@"{_path}/{friendlvlNotFound.First()}");
                            await SenderMessage.SendMessageAsync($"Данные внесены в файл {friendlvlNotFound.First()}", ConsoleColor.Green);
                        }
                        break;
                    case "lvl1.txt":
                        {
                            users = await LoadUser($@"{_path}/{friendlvlFound.Last()}");
                            await SenderMessage.SendMessageAsync($"Загружено: {users.Count}");
                            GetFriends(ref users, api);

                            if(skipEmptyFriends)
                                users = users.Where(n => n.friends.Count > 0).ToList();

                            users.ForEach(n => WriteUserInFile(n, $@"{_path}/{friendlvlNotFound.First()}"));
                            await SenderMessage.SendMessageAsync($"Данные внесены в файл {friendlvlNotFound.First()}", ConsoleColor.Green);
                        }
                        break;
                    case "lvl2.txt":
                        {
                            users = await LoadUser($@"{_path}/{friendlvlFound.Last()}");
                            await SenderMessage.SendMessageAsync($"Загружено: {users.Count}");
                            foreach (VkApiUser u in users)
                            {
                                VkApiUser temp = u;
                                await SenderMessage.SendMessageAsync($"У пользователя: {u.me} число друзей {u.friends.Count}");
                                GetFriends(ref temp.friends, api);

                                if (skipEmptyFriends)
                                    temp.friends = temp.friends.Where(n => n.friends.Count > 0).ToList();

                                temp.friends.ForEach(n => WriteUserInFile(n, $@"{_path}/{friendlvlNotFound.First()}"));
                                await SenderMessage.SendMessageAsync($"Данные внесены в файл {friendlvlNotFound.First()}", ConsoleColor.Green);
                            }
                        }
                        break;
                }  
            }
            if (!File.Exists($@"{_path}/EdgesAll.txt"))
            {
                await SenderMessage.SendMessageAsync($"Начало создание ребер");

                foreach (string u in friendlvlFound)
                {
                    if (u == "lvl0.txt") continue;
                    users = await LoadUser($@"{_path}/{u}");
                    WriteEdgesInFile(users, $@"{_path}/EdgesAll.txt");
                    await SenderMessage.SendMessageAsync($"Для {u} записаны ребра", ConsoleColor.Green);
                }
            }
            await SenderMessage.SendMessageAsync($"Все ребра записаны!", ConsoleColor.Green);

            await SenderMessage.SendMessageAsync($"Удаляем дубли", ConsoleColor.Green);
            await RewoveUnnecessaryEdges($@"{_path}/EdgesAll.txt");
            await SenderMessage.SendMessageAsync($"Все дубли удалены", ConsoleColor.Green);
            Console.ReadKey();
        }
        private static async Task RewoveUnnecessaryEdges(string Path)
        {
            List<string> edges = await txtHelper.ReadFileLinesAsync(Path);

            for(int i = 0;i < edges.Count; i++)
            {
                if (edges[i] == "") continue;
                string temp = edges[i].Split(CsvHelper.delimiter)[1] + $"{CsvHelper.delimiter}" + edges[i].Split(CsvHelper.delimiter)[0];
                int index = edges.FindIndex(n => n == temp);
                if(index == -1) continue;
                edges[index] = "";
            }
            txtHelper.WriteFileLines($@"{_path}/Edges.txt", edges.Where(n => n != "").ToList());
        }
        private static void GetFriends(ref List<VkApiUser> users, VkApi api)
        {
            string parametrs = "";
            int skipCount = 0;
            int stepCount = 10;
            while (skipCount < users.Count)
            {
                List<VkApiUser> tempUser = users.Skip(skipCount).Take(stepCount).ToList();
                
                parametrs = string.Join(", ", tempUser.Select(n => n.me));

                var result = GetFriendsID(ref parametrs, api);
                
                for (int i = skipCount; i - skipCount < tempUser.Count; i++)
                {
                    foreach (var item in result[i - skipCount].ToListOf<string>(n => n))
                        users[i].friends.Add(new VkApiUser(item));
                }
                skipCount += tempUser.Count < stepCount ? tempUser.Count : stepCount;
                Thread.Sleep(310);
            }
            
        }
        private static VkResponse GetFriendsID(ref string ids, VkApi api)
        {
            ids = ids.Remove(ids.Length - 1);
            parameters["userId"] = ids;
            var result = api.Call("execute.getFriendsById", new VkParameters(parameters));
            ids = "";
            return result;
        }
        private static string CorrectorVkId(string item) => item.Trim('@').Replace("https://vk.com/", "");
        private static async Task<VkApi> Authorization()
        {
            string AccessToken = await txtHelper.ReadFileAsync(Authorize.GetAuthorizeDataPath());
            VkApi api = new VkApi();
            api.Authorize(new ApiAuthParams()
            {
                AccessToken = AccessToken,
                Settings = Settings.All
            });
            if (api != null) 
                await SenderMessage.SendMessageAsync("Авторизация прошла успешно");
            else
            {
                await SenderMessage.SendErrorMessageAsync("Ошибка в авторизация");
                skip = true;
            }
            return api;
        }

    }
}
