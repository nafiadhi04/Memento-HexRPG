using Godot;
using System;
using Godot.Collections;
using MementoTest.Core; 

public partial class ConfigManager : Node
{
	public static ConfigManager Instance { get; private set; }
	private Dictionary _configData = new Dictionary();
	private const string CONFIG_PATH = "res://game_config.json";

	public override void _Ready()
	{
		if (Instance == null) Instance = this;
		else QueueFree();
		LoadConfig();
	}

	private void LoadConfig()
	{
		if (!FileAccess.FileExists(CONFIG_PATH))
		{
			GD.PrintErr($"[Config] File missing at {CONFIG_PATH}");
			return;
		}

		using var file = FileAccess.Open(CONFIG_PATH, FileAccess.ModeFlags.Read);
		var json = new Json();
		if (json.Parse(file.GetAsText()) == Error.Ok)
		{
			_configData = (Dictionary)json.Data;
			GD.Print("[Config] Loaded Successfully");
		}
	}

	// --- PENGAMBIL DATA BERDASARKAN CLASS ---

	public int GetClassHP(PlayerClassType type)
	{
		return GetClassInt(type.ToString(), "max_hp", 100);
	}

	public int GetClassAP(PlayerClassType type)
	{
		return GetClassInt(type.ToString(), "max_ap", 5);
	}

	public int GetClassRange(PlayerClassType type)
	{
		return GetClassInt(type.ToString(), "attack_range", 1);
	}

	// --- PENGAMBIL DATA SKILL ---

	public int GetSkillDamage(string skillName)
	{
		return GetSkillInt(skillName, "damage", 10);
	}

	public int GetSkillAPCost(string skillName)
	{
		return GetSkillInt(skillName, "ap_cost", 2);
	}

	// --- HELPER PRIVATE ---

	private int GetClassInt(string className, string key, int def)
	{
		if (_configData.ContainsKey("classes"))
		{
			var classes = (Dictionary)_configData["classes"];
			if (classes.ContainsKey(className))
			{
				var stats = (Dictionary)classes[className];
				if (stats.ContainsKey(key)) return (int)stats[key];
			}
		}
		return def;
	}

	private int GetSkillInt(string skillName, string key, int def)
	{
		if (_configData.ContainsKey("skills"))
		{
			var skills = (Dictionary)_configData["skills"];
			if (skills.ContainsKey(skillName))
			{
				var stats = (Dictionary)skills[skillName];
				if (stats.ContainsKey(key)) return (int)stats[key];
			}
		}
		return def;
	}
	// Method untuk mengambil tabel unlock berdasarkan Class
	public Dictionary<int, string> GetClassUnlocks(string className)
	{
		Dictionary<int, string> unlocks = new Dictionary<int, string>();

		// 1. Validasi akses JSON path: classes -> className -> unlock_progression
		if (_configData.ContainsKey("classes") &&
			_configData["classes"].AsGodotDictionary().ContainsKey(className))
		{
			var classData = _configData["classes"].AsGodotDictionary()[className].AsGodotDictionary();

			if (classData.ContainsKey("unlock_progression"))
			{
				var rawUnlocks = classData["unlock_progression"].AsGodotDictionary();

				// 2. Konversi (String Key "5") menjadi (Int Key 5)
				foreach (var key in rawUnlocks.Keys)
				{
					int killCount = key.AsInt32();
					string skillName = rawUnlocks[key].AsString();
					unlocks[killCount] = skillName;
				}
			}
		}

		return unlocks;
	}

	
}