using Gallop;
using System.Runtime.CompilerServices;
using UmamusumeResponseAnalyzer.Plugin;
using UmamusumeResponseAnalyzer.TerminalGui;

namespace EventLoggerPlugin;

public sealed class EventLoggerPlugin : IPlugin
{
    static string DataDirectory => Path.Combine("PluginData", "EventLoggerPlugin");
    readonly object scenarioGate = new();
    IDisposable? ramenPartProducer;
    IDisposable? pioneerPartProducer;
    IDisposable? festaPartProducer;
    int scenarioCharaId;
    PendingTraining? pendingTraining;

    sealed record PendingTraining(Guid RequestId, int Turn);

    public void Initialize(IPluginContext context)
    {
        const int SuccessionDisplayPriority = 3;

        EventLogger.ConfigureDataDirectory(DataDirectory);
        Directory.CreateDirectory(DataDirectory);
        EventLoggerDisplay.Initialize(context);
        if (context.IsPluginAvailable("RamenScenarioAnalyzer"))
            ramenPartProducer = RegisterRamenPartProducer();
        if (context.IsPluginAvailable("PioneerScenarioAnalyzer"))
            pioneerPartProducer = RegisterPioneerPartProducer();
        if (context.IsPluginAvailable("FestaScenarioAnalyzer"))
            festaPartProducer = RegisterFestaPartProducer();

        context.Analyzers.Register<SingleModeCheckEventResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/check_event")],
            invocation => invocation.Endpoint.Path is "/umamusume/single_mode_pioneer/check_event" or "/umamusume/single_mode_festa/check_event"
                ? ValueTask.CompletedTask : AnalyzeCheckEvent(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeExecCommandResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/exec_command")],
            invocation => invocation.Endpoint.Path is "/umamusume/single_mode_pioneer/exec_command" or "/umamusume/single_mode_festa/exec_command"
                ? ValueTask.CompletedTask : AnalyzeExecCommand(invocation.Payload, invocation.Headers.RequestId),
            priority: -1);
        context.Analyzers.Register<SingleModeRaceEndResponse>(
            AnalyzerKind.Response,
            [
                EndpointPattern.Wildcard("/umamusume/single_mode*/race_end"),
                EndpointPattern.Regex(
                    "^/umamusume/single_mode_(?:arc/arc_race_end|legend/legend_race_end|venus/venus_race_end)$"),
            ],
            invocation => invocation.Endpoint.Path is "/umamusume/single_mode_pioneer/race_end" or "/umamusume/single_mode_festa/race_end"
                ? ValueTask.CompletedTask : AnalyzeRaceEnd(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeLoadResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/load")],
            invocation => invocation.Endpoint.Path is "/umamusume/single_mode_pioneer/load" or "/umamusume/single_mode_festa/load"
                ? ValueTask.CompletedTask : AnalyzeLoad(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeTeamTeamRaceEndOutResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Regex("^/umamusume/single_mode_team/team_race_end(?:_out)?$")],
            invocation => AnalyzeTeamRaceEnd(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModePioneerCheckEventResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Regex("^/umamusume/single_mode_pioneer/(?:change_short_cut|check_event|check_point|continue|finish_claw_crane|gain_skills|pioneer_complete|pioneer_live|planning|race_entry|race_out)$")],
            invocation => AnalyzePioneerCheckEvent(invocation.Payload,
                invocation.Endpoint.Path == "/umamusume/single_mode_pioneer/gain_skills"),
            priority: -1);
        context.Analyzers.Register<SingleModePioneerExecCommandResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Regex("^/umamusume/single_mode_pioneer/(?:exec_command|shima_training_exec)$")],
            invocation => AnalyzePioneerExecCommand(invocation.Payload, invocation.Headers.RequestId),
            priority: -1);
        context.Analyzers.Register<SingleModePioneerLoadResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Exact("/umamusume/single_mode_pioneer/load")],
            invocation => AnalyzePioneerLoad(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModePioneerStartResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Exact("/umamusume/single_mode_pioneer/start")],
            invocation => AnalyzePioneerStart(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModePioneerRaceEndResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Exact("/umamusume/single_mode_pioneer/race_end")],
            invocation => AnalyzePioneerRaceEnd(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeGainSkillsResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/gain_skills")],
            invocation => invocation.Endpoint.Path is "/umamusume/single_mode_pioneer/gain_skills" or "/umamusume/single_mode_festa/gain_skills"
                ? ValueTask.CompletedTask : AnalyzeGainSkills(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeFestaCheckEventResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Regex("^/umamusume/single_mode_festa/(?:change_short_cut|check_event|check_point|continue|finish_claw_crane|gain_skills|attach_advice|festa_live|race_entry|race_out)$")],
            invocation => AnalyzeFestaCheckEvent(invocation.Payload,
                invocation.Endpoint.Path == "/umamusume/single_mode_festa/gain_skills"),
            priority: -1);
        context.Analyzers.Register<SingleModeFestaExecCommandResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Exact("/umamusume/single_mode_festa/exec_command")],
            invocation => AnalyzeResponse(new(invocation.Payload.data.chara_info,
                invocation.Payload.data.unchecked_event_array, null, invocation.Payload.data.home_info),
                invocation.Payload.data.command_result, festaDataSet: invocation.Payload.data.festa_data_set,
                requestId: invocation.Headers.RequestId),
            priority: -1);
        context.Analyzers.Register<SingleModeFestaStartResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Exact("/umamusume/single_mode_festa/start")],
            invocation =>
            {
                var data = invocation.Payload.data.single_mode_start_common;
                return AnalyzeResponse(new(data.chara_info, data.unchecked_event_array, null, data.home_info),
                    festaDataSet: invocation.Payload.data.festa_data_set);
            }, priority: -1);
        context.Analyzers.Register<SingleModeFestaLoadResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Exact("/umamusume/single_mode_festa/load")],
            invocation =>
            {
                var data = invocation.Payload.data.single_mode_load_common;
                return AnalyzeResponse(new(data.chara_info, data.unchecked_event_array, null, data.home_info),
                    raceHistory: data.race_history, festaDataSet: invocation.Payload.data.festa_data_set);
            }, priority: -1);
        context.Analyzers.Register<SingleModeFestaRaceEndResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Exact("/umamusume/single_mode_festa/race_end")],
            invocation => AnalyzeResponse(new(invocation.Payload.data.chara_info, [], null, invocation.Payload.data.home_info),
                raceHistory: invocation.Payload.data.race_history, festaDataSet: invocation.Payload.data.festa_data_set),
            priority: -1);
        context.Analyzers.Register<SingleModeExecCommandRequest>(
            AnalyzerKind.Request,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/exec_command")],
            invocation => ParseTrainingRequest(invocation.Payload, invocation.Headers.RequestId),
            priority: -1);
        context.Analyzers.Register<SingleModePioneerShimaTrainingExecRequest>(
            AnalyzerKind.Request,
            [EndpointPattern.Exact("/umamusume/single_mode_pioneer/shima_training_exec")],
            invocation =>
            {
                EventLogger.MarkAfterTraining(invocation.Payload.current_turn);
                Interlocked.Exchange(ref pendingTraining, new(invocation.Headers.RequestId, invocation.Payload.current_turn));
                EventLogger.RecordPlayerChoice(invocation.Payload.current_turn, 3101);
                return ValueTask.CompletedTask;
            },
            priority: -1);
        context.Analyzers.Register<SingleModeCheckEventResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/check_event")],
            invocation => SuccessionChoiceAnalyzer.Analyze(invocation.Payload),
            priority: SuccessionDisplayPriority);
        context.Analyzers.Register<SingleModeExecCommandResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/exec_command")],
            invocation => SuccessionChoiceAnalyzer.Analyze(invocation.Payload),
            priority: SuccessionDisplayPriority);
        context.Analyzers.Register<SingleModeLoadResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/load")],
            invocation => SuccessionChoiceAnalyzer.Analyze(invocation.Payload),
            priority: SuccessionDisplayPriority);
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref pendingTraining, null);
        try
        {
            ramenPartProducer?.Dispose();
        }
        finally
        {
            try
            {
                pioneerPartProducer?.Dispose();
            }
            finally
            {
                try
                {
                    festaPartProducer?.Dispose();
                }
                finally
                {
                    EventLoggerDisplay.Dispose();
                }
            }
        }
        return ValueTask.CompletedTask;
    }

    ValueTask AnalyzeCheckEvent(SingleModeCheckEventResponse response)
        => AnalyzeResponse(new(
            response.data?.chara_info,
            response.data?.unchecked_event_array,
            response.data?.select_index_info_array,
            response.data?.home_info));

    ValueTask AnalyzeExecCommand(SingleModeExecCommandResponse response, Guid requestId)
        => AnalyzeResponse(
            new(
                response.data?.chara_info,
                response.data?.unchecked_event_array,
                null,
                response.data?.home_info),
            response.data?.command_result, requestId: requestId);

    ValueTask AnalyzeLoad(SingleModeLoadResponse response)
    {
        var common = response.data?.single_mode_load_common;
        return common is null
            ? ValueTask.CompletedTask
            : AnalyzeResponse(
                new(
                    common.chara_info,
                    common.unchecked_event_array,
                    null,
                    common.home_info),
                raceHistory: common.race_history);
    }

    ValueTask AnalyzeRaceEnd(SingleModeRaceEndResponse response)
        => AnalyzeResponse(default, raceHistory: response.data?.race_history);

    ValueTask AnalyzeTeamRaceEnd(SingleModeTeamTeamRaceEndOutResponse response)
        => AnalyzeResponse(new(
            response.data?.chara_info,
            response.data?.unchecked_event_array,
            null));

    ValueTask AnalyzePioneerCheckEvent(SingleModePioneerCheckEventResponse response, bool isGainSkills)
        => AnalyzeResponse(new(
                response.data?.chara_info,
                isGainSkills && response.data is not null ? [] : response.data?.unchecked_event_array,
                response.data?.select_index_info_array,
                response.data?.home_info),
            pioneerDataSet: response.data?.pioneer_data_set);

    ValueTask AnalyzePioneerExecCommand(SingleModePioneerExecCommandResponse response, Guid requestId)
        => AnalyzeResponse(new(
                response.data?.chara_info,
                response.data?.unchecked_event_array,
                null,
                response.data?.home_info),
            response.data?.command_result,
            pioneerDataSet: response.data?.pioneer_data_set, requestId: requestId);

    ValueTask AnalyzePioneerLoad(SingleModePioneerLoadResponse response)
    {
        var common = response.data?.single_mode_load_common;
        return common is null
            ? ValueTask.CompletedTask
            : AnalyzeResponse(new(common.chara_info, common.unchecked_event_array, null, common.home_info),
                raceHistory: common.race_history, pioneerDataSet: response.data?.pioneer_data_set);
    }

    ValueTask AnalyzePioneerRaceEnd(SingleModePioneerRaceEndResponse response)
        => AnalyzeResponse(new(response.data?.chara_info, null, null, response.data?.home_info),
            raceHistory: response.data?.race_history, pioneerDataSet: response.data?.pioneer_data_set);

    ValueTask AnalyzePioneerStart(SingleModePioneerStartResponse response)
    {
        var common = response.data?.single_mode_start_common;
        return common is null
            ? ValueTask.CompletedTask
            : AnalyzeResponse(new(common.chara_info, common.unchecked_event_array, null, common.home_info),
                pioneerDataSet: response.data?.pioneer_data_set);
    }

    ValueTask AnalyzeFestaCheckEvent(SingleModeFestaCheckEventResponse response, bool isGainSkills)
        => AnalyzeResponse(new(response.data.chara_info,
                isGainSkills ? [] : response.data.unchecked_event_array,
                response.data.select_index_info_array, response.data.home_info),
            festaDataSet: response.data.festa_data_set);

    // 学技能响应重发选训练状态且 skill_point 已扣减；commandResult 为 null 时 AnalyzeResponse
    // 不会走 Start 的基线复位，由 Update 差分把扣减累计进 skillPtSpent。
    ValueTask AnalyzeGainSkills(SingleModeGainSkillsResponse response)
    {
        var data = response.data;
        if (data is null)
            return ValueTask.CompletedTask;

        // 响应不含 unchecked_event_array；空数组表示无待处理事件，保证 Update 被调用。
        return AnalyzeResponse(new(
            data.chara_info,
            [],
            null,
            data.home_info));
    }

    ValueTask AnalyzeResponse(
        EventLoggerSnapshot snapshot,
        SingleModeCommandResult? commandResult = null,
        SingleRaceHistory[]? raceHistory = null,
        SingleModePioneerDataSet? pioneerDataSet = null,
        SingleModeFestaDataSet? festaDataSet = null,
        Guid? requestId = null)
    {
        var pioneerWarnings = new List<string>();
        if (snapshot.CharaInfo is { scenario_id: (int)ScenarioType.Pioneer } pioneer &&
            pioneer.turn != 1 && EventLogger.Current.CurrentTurn != pioneer.turn - 1 &&
            EventLogger.Current.CurrentTurn != pioneer.turn)
            pioneerWarnings.Add($"警告：回合不连续，已记录到第 {EventLogger.Current.CurrentTurn} 回合，当前为第 {pioneer.turn} 回合。");
        var sessionReset = PrepareScenarioSession(snapshot);
        var pending = Volatile.Read(ref pendingTraining);
        var trainingTurn = commandResult is not null && pending is not null && pending.RequestId == requestId &&
                           ReferenceEquals(Interlocked.CompareExchange(ref pendingTraining, null, pending), pending)
            ? pending.Turn
            : -1;
        if (commandResult is not null && !sessionReset)
        {
            AnalyzeCommandResult(commandResult, trainingTurn);
        }

        if (commandResult is not null)
        {
            if (EventLogger.Current.IsCapturingVital)
                EventLogger.UpdateWhileCapturing(snapshot);
            else
                EventLogger.Start(snapshot);
        }

        if (snapshot.UncheckedEvents is not null)
        {
            EventLogger.Update(snapshot);
            EnsureScenarioTurn(snapshot);
            EventLogger.RecordScenarioEvents(snapshot.UncheckedEvents);
        }
        else if (snapshot.CharaInfo is { scenario_id: (int)ScenarioType.Pioneer or (int)ScenarioType.Festa })
            EnsureScenarioTurn(snapshot);

        if (raceHistory is not null)
            EventLogger.UpdateRaceHistory(raceHistory);

        TrackScenarioTurn(snapshot, pioneerDataSet, pioneerWarnings, festaDataSet);
        UpdateScenarioDisplay(snapshot, pioneerWarnings);

        return ValueTask.CompletedTask;
    }

    static void AnalyzeCommandResult(SingleModeCommandResult commandResult, int trainingTurn)
    {
        if (commandResult.result_state != 1)
            return;

        EventLoggerDisplay.Notify("训练失败！", UiSeverity.Warning);
        if (trainingTurn >= 0)
            EventLogger.MarkTrainingFailed(trainingTurn);
    }

    ValueTask ParseTrainingRequest(SingleModeExecCommandRequest request, Guid requestId)
    {
        var common = request.single_mode_exec_command_request_common;
        var turn = common.current_turn;
        EventLogger.MarkAfterTraining(turn);
        if (common.command_type != 1)
            return ValueTask.CompletedTask;

        Interlocked.Exchange(ref pendingTraining, new(requestId, turn));
        EventLogger.RecordPlayerChoice(turn, GameGlobal.ToTrainId[common.command_id]);
        return ValueTask.CompletedTask;
    }

    bool PrepareScenarioSession(EventLoggerSnapshot snapshot)
    {
        if (snapshot.CharaInfo is not { } chara ||
            chara.scenario_id is not ((int)ScenarioType.Legend or (int)ScenarioType.Ramen or (int)ScenarioType.Pioneer or (int)ScenarioType.Festa))
            return false;

        lock (scenarioGate)
        {
            var round = EventLogger.Current;
            var reset = scenarioCharaId != chara.single_mode_chara_id ||
                round.Scenario != chara.scenario_id ||
                chara.turn == 1 && round.CurrentTurn != 1 ||
                chara.turn != 1 &&
                round.CurrentTurn != chara.turn - 1 &&
                round.CurrentTurn != chara.turn;
            if (!reset)
                return false;

            scenarioCharaId = chara.single_mode_chara_id;
            Interlocked.Exchange(ref pendingTraining, null);
            EventLogger.ResetAndStartSession(snapshot, isFullGame: chara.turn == 1);
            return true;
        }
    }

    void TrackScenarioTurn(EventLoggerSnapshot snapshot, SingleModePioneerDataSet? pioneerDataSet,
        List<string> pioneerWarnings, SingleModeFestaDataSet? festaDataSet)
    {
        if (snapshot.CharaInfo is not { } chara ||
            chara.scenario_id is not ((int)ScenarioType.Legend or (int)ScenarioType.Ramen or (int)ScenarioType.Pioneer or (int)ScenarioType.Festa) ||
            chara.scenario_id != (int)ScenarioType.Pioneer && snapshot.HomeInfo is null)
            return;

        lock (scenarioGate)
        {
            var round = EventLogger.Current;
            if (round.CurrentTurn != chara.turn)
            {
                EventLogger.EnsureScenarioTurn(chara.scenario_id, chara.turn);
                round = EventLogger.Current;
            }

            var stats = round.NewTurnBuilder(chara.turn);
            stats.isTraining = chara.playing_state == 1;
            stats.motivation = chara.motivation;
            if (chara.scenario_id == (int)ScenarioType.Pioneer)
            {
                if (chara.playing_state == 1 && snapshot.HomeInfo?.command_info_array is { } commands &&
                    pioneerDataSet?.command_info_array is { } scenarioCommands &&
                    GameGlobal.TrainIds.All(trainId =>
                        commands.Any(command => GameGlobal.ToTrainId.TryGetValue(command.command_id, out var id) && id == trainId) &&
                        scenarioCommands.Any(command => GameGlobal.ToTrainId.TryGetValue(command.command_id, out var id) && id == trainId)))
                {
                    stats.fiveTrainStats = CreateTrainingStats(chara, snapshot.HomeInfo, scenarioCommands);
                    UpdatePioneerTrainingLevels(stats, chara, round, pioneerWarnings);
                }
            }
            else if (chara.scenario_id == (int)ScenarioType.Festa)
            {
                if (chara.state is not (2 or 3) && chara.playing_state == 1 &&
                    snapshot.UncheckedEvents is not { Length: > 0 } &&
                    snapshot.HomeInfo?.command_info_array is { } commands && festaDataSet is not null &&
                    GameGlobal.TrainIds.All(trainId => commands.Any(command => command.command_type == 1 &&
                        GameGlobal.ToTrainId.TryGetValue(command.command_id, out var id) && id == trainId)))
                {
                    var selected = SelectFestaTrainingCommands(commands);
                    stats.fiveTrainStats = CreateTrainingStats(chara, new() { command_info_array = selected },
                        festaCommands: festaDataSet.command_info_array);
                    stats.trainLevel = selected.Select(command =>
                    {
                        var levels = chara.training_level_info_array.Where(level => level.command_id == command.command_id).ToArray();
                        if (levels.Length == 0 && command.command_id is not (>= 5101 and <= 5105))
                            levels = [.. chara.training_level_info_array.Where(level => level.command_id == GameGlobal.ToTrainId[command.command_id])];
                        return levels.Single().level;
                    }).ToArray();
                }
            }
            else if (snapshot.HomeInfo is { } homeInfo)
            {
                stats.fiveTrainStats = CreateTrainingStats(chara, homeInfo);
                stats.trainLevel = CreateTrainingLevels(chara);
                if (chara.scenario_id == (int)ScenarioType.Legend)
                    UpdateLegendState(stats, chara, homeInfo);
            }
            EventLogger.CommitScenarioTurn(chara.scenario_id, chara.turn, stats);
        }
    }

    static void EnsureScenarioTurn(EventLoggerSnapshot snapshot)
    {
        if (snapshot.CharaInfo is { } chara &&
            chara.scenario_id is (int)ScenarioType.Legend or (int)ScenarioType.Ramen or (int)ScenarioType.Pioneer or (int)ScenarioType.Festa)
            EventLogger.EnsureScenarioTurn(chara.scenario_id, chara.turn);
    }

    static SingleModeCommandInfo[] SelectFestaTrainingCommands(SingleModeCommandInfo[] commands)
        => GameGlobal.TrainIds.Select(trainId =>
        {
            var candidates = commands.Where(command => command.command_type == 1 &&
                GameGlobal.ToTrainId.TryGetValue(command.command_id, out var id) && id == trainId)
                .GroupBy(command => command.command_id is >= 5101 and <= 5105)
                .Select(group => group.SingleOrDefault(command => command.is_enable == 1) ?? group.Single())
                .ToArray();
            return candidates.SingleOrDefault(command => command.command_id is >= 5101 and <= 5105 && command.is_enable == 1)
                ?? candidates.SingleOrDefault(command => command.command_id is not (>= 5101 and <= 5105))
                ?? candidates.Single();
        }).ToArray();

    static TrainStats[] CreateTrainingStats(SingleModeChara chara, SingleModeHomeInfo homeInfo,
        SingleModePioneerCommandInfo[]? pioneerCommands = null, SingleModeFestaCommandInfo[]? festaCommands = null)
    {
        var currentStats = new[] { chara.speed, chara.stamina, chara.power, chara.guts, chara.wiz };
        var result = new TrainStats[GameGlobal.TrainIds.Length];
        for (var i = 0; i < GameGlobal.TrainIds.Length; i++)
        {
            var trainId = GameGlobal.TrainIds[i];
            var commands = homeInfo.command_info_array
                .Where(command => GameGlobal.ToTrainId.TryGetValue(command.command_id, out var id) && id == trainId)
                .ToArray();
            var values = new int[31];
            foreach (var command in commands)
                foreach (var parameter in command.params_inc_dec_info_array ?? [])
                    if (parameter.target_type is >= 1 and <= 5 or 10 or 30)
                        values[parameter.target_type] += parameter.value;

            var baseGains = Enumerable.Range(0, 5)
                .Select(index =>
                    ScoreUtils.ReviseOver1200(currentStats[index] + values[index + 1]) -
                    ScoreUtils.ReviseOver1200(currentStats[index]))
                .ToArray();
            var basePtGain = values[30];
            if (pioneerCommands is not null)
                foreach (var parameter in pioneerCommands.Single(command =>
                             GameGlobal.ToTrainId.TryGetValue(command.command_id, out var id) && id == trainId).params_inc_dec_info_array)
                    if (parameter.target_type is >= 1 and <= 5 or 30)
                        values[parameter.target_type] += parameter.value;
            if (festaCommands is not null)
                foreach (var parameter in festaCommands.SingleOrDefault(command =>
                             command.command_type == 1 && command.command_id == commands.Single().command_id)?.params_inc_dec_info_array ?? [])
                    if (parameter.target_type is >= 1 and <= 5 or 10 or 30)
                        values[parameter.target_type] += parameter.value;
            var gains = Enumerable.Range(0, 5)
                .Select(index =>
                    ScoreUtils.ReviseOver1200(currentStats[index] + values[index + 1]) -
                    ScoreUtils.ReviseOver1200(currentStats[index]))
                .ToArray();
            var failureRate = commands
                .FirstOrDefault(command => command.command_id == trainId)?.failure_rate
                ?? commands.FirstOrDefault()?.failure_rate
                ?? 0;
            result[i] = new()
            {
                FiveValueGain = gains,
                PtGain = values[30],
                FiveValueGainNonScenario = baseGains,
                PtGainNonScenario = basePtGain,
                VitalGain = Math.Clamp(values[10], -chara.vital, chara.max_vital - chara.vital),
                FailureRate = failureRate,
            };
        }
        return result;
    }

    static int[] CreateTrainingLevels(SingleModeChara chara)
    {
        var levels = new[] { 1, 1, 1, 1, 1 };
        foreach (var training in chara.training_level_info_array ?? [])
            // 仅抓取基础训练等级 (101,105,102,103,106)
            if (training.command_id < 110 && GameGlobal.ToTrainIndex.TryGetValue(training.command_id, out var index))
                levels[index] = training.level;
        return levels;
    }

    static void UpdatePioneerTrainingLevels(TurnStats stats, SingleModeChara chara,
        EventLoggerRoundSnapshot round, List<string> warnings)
    {
        for (var i = 0; i < GameGlobal.TrainIds.Length; i++)
        {
            if (chara.turn == 1)
            {
                stats.trainLevel[i] = 1;
                stats.trainLevelCount[i] = 0;
                continue;
            }

            var trainId = GameGlobal.TrainIds[i];
            var previous = round.Turns[chara.turn - 1];
            var level = previous?.TrainLevel[i] ?? 1;
            var count = previous?.TrainLevelCount[i] ?? 0;
            if (previous is not null && previous.PlayerChoice == trainId && !previous.IsTrainingFailed &&
                chara.turn - 1 is not (>= 37 and <= 40 or >= 61 and <= 64))
                count++;
            if (chara.turn is 25 or 37 or 49)
                count += 4;
            level += count / 4;
            count %= 4;
            if (level >= 5)
            {
                level = 5;
                count = 0;
            }

            var actualLevel = chara.training_level_info_array.First(training =>
                GameGlobal.ToTrainId.TryGetValue(training.command_id, out var id) && id == trainId).level;
            if (level != actualLevel)
            {
                warnings.Add($"警告：训练等级预测错误，预测{trainId}为lv{level}(+{count})，实际为lv{actualLevel}");
                level = actualLevel;
                count = 0;
            }
            stats.trainLevel[i] = level;
            stats.trainLevelCount[i] = count;
        }
    }

    static void UpdateLegendState(
        TurnStats stats,
        SingleModeChara chara,
        SingleModeHomeInfo homeInfo)
    {
        stats.legend_friendAtTrain = new bool[5];
        var groupCardPositions = chara.support_card_array
            .Where(card => card.support_card_id == 30241)
            .Select(card => card.position)
            .ToHashSet();
        foreach (var command in homeInfo.command_info_array)
            if (GameGlobal.ToTrainIndex.TryGetValue(command.command_id, out var index) &&
                (command.training_partner_array ?? []).Any(groupCardPositions.Contains))
                stats.legend_friendAtTrain[index] = true;
        stats.legend_isEffect104 = (chara.chara_effect_id_array ?? []).Contains(104);
        stats.legend_friendClickEventCountConcerned = !stats.legend_isEffect104;
    }

    void UpdateScenarioDisplay(EventLoggerSnapshot source, IReadOnlyList<string> pioneerWarnings)
    {
        if (source.CharaInfo is not { } chara)
            return;

        if (chara.scenario_id == (int)ScenarioType.Ramen &&
            ramenPartProducer is { } ramenProducer)
        {
            UpdateRamenPart(
                ramenProducer,
                EventLoggerScenarioDisplayPart.Capture(
                    chara.scenario_id,
                    chara.single_mode_chara_id,
                    chara.turn));
        }
        else if (chara.scenario_id == (int)ScenarioType.Pioneer &&
                 pioneerPartProducer is { } pioneerProducer)
        {
            UpdatePioneerPart(pioneerProducer,
                EventLoggerScenarioDisplayPart.Capture(chara.scenario_id, chara.single_mode_chara_id, chara.turn),
                pioneerWarnings);
        }
        else if (chara.scenario_id == (int)ScenarioType.Festa && festaPartProducer is { } festaProducer)
        {
            UpdateFestaPart(festaProducer,
                EventLoggerScenarioDisplayPart.Capture(chara.scenario_id, chara.single_mode_chara_id, chara.turn));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IDisposable RegisterRamenPartProducer() => RamenScenarioDisplayBridge.Register();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IDisposable RegisterPioneerPartProducer() => PioneerScenarioDisplayBridge.Register();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IDisposable RegisterFestaPartProducer() => FestaScenarioDisplayBridge.Register();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void UpdateRamenPart(
        IDisposable producer,
        EventLoggerScenarioDisplayPart part)
        => RamenScenarioDisplayBridge.Update(producer, part);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void UpdatePioneerPart(IDisposable producer, EventLoggerScenarioDisplayPart part, IReadOnlyList<string> warnings)
        => PioneerScenarioDisplayBridge.Update(producer, part, warnings);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void UpdateFestaPart(IDisposable producer, EventLoggerScenarioDisplayPart part)
        => FestaScenarioDisplayBridge.Update(producer, part);
}
