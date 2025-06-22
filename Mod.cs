using p4g64.bgm.fieldbgmframework.Configuration;
using p4g64.bgm.fieldbgmframework.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Memory.Streams;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using System.Runtime.InteropServices;
using static p4g64.bgm.fieldbgmframework.Utils;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

#if DEBUG
using System.Diagnostics;
#endif

namespace p4g64.bgm.fieldbgmframework
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private delegate int decideFieldBGM_Delegate();
        private IHook<decideFieldBGM_Delegate> _decideFieldBGM;
        private IHook<decideFieldBGM_Delegate> _decideFieldEnv;

        private static nint currentDayPointerAddr;
        private static nint currentFieldBGMCueIDAddr;
        private static nint CurrentENVCueID;
        private static nint neededForBGMAddr;
        private static nint DungeonFloorPTR;
        private static nint FieldDataStructPTR;
        private static nint BitFieldPTR;

        public unsafe delegate void SomethingForGameBGMDelegate(nint a1, int a2);
        public SomethingForGameBGMDelegate BGMFunc1;
        public SomethingForGameBGMDelegate SndMan_Play;
        public SomethingForGameBGMDelegate SndMan_FadeOut;

        public unsafe delegate byte GetWeatherDelegate(int TotalDay, int TimeSlot);
        public GetWeatherDelegate GetWeather;

        public unsafe delegate bool isDateInRangeDelegate(short startDay, short startMonth, short endDay, short endMonth);
        public isDateInRangeDelegate isDateInRange;

        public unsafe delegate int GetCurrentDungeonFloorDelegate();
        public GetCurrentDungeonFloorDelegate GetCurrentDungeonFloor;

        private static int LastEnvValue = 0;

        public List<List<BgmTable>> allBgmLists = new List<List<BgmTable>>();
        public List<BgmTable> FinalBGMList = new List<BgmTable>();

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;
            _modLoader.OnModLoaderInitialized += OnLoaderInitialized;

            Initialise(_logger, _configuration, _modLoader);

#if DEBUG
            // Attaches debugger in debug mode; ignored in release.
            Debugger.Launch();
