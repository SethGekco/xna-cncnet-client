using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// A single player setup preset: the local player's row settings and the
    /// AI roster (difficulty, side, color, team and start per AI), both in
    /// <see cref="PlayerInfo"/> string format. Deliberately separate from
    /// <see cref="GameOptionPreset"/>, which stores the game option check
    /// boxes and dropdowns - a lineup can be re-enacted without touching the
    /// game options, and vice versa.
    /// </summary>
    public class PlayerPreset
    {
        public PlayerPreset(string profileName)
        {
            ProfileName = profileName;

            if (ProfileName.Contains('[') || ProfileName.Contains(']'))
                throw new ArgumentException("Player preset name cannot contain the [] characters.");
        }

        /// <summary>
        /// Checks if a specific name is valid for the name of a player preset.
        /// Returns null if the name is valid, an error message otherwise.
        /// </summary>
        public static string IsNameValid(string name)
        {
            if (name.Contains('[') || name.Contains(']'))
                return "Player preset name cannot contain the [] characters.";

            return null;
        }

        public string ProfileName { get; }

        private string humanPlayerValues = string.Empty;
        private List<string> aiPlayerValues = new List<string>();

        public void SetPlayerValues(string humanPlayer, List<string> aiPlayers)
        {
            humanPlayerValues = humanPlayer ?? string.Empty;
            aiPlayerValues = aiPlayers ?? new List<string>();
        }

        public string GetHumanPlayerValues() => humanPlayerValues;

        public List<string> GetAIPlayerValues() => new List<string>(aiPlayerValues);

        public void Read(IniSection section)
        {
            // PlayerInfo strings are comma-separated internally, so each
            // player gets its own key, like in SkirmishSettings.ini.
            humanPlayerValues = section.GetStringValue("HumanPlayer", string.Empty);

            aiPlayerValues.Clear();
            for (int i = 0; section.KeyExists("AIPlayer" + i); i++)
                aiPlayerValues.Add(section.GetStringValue("AIPlayer" + i, string.Empty));
        }

        public void Write(IniSection section)
        {
            if (!string.IsNullOrEmpty(humanPlayerValues))
                section.SetStringValue("HumanPlayer", humanPlayerValues);

            for (int i = 0; i < aiPlayerValues.Count; i++)
                section.SetStringValue("AIPlayer" + i, aiPlayerValues[i]);
        }
    }

    /// <summary>
    /// Handles player setup presets. Mirrors <see cref="GameOptionPresets"/>
    /// but stores to its own file, so the two preset collections stay
    /// independent.
    /// </summary>
    public class PlayerPresets
    {
        private const string IniFileName = "PlayerPresets.ini";
        private const string PresetDefinitionsSectionName = "Presets";

        private PlayerPresets() { }

        private static PlayerPresets _instance;
        public static PlayerPresets Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PlayerPresets();

                return _instance;
            }
        }

        private IniFile playerPresetsIni;
        private Dictionary<string, PlayerPreset> presets;

        public PlayerPreset GetPreset(string name)
        {
            LoadIniIfNotInitialized();

            if (presets.TryGetValue(name, out PlayerPreset value))
                return value;

            return null;
        }

        public List<string> GetPresetNames()
        {
            LoadIniIfNotInitialized();

            return presets.Keys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .ToList();
        }

        public void AddPreset(PlayerPreset preset)
        {
            LoadIniIfNotInitialized();

            presets[preset.ProfileName] = preset;
            WriteIni();
        }

        public void DeletePreset(string name)
        {
            LoadIniIfNotInitialized();

            if (!presets.ContainsKey(name))
                return;

            presets.Remove(name);
            WriteIni();
        }

        private void LoadIniIfNotInitialized()
        {
            if (playerPresetsIni == null)
                LoadIni();
        }

        private void LoadIni()
        {
            playerPresetsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, IniFileName));
            presets = new Dictionary<string, PlayerPreset>();

            IniSection presetsDefinitions = playerPresetsIni.GetSection(PresetDefinitionsSectionName);
            if (presetsDefinitions == null)
                return;

            foreach (var kvp in presetsDefinitions.Keys)
            {
                if (!presets.ContainsKey(kvp.Value))
                {
                    IniSection presetSection = playerPresetsIni.GetSection(kvp.Value);
                    if (presetSection == null)
                        continue;

                    var preset = new PlayerPreset(kvp.Value);
                    preset.Read(presetSection);
                    presets[kvp.Value] = preset;
                }
            }
        }

        private void WriteIni()
        {
            playerPresetsIni = new IniFile();
            int i = 0;
            var definitionsSection = new IniSection(PresetDefinitionsSectionName);
            playerPresetsIni.AddSection(definitionsSection);
            foreach (var kvp in presets)
            {
                definitionsSection.SetStringValue(i.ToString(), kvp.Value.ProfileName);
                var presetSection = new IniSection(kvp.Value.ProfileName);
                kvp.Value.Write(presetSection);
                playerPresetsIni.AddSection(presetSection);
                i++;
            }

            playerPresetsIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, IniFileName));
        }
    }
}
