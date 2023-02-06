using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using SideLoader;
using System.Collections.Generic;

namespace OutwardQuickbarMod {

    /// <summary>
    /// Main class of quickbar plugin.
    /// See base class docs <see href="https://docs.bepinex.dev/api/BepInEx.BaseUnityPlugin.html">here</see>.
    /// </summary>
    /// <remarks>
    /// Does not require to be manually instantiated.
    /// </remarks>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class QuickbarPlugin : BaseUnityPlugin {

        /// <summary>
        /// Unique Id of the quickbar plugin. Should not change between versions.
        /// See full docs <see href="https://docs.bepinex.dev/api/BepInEx.BepInPlugin.html">here</see>.
        /// </summary>
        public const string GUID = "com.arnoldaz.multiplequickbars";

        /// <summary>
        /// User visible name of the quickbar plugin.
        /// See full docs <see href="https://docs.bepinex.dev/api/BepInEx.BepInPlugin.html">here</see>.
        /// </summary>
        public const string NAME = "Multiple Quickbars";

        /// <summary>
        /// Version of the quickbar plugin.
        /// See full docs <see href="https://docs.bepinex.dev/api/BepInEx.BepInPlugin.html">here</see>.
        /// </summary>
        public const string VERSION = "1.0.0";

        /// <summary>
        /// Overriden base BepInEx logger to allow use outside of this class.
        /// See full logger docs <see href="https://docs.bepinex.dev/api/BepInEx.Logging.ManualLogSource.html">here</see>.
        /// </summary>
        public static new ManualLogSource Logger;

        /// <summary>
        /// Maximum quickbar count for BepInEx options.
        /// </summary>
        private const int MAX_QUICKBAR_COUNT = 20;

        /// <summary>
        /// BepInEx config entry for total quickbar count in-game.
        /// See full docs <see href="https://docs.bepinex.dev/api/BepInEx.Configuration.ConfigEntry-1.html">here</see>.
        /// </summary>
        public static ConfigEntry<int> TotalQuickbarCount;

        /// <summary>
        /// Dictionary of quickbar saves by character UID.
        /// </summary>
        private static readonly Dictionary<UID, QuickbarSaveExtension> m_savedQuickbars = new();

        /// <summary>
        /// Gets keybinding string representation based on index,
        /// which is visible to the user through game settings.
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
        /// Implementation of Unity script Awake function, which is called once after starting the game
        /// and is functionally same as constructor.
        /// Setups all required configs and keybindings for quickbar plugin.
        /// </summary>
        internal void Awake() {
            Logger = base.Logger;

            Logger.LogInfo($"{NAME} v{VERSION} initialized.");

            TotalQuickbarCount = Config.Bind("Settings", "Total quickbar count", 4,
                new ConfigDescription("Total amount of quickbars to choose from", new AcceptableValueRange<int>(1, MAX_QUICKBAR_COUNT)));

            for (int i = 0; i < TotalQuickbarCount.Value; i++)
                CustomKeybindings.AddAction(GetKeybindingName(i), KeybindingsCategory.CustomKeybindings, ControlType.Both);
        }

        /// <summary>
        /// Implementation of Unity script Update function, which happens every frame.
        /// Checks for key presses to switches quickbars if pressed.
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
        /// Switches characters quickbar to a different one from existing list.
        /// </summary>
        /// <param name="character">Characted to switch quickbars for.</param>
        /// <param name="quickbarIndex">Index of the new quickbar.</param>
        private static void SwitchQuickbar(Character character, int quickbarIndex) {
            // First need to save current quickbar data in case it was changed in-game, so it wouldn't get lost.
            var save = GetQuickbarSave(character.UID);
            save.SetQuickbarFromCharacter(character);

            Logger.LogDebug($"Switching to quickbar with index {quickbarIndex}");
            save.SetActiveQuickbar(character, quickbarIndex);
        }
    }
}