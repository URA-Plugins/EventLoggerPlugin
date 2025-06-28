﻿using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.IO.Compression;
using UmamusumeResponseAnalyzer;
using UmamusumeResponseAnalyzer.Plugin;

[assembly: LoadInHostContext]
namespace EventLoggerPlugin
{
    public class EventLoggerPlugin : IPlugin
    {
        public Version Version => new(1, 0, 0);

        public string Name => "EventLoggerPlugin";

        public string Author => "xulai1001";
        public async Task UpdatePlugin(ProgressContext ctx)
        {
            var progress = ctx.AddTask($"[EventLoggerPlugin] Update");

            using var client = new HttpClient();
            using var resp = await client.GetAsync($"https://api.github.com/repos/URA-Plugins/{Name}/releases/latest");
            var json = await resp.Content.ReadAsStringAsync();
            var jo = JObject.Parse(json);

            var isLatest = ("v" + Version.ToString()).Equals("v" + jo["tag_name"]?.ToString());
            if (isLatest)
            {
                progress.Increment(progress.MaxValue);
                progress.StopTask();
                return;
            }
            progress.Increment(25);

            using var msg = await client.GetAsync(jo["assets"][0]["browser_download_url"].ToString(), HttpCompletionOption.ResponseHeadersRead);
            using var stream = await msg.Content.ReadAsStreamAsync();
            var buffer = new byte[8192];
            while (true)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0)
                    break;
                progress.Increment(read / msg.Content.Headers.ContentLength ?? 1 * 0.5);
            }
            using var archive = new ZipArchive(stream);
            archive.ExtractToDirectory(Path.Combine("Plugins", Name), true);
            progress.Increment(25);

            progress.StopTask();
        }

        [Analyzer(priority: -1)]
        public void StartEventLogger(JObject jo)
        {
            if (!jo.HasCharaInfo()) return;
            if (jo["data"] is null || jo["data"] is not JObject data) return;

            if (data["command_result"] is JObject command_result) // 训练结果
            {
                if (command_result["result_state"].ToInt() == 1) // 训练失败
                {
                    AnsiConsole.MarkupLine("训练失败！");
                    if (GameStats.stats[GameStats.currentTurn] != null)
                        GameStats.stats[GameStats.currentTurn].isTrainingFailed = true;
                }
                EventLogger.Start(jo.ToObject<Gallop.SingleModeCheckEventResponse>()); // 开始记录事件，跳过从上一次调用update到这里的所有事件和训练
            }

            if (data.ContainsKey("unchecked_event_array"))
            {
                var @event = jo.ToObject<Gallop.SingleModeCheckEventResponse>();
                if (@event != null)
                {
                    // 这时当前事件还没有生效，先显示上一个事件的收益
                    EventLogger.Update(@event);
                    foreach (var i in @event.data.unchecked_event_array)
                    {
                        if (GameStats.stats[GameStats.currentTurn] != null)
                        {
                            if (i.story_id == 830137001)//第一次点击女神
                            {
                                GameStats.stats[GameStats.currentTurn].venus_isVenusCountConcerned = false;
                            }

                            if (i.story_id == 830137003)//女神三选一事件
                            {
                                GameStats.stats[GameStats.currentTurn].venus_venusEvent = true;
                            }


                            if (i.story_id == 400006112)//ss训练
                            {
                                GameStats.stats[GameStats.currentTurn].larc_playerChoiceSS = true;
                            }

                            if (i.story_id == 809043002)//佐岳启动
                            {
                                GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = 5;
                            }

                            if (i.story_id == 809043003)//佐岳充电
                            {
                                var suc = i.event_contents_info.choice_array[0].select_index;
                                var eventType = 0;
                                if (suc == 1)//加心情
                                {
                                    eventType = 2;
                                }
                                else if (suc == 2)//不加心情
                                {
                                    eventType = 1;
                                }

                                GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = eventType;
                            }
                            if (i.story_id == 400006115)//远征佐岳加pt
                            {
                                GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = 4;
                            }
                            if (i.story_id == 809044002) // 凉花出门
                            {
                                GameStats.stats[GameStats.currentTurn].uaf_friendEvent = 5;
                            }
                            if (i.story_id == 809044003) // 凉花加体力
                            {
                                GameStats.stats[GameStats.currentTurn].uaf_friendEvent = 1;
                            }
                        }
                    }
                }
            }
        }
        [Analyzer(false, -1)]
        public void ParseChoiceRequest(JObject jo)
        {
            if (jo["choice_number"].ToInt() > 0)  // 玩家点击了事件
            {
                EventLogger.UpdatePlayerChoice(jo.ToObject<Gallop.SingleModeChoiceRequest>());
            }
        }
        [Analyzer(false, -1)]
        public void ParseTrainingRequest(JObject jo)
        {
            if (jo["command_type"].ToInt() == 1) //玩家点击了训练
            {
                var @event = jo.ToObject<Gallop.SingleModeExecCommandRequest>();
                var turn = @event.current_turn;
                if (GameStats.currentTurn != 0 && turn != GameStats.currentTurn) return;
                var trainingId = GameGlobal.ToTrainId[@event.command_id];
                if (GameStats.stats[turn] != null)
                    GameStats.stats[turn].playerChoice = trainingId;
            }
        }
    }
}
