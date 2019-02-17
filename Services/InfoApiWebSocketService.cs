﻿using LittleWeebLibrary.EventArguments;
using LittleWeebLibrary.GlobalInterfaces;
using LittleWeebLibrary.Handlers;
using LittleWeebLibrary.Models;
using LittleWeebLibrary.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeebFileNameParserLibrary;

namespace LittleWeebLibrary.Services
{
    public interface IInfoApiWebSocketService
    {
        Task GetCurrentlyAiring();
        Task GetCurrentlyAiring(JObject query);
        Task SearchKitsu(JObject query);
        Task GetAnimeProfile(JObject query);
        Task GetFilesForAnime(JObject query);
        Task GetAnimeEpisodes(JObject query);
        Task AddRule(JObject query);
        Task GetAllGenres(JObject query);
        Task GetAllCategories(JObject query);
        Task GetBotList(JObject query);
    }
    public class InfoApiWebSocketService : IInfoApiWebSocketService
    {     

        private readonly IAnimeProfileHandler AnimeProfileHandler;
        private readonly INiblHandler NiblHandler;
        private readonly IKitsuHandler KitsuHandler;
        private readonly IAnimeRuleHandler AnimeRuleHandler;
        private readonly IWebSocketHandler WebSocketHandler;
        private readonly IDataBaseHandler DataBaseHandler;
        private readonly IDebugHandler DebugHandler;
        private WeebFileNameParser WeebFileNameParser;

        public InfoApiWebSocketService(IWebSocketHandler webSocketHandler, IAnimeProfileHandler infoApiHandler, INiblHandler niblHandler, IKitsuHandler kitsuHandler, IAnimeRuleHandler animeRuleHandler, IDataBaseHandler dataBaseHandler, IDebugHandler debugHandler)
        {
            debugHandler.TraceMessage("Constructor Called.", DebugSource.CONSTRUCTOR, DebugType.ENTRY_EXIT);

            WebSocketHandler = webSocketHandler;
            AnimeProfileHandler = infoApiHandler;
            NiblHandler = niblHandler;
            KitsuHandler = kitsuHandler;
            AnimeRuleHandler = animeRuleHandler;
            DataBaseHandler = dataBaseHandler;
            DebugHandler = debugHandler;

            WeebFileNameParser = new WeebFileNameParser();
        }