#endif

            SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 33 DB", "DecideFieldBGM", address =>
            {
                _decideFieldBGM = _hooks.CreateHook<decideFieldBGM_Delegate>(DecideFieldBGM, address).Activate();
            });

            SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 33 FF", "DecideFieldEnv", address =>
            {
                _decideFieldEnv = _hooks.CreateHook<decideFieldBGM_Delegate>(DecideFieldEnv, address).Activate();
            });

            SigScan("48 8B 0D ?? ?? ?? ?? 4C 8D 1D ?? ?? ?? ?? 49 89 C2", "Get Current Day Ptr", address =>
            {
                currentDayPointerAddr = address;
            });

            SigScan("8B 0D ?? ?? ?? ?? 8B D3 E8 ?? ?? ?? ?? 8B D3 33 C9 E8 ?? ?? ?? ?? 89 1D ?? ?? ?? ??", "Needed For BGM", address =>
            {
                var funcAddress = GetGlobalAddress(address + 2);
                neededForBGMAddr = (nint)funcAddress;
            });

            SigScan("89 1D ?? ?? ?? ?? 0F B6 71 ??", "Field BGM Cue ID PTR", address =>
            {
                var funcAddress = GetGlobalAddress(address + 2);
                currentFieldBGMCueIDAddr = (nint)funcAddress;
            });

            SigScan("66 89 3D ?? ?? ?? ?? 0F B6 71 ??", "Field ENV Cue ID PTR", address =>
            {
                var funcAddress = GetGlobalAddress(address + 3);
                CurrentENVCueID = (nint)funcAddress;
            });

            SigScan("48 8B 05 ?? ?? ?? ?? 33 DB 48 85 C0 75 ?? 8B FB", "Dungeon Floor Data PTR", address =>
            {
                var funcAddress = GetGlobalAddress(address + 3);
                DungeonFloorPTR = (nint)funcAddress;
                Log($"Dungeon Floor Data PTR found at 0x{DungeonFloorPTR:X8}");
            });

            SigScan("48 8B 05 ?? ?? ?? ?? 44 0F B7 C7", "Field Data Struct PTR", address =>
            {
                var funcAddress = GetGlobalAddress(address + 3);
                FieldDataStructPTR = (nint)funcAddress;
                Log($"Field Data Struct PTR found at 0x{FieldDataStructPTR:X8}");
            });


            SigScan("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 48 8B 05 ?? ?? ?? ?? 48 31 E0", "BGMFunc1", address =>
            {
                BGMFunc1 = _hooks.CreateWrapper<SomethingForGameBGMDelegate>(address, out _);
            });

            SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 54 41 56 41 57 48 83 EC 20 48 63 E9", "SndMan_Play", address =>
            {
                SndMan_Play = _hooks.CreateWrapper<SomethingForGameBGMDelegate>(address, out _);
            });

            SigScan("E8 ?? ?? ?? ?? 8B 0D ?? ?? ?? ?? 8D 97 ?? ?? ?? ??", "SndMan_FadeOut", address =>
            {
                var funcAddress = GetGlobalAddress(address + 1);
                SndMan_FadeOut = _hooks.CreateWrapper<SomethingForGameBGMDelegate>((long)funcAddress, out _);
            });

            SigScan("E8 ?? ?? ?? ?? 48 8B 4F ?? 48 8B 89 ?? ?? ?? ?? 85 C0", "GetCurrentDungeonFloor", address =>
            {
                var funcAddress = GetGlobalAddress(address + 1);
                GetCurrentDungeonFloor = _hooks.CreateWrapper<GetCurrentDungeonFloorDelegate>((long)funcAddress, out _);
            });

            SigScan("48 89 5C 24 ?? 57 48 83 EC 20 48 63 F9 89 D3 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 15 ?? ?? ?? ??", "GetWeather", address =>
            {
                GetWeather = _hooks.CreateWrapper<GetWeatherDelegate>(address, out _);
            });

            SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B F9 41 8B F1", "isDateInRange", address =>
            {
                isDateInRange = _hooks.CreateWrapper<isDateInRangeDelegate>(address, out _);
            });

            var memory = Memory.Instance;

            SigScan("75 ?? E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? FF 06", "remove BGM Blocking JNZ", address =>
            {
                // memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90 });
            });

            SigScan("E8 ?? ?? ?? ?? 8B 0D ?? ?? ?? ?? 8D 97 ?? ?? ?? ??", "BGM FadeOut on ENV Play Remove", address =>
            {
                memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 });
            });

            _modLoader.ModLoading += ModLoading;
        }

        public unsafe int DecideFieldBGM()
        {
            int result = _decideFieldBGM.OriginalFunction();

            LogDebug($"Current Field BGM ID is {GetIntFromPointer(currentFieldBGMCueIDAddr)}");

            return result;
        }

        public unsafe int DecideFieldEnv()
        {
            int result = _decideFieldEnv.OriginalFunction();

            int newBGMID = 0;

            short currentDay = GetTotalDays();
            short currentTimeSlot = GetCurrentTimeSlot();
            int currentWeather = GetWeather(currentDay, currentTimeSlot);
            int fieldMajorID = GetFieldMajorID();
            int fieldMinorID = GetFieldMinorID();
            int currentDungeonFloor = GetCurrentDungeonFloor();


            LogDebug($"\nDecideFieldEnv called; logging relevant data\ncurrent total day -> {currentDay:D3};  current time slot -> {currentTimeSlot};  current weather -> {currentWeather}\nField major_minor id -> f{fieldMajorID:D3}_{fieldMinorID:D3}; Current Dungeon Floor -> {currentDungeonFloor}\n");

            foreach (var bgm in FinalBGMList)
            {
                if (bgm.MajorId != fieldMajorID && bgm.MajorId != -1)
                {
                    LogDebug($"Skipping BGM entry for field {bgm.MajorId:D3}_{bgm.MinorId:D3} cueID {bgm.CueId} due to MajorId mismatch: {fieldMajorID} != {bgm.MajorId}");
                    continue;
                }

                if (bgm.MinorId != fieldMinorID && bgm.MinorId != -1)
                {
                    LogDebug($"Skipping BGM entry for field {bgm.MajorId:D3}_{bgm.MinorId:D3} cueID {bgm.CueId} due to MinorId mismatch: {fieldMinorID} != {bgm.MinorId}");
                    continue;
                }

                if (bgm.DungeonFloor != currentDungeonFloor && bgm.DungeonFloor != -1)
                {
                    LogDebug($"Skipping BGM entry for field {bgm.MajorId:D3}_{bgm.MinorId:D3} cueID {bgm.CueId} due to DungeonFloor mismatch: {currentDungeonFloor} != {bgm.DungeonFloor}");
                    continue;
                }

                if (bgm.Flag != -1 && !BitChk(bgm.Flag))
                {
                    LogDebug($"Skipping BGM entry for field {bgm.MajorId:D3}_{bgm.MinorId:D3} cueID {bgm.CueId} due to Flag mismatch: {bgm.Flag} is not set");
                    continue;
                }

                if (!isDateInRange(bgm.StartMonth, bgm.StartDay, bgm.EndMonth, bgm.EndDay))
                {
                    LogDebug($"Skipping BGM entry for field {bgm.MajorId:D3}_{bgm.MinorId:D3} cueID {bgm.CueId} due to date range mismatch: {currentDay}/{currentTimeSlot} not in {bgm.StartMonth}/{bgm.StartDay} -- {bgm.EndMonth}/{bgm.EndDay}");
                    continue;
                }

                if (bgm.Weather != currentWeather && bgm.Weather != -1)
                {
                    LogDebug($"Skipping BGM entry for field {bgm.MajorId:D3}_{bgm.MinorId:D3} cueID {bgm.CueId} due to Weather mismatch: {currentWeather} != {bgm.Weather}");
                    continue;
                }

                if (bgm.Time != currentTimeSlot && bgm.Time != -1)
                {
                    LogDebug($"Skipping BGM entry for field {bgm.MajorId:D3}_{bgm.MinorId:D3} cueID {bgm.CueId} due to TimeSlot mismatch: {currentTimeSlot} != {bgm.Time}");
                    continue;
                }

                newBGMID = bgm.CueId;
                LogDebug($"DecideFieldEnv found matching BGM: {bgm}");
                break;
            }

            if (newBGMID > 0 && newBGMID != GetIntFromPointer(currentFieldBGMCueIDAddr))
            {
                BGMFunc1(neededForBGMAddr, newBGMID);
                SndMan_Play(0, newBGMID);
                ModifyIntThroughPointer(currentFieldBGMCueIDAddr, newBGMID);
            }

            if (newBGMID > 0)
                result = 1;

            if (result > 0 && newBGMID == 0)
            {
                SndMan_FadeOut(0, 0x1E); // keep original functionality
            }

            return result;
        }

        public unsafe short GetTotalDays()
        {
            nint a1 = currentDayPointerAddr;

            int opd = *(int*)(a1 + 3);

            nint newAddress = a1 + (nint)opd + 7;

            short** days = (short**)newAddress;
            return **days;
        }

        public unsafe short GetCurrentTimeSlot()
        {
            nint a1 = currentDayPointerAddr;

            int opd = *(int*)(a1 + 3);

            nint newAddress = a1 + (nint)opd + 7;

            short* daysPointer = *(short**)newAddress;

            return *(daysPointer + 1); // +1 because short is 2 bytes
        }

        public unsafe int GetFieldMajorID()
        {
            nint dataPtr = *(nint*)FieldDataStructPTR;
            return *(int*)dataPtr;
        }

        public unsafe int GetFieldMinorID()
        {
            nint dataPtr = *(nint*)FieldDataStructPTR;
            return *(int*)(dataPtr + 4);
        }

        public unsafe static int GetIntFromPointer(nint address)
        {
            return *((int*)address.ToPointer());
        }

        public unsafe static void ModifyIntThroughPointer(nint address, int newValue)
        {
            *((int*)address.ToPointer()) = newValue;
        }

        public unsafe bool BitChk(int bitIndex)
        {
            int byteOffset = bitIndex / 8;
            int bitPosition = bitIndex % 8;

            byte* bytePtr = (byte*)BitFieldPTR + byteOffset;
            byte targetByte = *bytePtr;

            return (targetByte & (1 << bitPosition)) != 0;
        }

        public unsafe void BitSet(int bitIndex, bool value)
        {
            int byteOffset = bitIndex / 8;
            int bitPosition = bitIndex % 8;

            byte* bytePtr = (byte*)BitFieldPTR + byteOffset;

            if (value)
            {
                *bytePtr |= (byte)(1 << bitPosition);
            }
            else
            {
                *bytePtr &= (byte)~(1 << bitPosition);
            }
        }

        private void OnLoaderInitialized()
        {
            _modLoader.OnModLoaderInitialized -= OnLoaderInitialized;

            var merged = BgmTable.MergeDistinctKeepingLast(allBgmLists);
            FinalBGMList = BgmTable.SortBgmTable(merged);

            LogNoPrefix("Final BGM Table Collected From mods:");
            foreach (var bgm in FinalBGMList)
            {
                LogNoPrefix(bgm.ToString());
            }
        }

        private void ModLoading(IModV1 mod, IModConfigV1 modConfig)
        {
            var modsPath = Path.Combine(_modLoader.GetDirectoryForModId(modConfig.ModId), "bgm");
            if (!Directory.Exists(modsPath))
                return;

            AddFolder(modsPath);
        }

        private void AddFolder(string folder)
        {
            var bgm_table_json = Path.Join(folder, "fieldbgm.json");
            if (File.Exists(bgm_table_json))
            {
                Log($"Loading new Field BGM table from {bgm_table_json}");

                allBgmLists.Add(BgmTable.LoadFromJson(bgm_table_json));
            }
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}