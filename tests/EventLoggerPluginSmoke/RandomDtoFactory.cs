using System.Reflection;
using Gallop;
using UmamusumeResponseAnalyzer.PluginTesting;

static class RandomDtoFactory
{
    public static object Create(Type type, string salt)
    {
        var value = RandomDtoGenerator.Create(type, salt);
        StabilizeDto(value);
        return value;
    }

    static void StabilizeDto(object value)
    {
        var type = value.GetType();
        StabilizeSingleModeRequest(value, "single_mode_exec_command_request_common", common =>
        {
            SetIntField(common, "command_type", 0);
            SetIntField(common, "command_id", 0);
            SetIntField(common, "current_turn", 1);
        });
        StabilizeSingleModeRequest(value, "single_mode_check_event_request_common", common =>
        {
            SetIntField(common, "choice_number", 0);
        });

        if (type.Name.EndsWith("CheckEventResponse", StringComparison.Ordinal))
            StabilizeCheckEventResponse(value);
        if (type.Name.EndsWith("RaceEndOutResponse", StringComparison.Ordinal))
            StabilizeCheckEventResponse(value);
        if (type.Name.EndsWith("ExecCommandResponse", StringComparison.Ordinal))
            StabilizeExecCommandResponse(value);
        if (type.Name.EndsWith("RaceEndResponse", StringComparison.Ordinal))
            StabilizeRaceEndResponse(value);
        if (type.Name.StartsWith("SingleModeRamen", StringComparison.Ordinal) &&
            type.Name.EndsWith("Response", StringComparison.Ordinal))
        {
            StabilizeRamenResponse(value);
        }
        if (type.Name is "SingleModeLegendCheckEventResponse" or "SingleModeLegendExecCommandResponse")
            StabilizeLegendCheckEventResponse(value);
    }

    static void StabilizeSingleModeRequest(object value, string fieldName, Action<object> stabilize)
    {
        var common = value.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(value);
        if (common is not null)
            stabilize(common);
    }

    static void StabilizeCheckEventResponse(object value)
    {
        var dataField = value.GetType().GetField("data", BindingFlags.Instance | BindingFlags.Public);
        var data = dataField?.GetValue(value);
        if (data is null)
            return;

        SetArrayFieldEmpty(data, "unchecked_event_array");
        SetArrayFieldEmpty(data, "select_index_info_array");

        var charaInfo = (SingleModeChara?)data.GetType().GetField("chara_info", BindingFlags.Instance | BindingFlags.Public)?.GetValue(data);
        if (charaInfo is not null)
        {
            charaInfo.state = 1;
            charaInfo.turn = 1;
            charaInfo.skill_array = [];
            charaInfo.skill_tips_array = [];
            charaInfo.support_card_array = [];
            StabilizeProperFields(charaInfo);
        }
    }

    static void StabilizeExecCommandResponse(object value)
    {
        var data = value.GetType().GetField("data", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value);
        if (data is null)
            return;

        SetArrayFieldEmpty(data, "unchecked_event_array");

        var commandResult = data.GetType().GetField("command_result", BindingFlags.Instance | BindingFlags.Public)?.GetValue(data);
        if (commandResult is not null)
            SetIntField(commandResult, "result_state", 0);

        var charaInfo = (SingleModeChara?)data.GetType().GetField("chara_info", BindingFlags.Instance | BindingFlags.Public)?.GetValue(data);
        if (charaInfo is not null)
        {
            charaInfo.state = 1;
            charaInfo.turn = 1;
            charaInfo.skill_array = [];
            charaInfo.skill_tips_array = [];
            charaInfo.support_card_array = [];
            StabilizeProperFields(charaInfo);
        }
    }

    static void StabilizeProperFields(SingleModeChara charaInfo)
    {
        charaInfo.proper_distance_short = charaInfo.proper_distance_mile =
            charaInfo.proper_distance_middle = charaInfo.proper_distance_long = 1;
        charaInfo.proper_running_style_nige = charaInfo.proper_running_style_oikomi =
            charaInfo.proper_running_style_sashi = charaInfo.proper_running_style_senko = 1;
        charaInfo.proper_ground_turf = charaInfo.proper_ground_dirt = 1;
    }

    static void StabilizeRaceEndResponse(object value)
    {
        var data = value.GetType().GetField("data", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value);
        if (data is null)
            return;

        SetArrayFieldEmpty(data, "race_history");
    }

