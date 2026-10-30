using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using UmamusumeResponseAnalyzer;
using UmamusumeResponseAnalyzer.Entities;
using HostSkillData = UmamusumeResponseAnalyzer.Entities.SkillData;
static class SuccessionDatabase
{
    internal static void InitializeEventLoggerSuccessionDatabase()
        => InitializeDatabase(
            "EventLogger succession",
            [
                new()
                {
                    Name = "测试技能",
                    DisplayName = "测试技能",
                    Id = 100010,
                    GroupId = 10,
                    Rarity = 1,
                    Rate = 1,
                    Propers = [],
                },
                new()
                {
                    Name = "新增技能",
                    DisplayName = "新增技能",
                    Id = 100020,
                    GroupId = 20,
                    Rarity = 1,
                    Rate = 1,
                    Propers = [],
                },
            ],
            [],
            new Dictionary<int, string> { [1_000_001] = "测试白因子★" },
            []);

    static void InitializeDatabase(
        string fixtureName,
        IReadOnlyList<HostSkillData> skills,
        IReadOnlyList<BaseName> names,
        IReadOnlyDictionary<int, string> factorIds,
        IReadOnlyList<int> saddleIds)
    {
        WriteBrotliJson(Database.EVENT_NAME_FILEPATH, new List<Story>());
        WriteBrotliJson(Database.NAMES_FILEPATH, names.ToList(), new() { TypeNameHandling = TypeNameHandling.All });
        WriteBrotliJson(Database.SKILLS_FILEPATH, skills);
        WriteBrotliJson(Database.SKILL_UPGRADE_SPECIALITY_FILEPATH, new List<SkillUpgradeSpeciality>());
        WriteBrotliJson(Database.TALENT_SKILLS_FILEPATH, new Dictionary<int, TalentSkillData[]>());
        WriteBrotliJson(Database.FACTOR_IDS_FILEPATH, factorIds);
        WriteBrotliJson(Database.SADDLE_IDS_FILEPATH, saddleIds);
        WriteBrotliJson(Database.SUCCESSION_RELATION_FILEPATH, new SuccessionRelationTable());

        if (Database.Initialize().GetAwaiter().GetResult() != DatabaseAvailability.Ready)
            throw new InvalidOperationException($"{fixtureName} database fixture did not load atomically.");
    }

    static void WriteBrotliJson<T>(string path, T value, JsonSerializerSettings? settings = null)
    {
        using var file = File.Create(path);
        using var brotli = new BrotliStream(file, CompressionLevel.SmallestSize);
        using var writer = new StreamWriter(brotli, Encoding.UTF8);
        using var json = new JsonTextWriter(writer);
        JsonSerializer.CreateDefault(settings).Serialize(json, value);
    }

}