        public async Task GetAnimeProfile(JObject query)
        {
            DebugHandler.TraceMessage("GetAnimeProfile Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);
            DebugHandler.TraceMessage("Query: " + query.ToString(), DebugSource.TASK, DebugType.PARAMETERS);

            string id = query.Value<string>("id");
            bool noCache = false;
            int amountPerPage = 40;

            if (query.ContainsKey("no_cache"))
            {
                noCache = query.Value<bool>("no_cache");
            }

            if (query.ContainsKey("amount_per_page"))
            {
                amountPerPage = query.Value<int>("amount_per_page");
            }

            JsonKitsuAnimeInfo info = await AnimeProfileHandler.GetAnimeProfile(id, amountPerPage, noCache);

            await WebSocketHandler.SendMessage(info.ToJson());

        }

        public async Task GetFilesForAnime(JObject query)
        {
            DebugHandler.TraceMessage("GetFilesForAnime Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);
            DebugHandler.TraceMessage("Query: " + query.ToString(), DebugSource.TASK, DebugType.PARAMETERS);

            try
            {
                JArray animeNames = query.Value<JArray>("titles");

                JObject returnresult = new JObject();

                foreach (string title in animeNames)
                {
                    Dictionary<string, string> parsed = WeebFileNameParser.ParseFullString(title);

                    string searchQuery = parsed["MainAnimeTitle"];

                    if (parsed["SubAnimeTitle"].Length > 0)
                    {
                        searchQuery += " " + parsed["SubAnimeTitle"];
                    }

                    JObject result = await NiblHandler.SearchNibl(searchQuery);
                    returnresult.Add(title, result);
                }

                JsonNiblSearchResult resultoreturn = new JsonNiblSearchResult()
                {
                    result = returnresult
                };

                await WebSocketHandler.SendMessage(resultoreturn.ToJson());
            }
            catch (Exception e)
            {
                DebugHandler.TraceMessage("Failed to get files for requested anime(s): " + e.ToString(), DebugSource.TASK, DebugType.WARNING);

                JsonError jsonError = new JsonError
                {
                    errortype = "Exception",
                    errormessage = "failed to get files for requested anime(s)",
                    exception = e.ToString()
                };

                await WebSocketHandler.SendMessage(jsonError.ToString());

            }
            



        }

        public async Task GetCurrentlyAiring()
        {
            DebugHandler.TraceMessage("GetCurrentlyAiring Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);

            try
            {

                JsonCurrentlyAiring currentlyAiring = await AnimeProfileHandler.GetCurrentlyAiring();

                await WebSocketHandler.SendMessage(currentlyAiring.ToJson());

            }
            catch (Exception e)
            {

                DebugHandler.TraceMessage(e.ToString(), DebugSource.TASK, DebugType.ERROR);
                JsonError error = new JsonError()
                {
                    type = "get_currently_airing_error",
                    errormessage = "Could not get currently airing.",
                    errortype = "exception",
                    exception = e.ToString()
                };

                await WebSocketHandler.SendMessage(error.ToJson());
            }
        }

        public async Task GetCurrentlyAiring(JObject query)
        {
            DebugHandler.TraceMessage("GetCurrentlyAiring Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);

            try
            {
                JsonCurrentlyAiring currentlyAiring = new JsonCurrentlyAiring();

                bool noCache = false;
                int bot = 21;
                bool noniblfoundresult = false;
                bool cached = false;

                if (query.ContainsKey("no_cache"))
                {
                    noCache = query.Value<bool>("no_cache");
                }

                if (query.ContainsKey("no_nibl_found_anime"))
                {
                    noniblfoundresult = query.Value<bool>("no_nibl_found_anime");
                }

                if ( query.ContainsKey("bot_id"))
                {
                    bot = query.Value<int>("bot_id");
                }

                if (query.ContainsKey("cached"))
                {
                    cached = query.Value<bool>("cached");
                }

                currentlyAiring = await AnimeProfileHandler.GetCurrentlyAiring(noniblfoundresult, bot, noCache, cached);
                await WebSocketHandler.SendMessage(currentlyAiring.ToJson());

            }
            catch (Exception e)
            {

                DebugHandler.TraceMessage(e.ToString(), DebugSource.TASK, DebugType.ERROR);
                JsonError error = new JsonError()
                {
                    type = "get_currently_airing_error",
                    errormessage = "Could not get currently airing.",
                    errortype = "exception",
                    exception = e.ToString()
                };

                await WebSocketHandler.SendMessage(error.ToJson());
            }
        }

        public async Task GetAnimeEpisodes(JObject query)
        {
            DebugHandler.TraceMessage("GetAnimeEpisodes Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);


            string id = query.Value<string>("id");
            bool noCache = false;
            bool cached = false;
            int amountPerPage = 26;

            if (query.ContainsKey("no_cache"))
            {
                noCache = query.Value<bool>("no_cache");
            }

            if (query.ContainsKey("amount_per_page"))
            {
                amountPerPage = query.Value<int>("amount_per_page");
            }

            if (query.ContainsKey("cached"))
            {
                cached = query.Value<bool>("cached");
            }


            JsonKitsuAnimeInfo info = new JsonKitsuAnimeInfo();
            info = await AnimeProfileHandler.GetAnimeEpisodes(id, amountPerPage, noCache, cached);

            await WebSocketHandler.SendMessage(info.ToJson());
        }

        public async Task SearchKitsu(JObject query)
        {
            DebugHandler.TraceMessage("SearchKitsu Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);
            Dictionary<string, string> listWithQueries = new Dictionary<string, string>();




            if (query.ContainsKey("query")){

                DebugHandler.TraceMessage("Has query parameters: " + query.Value<JArray>("query").ToString(), DebugSource.TASK, DebugType.INFO);
                foreach (JObject val in query.Value<JArray>("query"))
                {
                    DebugHandler.TraceMessage("Adding query for " + val.ToString(), DebugSource.TASK, DebugType.INFO);

                    JToken token = val.First;

                    foreach (KeyValuePair<string, JToken> prop in val)
                    {
                        DebugHandler.TraceMessage("Adding query for " + prop.Key+ " with value: " + prop.Value.ToString(), DebugSource.TASK, DebugType.INFO);
                        listWithQueries.Add(prop.Key, prop.Value.ToString());
                    }
                }
            }


            if (query.ContainsKey("page") && !query.ContainsKey("pages"))
            {
                JsonKistuSearchResult searchResult = await KitsuHandler.SearchAnime(query.Value<string>("search"), listWithQueries, query.Value<int>("page"));

                await WebSocketHandler.SendMessage(searchResult.ToJson());
            }
            else if (query.ContainsKey("page") && query.ContainsKey("pages"))
            {
                JsonKistuSearchResult searchResult = await KitsuHandler.SearchAnime(query.Value<string>("search"), listWithQueries, query.Value<int>("page"), query.Value<int>("pages"));
                await WebSocketHandler.SendMessage(searchResult.ToJson());
            }
            else
            {
                JsonKistuSearchResult searchResult = await KitsuHandler.SearchAnime(query.Value<string>("search"), listWithQueries);
                await WebSocketHandler.SendMessage(searchResult.ToJson());
            }

        }

        public async Task AddRule(JObject query)
        {
            DebugHandler.TraceMessage(" AddRule Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);
            string animeid = query.Value<string>("id");
            JObject rules = query.Value<JObject>("rules");

            bool succes = await AnimeRuleHandler.AddRules(rules, animeid);

            if (succes)
            {
                JsonSuccess jsonSuccess = new JsonSuccess()
                {
                    message = "Succesfully added rules to anime with id: " + animeid
                };

                await WebSocketHandler.SendMessage(jsonSuccess.ToJson());
            }
            else
            {
                JsonError error = new JsonError()
                {
                    type = "add_rule_error",
                    errormessage = "Could not add rule to anime with id: " + animeid,
                    errortype = "warning"
                };

                await WebSocketHandler.SendMessage(error.ToJson());
            }
        }

        public async Task GetAllGenres(JObject query)
        {
            DebugHandler.TraceMessage(" GetAllGenres Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);

            JObject db_result = await DataBaseHandler.GetJObject("kitsu", "genres");

            bool noCache = false;

            if (query.ContainsKey("no_cache"))
            {
                noCache = query.Value<bool>("no_cache");
            }

            if (db_result.Count > 0 && !noCache)
            {
                await WebSocketHandler.SendMessage(db_result.ToString());
            }
            else
            {
                JsonKistuGenres genres = new JsonKistuGenres()
                {
                    result = await KitsuHandler.GetAllGenres()
                };

                if (genres.result.Count > 0)
                {
                    await DataBaseHandler.StoreJObject("kitsu", genres.ToJObject(), "genres");
                    await WebSocketHandler.SendMessage(genres.ToJson());
                }
                else
                {
                    JsonError error = new JsonError()
                    {
                        type = "kitsu_genres_error",
                        errormessage = "Failed to retrieve genres from kitsu.",
                        errortype = "warning"
                    };

                    await WebSocketHandler.SendMessage(error.ToJson());
                }
            }

           
        }

        public async Task GetAllCategories(JObject query)
        {
            DebugHandler.TraceMessage(" GetAllCategories Called.", DebugSource.TASK, DebugType.ENTRY_EXIT);

            JObject db_result = await DataBaseHandler.GetJObject("kitsu", "categories");

            bool noCache = false;

            if (query.ContainsKey("no_cache"))
            {
                noCache = query.Value<bool>("no_cache");
            }


            if (db_result.Count > 0 && !noCache)
            {
                await WebSocketHandler.SendMessage(db_result.ToString());
            }
            else
            {

                JsonKistuCategories categories = new JsonKistuCategories()
                {
                    result = await KitsuHandler.GetAllCategories()
                };

                if (categories.result.Count > 0)
                {
                    await DataBaseHandler.StoreJObject("kitsu", categories.ToJObject(), "categories");
                    await WebSocketHandler.SendMessage(categories.ToJson());
                }
                else
                {
                    JsonError error = new JsonError()
                    {
                        type = "kitsu_categories_error",
                        errormessage = "Failed to retrieve categories from kitsu.",
                        errortype = "warning"
                    };

                    await WebSocketHandler.SendMessage(error.ToJson());
                }
            }
                

        }

        public async Task GetBotList(JObject query)
        {
            JsonNiblBotList botlist = new JsonNiblBotList()
            {
                result = await NiblHandler.GetBotList()
            };
            bool noCache = false;

            if (query.ContainsKey("no_cache"))
            {
                noCache = query.Value<bool>("no_cache");
            }

            JObject db_result = await DataBaseHandler.GetJObject("nibl", "botlist");
            if (db_result.Count > 0 && !noCache)
            {
                await WebSocketHandler.SendMessage(db_result.ToString());
            }
            else
            {
                if (botlist.result.Count > 0)
                {

                    await DataBaseHandler.StoreJObject("nibl", botlist.ToJObject(), "botlist");
                    await WebSocketHandler.SendMessage(botlist.ToJson());
                }
                else
                {
                    JsonError error = new JsonError()
                    {
                        type = "nibl_botlist_error",
                        errormessage = "Failed to retrieve categories from nibl.",
                        errortype = "warning"
                    };

                    await WebSocketHandler.SendMessage(error.ToJson());
                }
            }            
        }
    }
}
