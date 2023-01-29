using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using SideLoader;
using System.Collections.Generic;

namespace OutwardQuickbarMod {

    /// <summary>
    /// Main class of quickbar plugin.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class QuickbarPlugin : BaseUnityPlugin {

        public const string GUID = "quickbar-mod";
        public const string NAME = "Outward Quickbar Mod";
        public const string VERSION = "1.0.0";

        /// <summary>
        /// Overriden BepInEx logger to allow use outside of this class.
        /// </summary>
        public static new ManualLogSource Logger;

        /// <summary>
        /// Maximum quickbar count for BepInEx options.
        /// </summary>
        private const int MAX_QUICKBAR_COUNT = 20;

        /// <summary>
        /// BepInEx config entry for total quickbar count in-game.
        /// </summary>
        public static ConfigEntry<int> TotalQuickbarCount;

        /// <summary>
        /// Saved dictionary of quickbar saves by character UID.
        /// </summary>
        private static readonly Dictionary<UID, QuickbarSaveExtension> m_savedQuickbars = new();

        /// <summary>
        /// Gets keybinding string representation based on index.
        /// </summary>
        /// <param name="quickbarIndex">Quickbar index.</param>
        /// <returns>Keybinding name.</returns>
        private string GetKeybindingName(int quickbarIndex) => $"Open quickbar {quickbarIndex + 1}";

        /// <summary>
        /// Gets globally saved quickbar save. Will create new if not found.
        /// </summary>
        /// <param name="characterUID">Character UID to get quickbar save by.</param>
        /// <returns>New or existing quickbar save.</returns>
        internal static QuickbarSaveExtension GetQuickbarSave(UID characterUID) {
            if (m_savedQuickbars.TryGetValue(characterUID, out var save))
                return save;

            var newSave = new QuickbarSaveExtension();
            m_savedQuickbars.Add(characterUID, newSave);

            return newSave;
        }

        /// <summary>
        /// Implementation of Unity script Awake function.
        /// Setups all required configs and keybindings.
        /// </summary>
        internal void Awake() {
            Logger = base.Logger;

            Logger.LogMessage($"{NAME} v{VERSION} initialized.");

            TotalQuickbarCount = Config.Bind("Settings", "Total quickbar count", 4,
                new ConfigDescription("Total amount of quickbars to choose from", new AcceptableValueRange<int>(1, MAX_QUICKBAR_COUNT)));

            for (int i = 0; i < TotalQuickbarCount.Value; i++)
                CustomKeybindings.AddAction(GetKeybindingName(i), KeybindingsCategory.CustomKeybindings, ControlType.Both);
        }

        /// <summary>
        /// Implementation of Unity script Update function.
        /// Checks for key presses to switch quickbars.
        /// </summary>
        internal void Update() {
            if (MenuManager.Instance.IsInMainMenuScene || NetworkLevelLoader.Instance.IsGameplayPaused)
                return;

            for (int i = 0; i < TotalQuickbarCount.Value; i++) {
                if (!CustomKeybindings.GetKeyDown(GetKeybindingName(i), out int playerId))
                    continue;

                var character = SplitScreenManager.Instance.LocalPlayers[playerId].AssignedCharacter;
                SwitchQuickbar(character, i);
                character.CharacterUI.ShowInfoNotification($"Switched to Quickbar {i + 1}");
                break;
            }
        }

        /// <summary>
        /// Switches characted to new quickbar.
        /// </summary>
        /// <param name="character">Characted to switch quickbars for.</param>
        /// <param name="quickbarIndex">Index of the new quickbar.</param>
        private static void SwitchQuickbar(Character character, int quickbarIndex) {
            var save = GetQuickbarSave(character.UID);
            save.SetQuickbarFromCharacter(character);

            Logger.LogDebug($"Switching to quickbar with index {quickbarIndex}");
            save.SetActiveQuickbar(character, quickbarIndex);
        }
    }
}