    static void StabilizeTrainingChara(SingleModeChara charaInfo, int[] trainIds, int[] evaluationIds)
    {
        charaInfo.state = 1;
        charaInfo.playing_state = 1;
        charaInfo.turn = 1;
        charaInfo.motivation = 5;
        charaInfo.vital = 80;
        charaInfo.max_vital = 100;
        charaInfo.speed = charaInfo.stamina = charaInfo.power = charaInfo.guts = charaInfo.wiz = 500;
        charaInfo.max_speed = charaInfo.max_stamina = charaInfo.max_power = charaInfo.max_guts = charaInfo.max_wiz = 1200;
        charaInfo.skill_point = 1200;
        charaInfo.support_card_array = [.. Enumerable.Range(1, 6).Select(position => new SingleModeSupportCard
        {
            position = position,
            support_card_id = 10000 + position
        })];
        charaInfo.evaluation_info_array = [.. evaluationIds.Select(targetId => new EvaluationInfo
        {
            target_id = targetId,
            evaluation = 80
        })];
        charaInfo.training_level_info_array = [.. trainIds.Select(commandId => new TrainingLevelInfo
        {
            command_id = commandId,
            level = 1
        })];
    }

    static void StabilizeRamenResponse(object value)
    {
        var data = value.GetType().GetField("data", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value);
        if (data is null)
            return;

        SetArrayFieldEmpty(data, "unchecked_event_array");

        var charaInfo = (SingleModeChara?)data.GetType().GetField("chara_info", BindingFlags.Instance | BindingFlags.Public)?.GetValue(data);
        if (charaInfo is not null)
        {
            StabilizeTrainingChara(charaInfo, [101, 105, 102, 103, 106], [1, 2, 3, 4, 5, 6]);
        }

        var homeInfo = (SingleModeHomeInfo?)data.GetType().GetField("home_info", BindingFlags.Instance | BindingFlags.Public)?.GetValue(data);
        if (homeInfo is not null)
            StabilizeHomeCommandInfo(homeInfo);
    }

    static void StabilizeHomeCommandInfo(SingleModeHomeInfo homeInfo)
    {
        var trainIds = new[] { 101, 105, 102, 103, 106 };
        homeInfo.command_info_array = [.. trainIds.Select((commandId, index) => new SingleModeCommandInfo
        {
            command_type = 1,
            command_id = commandId,
            is_enable = 1,
            failure_rate = index * 5,
            training_partner_array = [.. Enumerable.Range(1, index % 3 + 1)],
            tips_event_partner_array = [],
            params_inc_dec_info_array = CreateParams(commandId)
        })];
    }

    static void StabilizeLegendCheckEventResponse(object value)
    {
        var data = value.GetType().GetField("data", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value);
        if (data is null)
            return;

        SetArrayFieldEmpty(data, "unchecked_event_array");
        SetArrayFieldEmpty(data, "select_index_info_array");

        var charaInfo = (SingleModeChara?)data.GetType().GetField("chara_info", BindingFlags.Instance | BindingFlags.Public)?.GetValue(data);
        if (charaInfo is not null)
        {
            StabilizeTrainingChara(charaInfo, [101, 105, 102, 103, 106], [1, 2, 3, 4, 5, 6]);
            charaInfo.scenario_id = 10;
        }

        var homeInfo = (SingleModeHomeInfo?)data.GetType().GetField("home_info", BindingFlags.Instance | BindingFlags.Public)?.GetValue(data);
        if (homeInfo is not null)
            StabilizeHomeCommandInfo(homeInfo);
    }

    static SingleModeParamsIncDecInfo[] CreateParams(int baseValue)
    {
        var targetTypes = new[] { 1, 2, 3, 4, 5, 30, 10 };
        return [.. targetTypes.Select((targetType, index) => new SingleModeParamsIncDecInfo
        {
            target_type = targetType,
            value = targetType == 10 ? -10 : baseValue + index
        })];
    }

    static void SetIntField(object value, string fieldName, int fieldValue)
    {
        var field = value.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        if (field is not null && field.FieldType == typeof(int))
            field.SetValue(value, fieldValue);
    }

    static void SetArrayFieldEmpty(object value, string fieldName)
    {
        var field = value.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        if (field?.FieldType.IsArray != true)
            return;

        field.SetValue(value, Array.CreateInstance(field.FieldType.GetElementType()!, 0));
    }

}
