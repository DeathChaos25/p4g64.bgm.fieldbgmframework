using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace p4g64.bgm.fieldbgmframework
{
    public class BgmTable
    {
        [JsonPropertyName("majorId")]
        public int MajorId { get; set; }

        [JsonPropertyName("minorId")]
        public int MinorId { get; set; }

        [JsonPropertyName("dungeonFloor")]
        public short DungeonFloor { get; set; }

        [JsonPropertyName("startMonth")]
        public sbyte StartMonth { get; set; }

        [JsonPropertyName("startDay")]
        public sbyte StartDay { get; set; }

        [JsonPropertyName("endMonth")]
        public sbyte EndMonth { get; set; }

        [JsonPropertyName("endDay")]
        public sbyte EndDay { get; set; }

        [JsonPropertyName("weather")]
        public sbyte Weather { get; set; }

        [JsonPropertyName("time")]
        public sbyte Time { get; set; }

        [JsonPropertyName("flag")]
        public int Flag { get; set; }

        [JsonPropertyName("cueId")]
        public int CueId { get; set; }

        public override string ToString() =>
            $"BGM: field {MajorId:D3}_{MinorId:D3} (Floor {DungeonFloor}) start date {StartMonth}/{StartDay} -- end date {EndMonth}/{EndDay} -- TimeSlot/Weather {Time}/{Weather} -- Cue ID:{CueId}";

        public static List<BgmTable> LoadFromJson(string filePath)
        {
            try
            {
                var jsonString = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<BgmTable>>(jsonString, options)
                    ?? new List<BgmTable>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {filePath}: {ex.Message}");
                return new List<BgmTable>();
            }
        }

        public static List<BgmTable> MergeDistinctKeepingLast(List<List<BgmTable>> allLists)
        {
            // Create dictionary with composite key (all fields except CueId)
            var merged = new Dictionary<BgmKey, BgmTable>();

            foreach (var list in allLists)
            {
                foreach (var bgm in list)
                {
                    var key = new BgmKey(bgm);
                    merged[key] = bgm; // Last occurrence wins
                }
            }

            return merged.Values.ToList();
        }

        public static List<BgmTable> SortBgmTable(List<BgmTable> bgmTable)
        {
            return bgmTable.OrderBy(b => b.MajorId == -1 ? int.MaxValue : b.MajorId)
                .ThenBy(b => b.MinorId == -1 ? int.MaxValue : b.MinorId)
                .ThenBy(b => b.DungeonFloor == -1 ? short.MaxValue : b.DungeonFloor)
                .ThenBy(b => b.Flag == -1 ? int.MaxValue : b.Flag)
                .ThenBy(b => b.StartMonth == -1 ? sbyte.MaxValue : b.StartMonth)
                .ThenBy(b => b.StartDay == -1 ? sbyte.MaxValue : b.StartDay)
                .ThenBy(b => b.EndMonth == -1 ? sbyte.MaxValue : b.EndMonth)
                .ThenBy(b => b.EndDay == -1 ? sbyte.MaxValue : b.EndDay)
                .ThenBy(b => b.Weather == -1 ? sbyte.MaxValue : b.Weather)
                .ThenBy(b => b.Time == -1 ? sbyte.MaxValue : b.Time)
                .ToList();
        }

        // Private helper class for composite key
        private class BgmKey
        {
            public int MajorId { get; }
            public int MinorId { get; }
            public short DungeonFloor { get; }
            public sbyte StartMonth { get; }
            public sbyte StartDay { get; }
            public sbyte EndMonth { get; }
            public sbyte EndDay { get; }
            public sbyte Weather { get; }
            public sbyte Time { get; }
            public int Flag { get; }

            public BgmKey(BgmTable bgm)
            {
                MajorId = bgm.MajorId;
                MinorId = bgm.MinorId;
                DungeonFloor = bgm.DungeonFloor;
                StartMonth = bgm.StartMonth;
                StartDay = bgm.StartDay;
                EndMonth = bgm.EndMonth;
                EndDay = bgm.EndDay;
                Weather = bgm.Weather;
                Time = bgm.Time;
                Flag = bgm.Flag;
            }

            public override bool Equals(object obj) =>
                obj is BgmKey other &&
                MajorId == other.MajorId &&
                MinorId == other.MinorId &&
                DungeonFloor == other.DungeonFloor &&
                StartMonth == other.StartMonth &&
                StartDay == other.StartDay &&
                EndMonth == other.EndMonth &&
                EndDay == other.EndDay &&
                Weather == other.Weather &&
                Time == other.Time &&
                Flag == other.Flag;

            public override int GetHashCode()
            {
                HashCode hash = new();
                hash.Add(MajorId);
                hash.Add(MinorId);
                hash.Add(DungeonFloor);
                hash.Add(StartMonth);
                hash.Add(StartDay);
                hash.Add(EndMonth);
                hash.Add(EndDay);
                hash.Add(Weather);
                hash.Add(Time);
                hash.Add(Flag);
                return hash.ToHashCode();
            }
        }
    }
}
