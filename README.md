# Field BGM Framework for P4G (64-bit)

A framework for adding (or replacing) field bgm to Persona 4 Golden with conditional playback based on in-game states.

## Features

- Replace or add new field BGM
- Conditional playback using game data:
  - Field locations (Major/Minor IDs)
  - Dungeon floors
  - Calendar date ranges
  - Weather conditions
  - Time of day
  - Game progress flags
- Automatic merging of multiple mods' BGM entries
- Detailed logging for debugging

## How to use this with another mod

1. Add this framework as a dependency to your Reloaded-II mod
2. Create a folder named "bgm" inside of your mod
3. Create a "fieldbgm.json" file inside of the bgm folder

## How to configure your BGM entries

Below is a rough explanation of what each value does in the json entries:

`-1` can be used as a "wildcard" in place of any value to 'remove' that condition

### 1. Field MajorId & MinorId
Can be specific by setting both explicit field IDs, or wildcard it to cover a wide range.

**Examples:**
```json
"majorId": 6,
"minorId": 6
```
This will make the BGM only play inside the 2-2 Classroom

```json
"majorId": 6,
"minorId": -1
```
This will make it work on any Field with a major id of 6 regardless of the MinorID (in this example it means the BGM will work on all Yasogami field IDs if they're in major ID 6)

### 2. DungeonFloor
Specific floor in dungeons (a value of -1 means this entry will not apply to dungeon floors)

**Example:**
```json
"dungeonFloor": 5
```
Only plays on the first floor of Yukiko's Castle

### 3. StartMonth/StartDay - EndMonth/EndDay
Date Range for when the BGM will be used

**Example:**
```json
"startMonth": 4,
"startDay": 1,
"endMonth": 4,
"endDay": 31
```
BGM will only play during the month of April

### 4. Weather
**Values:**
- `0` → Clear
- `1` → Rain
- `2` → Cloudy
- `3` → Snowy
- `4` → Foggy
- `5` → Storm
- `6` → ??? Storm
- `7` → Thunder
- `-1` → Any weather

### 5. Time
**Timeslots:**
- `0` → Early Morning
- `1` → Morning
- `2` → Lunch Time
- `3` → Afternoon
- `4` → After School
- `5` → Evening/Night

### 6. Flag
Game progress flags (same as using BIT_CHK flowscript command)

Use `-1` to not have to have a flag enabled to play the BGM

### 7. CueId
The Cue ID of the BGM to play.  


---

## 📄 Sample fieldbgm.json

```json
[
  {
    "majorId": 6,
    "minorId": 6,
    "dungeonFloor": -1,
    "startMonth": 4,
    "startDay": 1,
    "endMonth": 4,
    "endDay": 31,
    "weather": 1,
    "time": -1,
    "flag": -1,
    "cueId": 23
  },
  {
    "majorId": 6,
    "minorId": 6,
    "dungeonFloor": -1,
    "startMonth": 5,
    "startDay": 1,
    "endMonth": 5,
    "endDay": 31,
    "weather": 1,
    "time": -1,
    "flag": -1,
    "cueId": 22
  }
]
```
