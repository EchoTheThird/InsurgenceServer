﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsurgenceServer.Database;
using InsurgenceServer.GTS;
using InsurgenceServer.Logger;
using InsurgenceServer.Trades;

namespace InsurgenceServer.WonderTrade
{
    public static class WonderTradeHandler
    {
        public static SynchronizedCollection<WonderTradeHolder> List = new SynchronizedCollection<WonderTradeHolder>();

        public static async Task Loop()
        {
            while (Data.Running)
            {
                foreach (var trade in List.ToList())
                {
                    try
                    {
                        if ((DateTime.UtcNow - trade.Time).TotalSeconds >= 60)
                        {
                            //Delete from list, send timeout message back to client
                            List.Remove(trade);
                            await trade.Client.SendMessage("<WTRESULT result=1 user=nil pkmn=nil>");
                        }
                        if (trade.Client == null || !trade.Client.Connected)
                        {
                            //Delete from list
                            List.Remove(trade);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                try
                {
                    while (List.Count >= 2)
                    {
                        //Get 2 random entries
                        var r = new Random();
                        var i1 = r.Next(0, List.Count);
                        var i2 = r.Next(0, List.Count);
                        //We don't want two the same entries
                        while(i1 == i2)
                        {
                            //Break this if we don't have 2 entries anymore
                            if (List.Count < 2)
                                continue;
                            i2 = r.Next(0, List.Count);
                        }
                        var trade1 = List[i1];
                        var trade2 = List[i2];
                        //If either of the clients is not connected anymore, try looping again
                        if (!trade1.Client.Connected || !trade2.Client.Connected)
                            continue;
                        //If two ips are the same and neither is an admin, try looping again
                        if ((Equals(trade1.Client.Ip, trade2.Client.Ip)) && (!trade1.Client.Admin || !trade2.Client.Admin))
                            continue;

                        //Execute trade, remove entries
                        await ExecuteTrade(trade1.Client, trade2.Client, trade1.Pokemon, trade2.Pokemon);
                        List.Remove(trade1);
                        List.Remove(trade2);
                    }
                }
                catch
                {
                    // ignored
                }
                await Task.Delay(2000);
            }
        }

        public static async Task ExecuteTrade(Client client1, Client client2, GTS.GamePokemon pkmn1, GTS.GamePokemon pkmn2)
        {
            var jsonstring1 = JsonConvert.SerializeObject(pkmn1);
            var jsonstring2 = JsonConvert.SerializeObject(pkmn2);

            try { await DbTradelog.LogWonderTrade(client1.Username, jsonstring1); }
            catch (Exception e) { Console.WriteLine("Error when logging WT: " + e); }
            try { await DbTradelog.LogWonderTrade(client2.Username, jsonstring2); }
            catch (Exception e) { Console.WriteLine("Error when logging WT: " + e); }

            var encoded1 = Utilities.Encoding.Base64Encode(jsonstring1);
            var encoded2 = Utilities.Encoding.Base64Encode(jsonstring2);

            await client1.SendMessage($"<WTRESULT result=2 user={client2.Username} pkmn={encoded2}>");
            await client2.SendMessage($"<WTRESULT result=2 user={client1.Username} pkmn={encoded1}>");
        }

        public static async Task DeleteFromClient(Client c)
        {
            foreach (var item in List.ToArray())
            {
                try
                {
                    if (item.Client == c) List.Remove(item);
                }
                catch (Exception e)
                {
                    ErrorLog.Log(e);
                }
            }
        }

        public static async Task AddTrade(Client client, string encodedPkmns)
        {
            if (await DbUserChecks.UserBanned(client.Username))
            {
                await client.SendMessage("<WTRESULT reult=0 user=nil pkmn=nil>");
                return;
            }
            if (List.Count(x => x.Client == client) > 0)
            {
                return;
            }

            GamePokemon pkmn;
            try
            {
                pkmn = JsonConvert.DeserializeObject<GTS.GamePokemon>(
                    Utilities.Encoding.Base64Decode(encodedPkmns));
            }
            catch
            {
                await client.SendMessage("<WTRESULT result=1 user=nil pkmn=nil>");
                throw;
            }
            if (!await TradeValidator.IsPokemonValid(pkmn, client.UserId))
            {
                await client.SendMessage("<WTRESULT reult=0 user=nil pkmn=nil>");
                return;
            }
            List.Add(new WonderTradeHolder(client, pkmn));
        }

        public static async Task CancelTrade(Client client)
        {
            await DeleteFromClient(client);
        }
    }
}
