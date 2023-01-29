﻿using SideLoader.SaveData;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace OutwardQuickbarMod {

    /// <summary>
    /// Serializable instance with quickbar mod saveable information.
    /// </summary>
    /// <remarks>
    /// Must be public to be serialized.
    /// </remarks>
    [Serializable]
    public class QuickbarSaveExtension : PlayerSaveExtension {

        /// <summary>
        /// Index of currently active quickbar.
        /// </summary>
        [XmlElement]
        public int ActiveQuickbarIndex = 0;

        /// <summary>
        /// Main quickbar data.
        /// Each list entry contains single quickbar data with each quickslot separated with <see cref="m_quickbarDataSeparator"/>.
        /// </summary>
        [XmlArray]
        public List<string> QuickbarData = new();
        
        /// <summary>
        /// Character to separate data in single string from multiple quickslots.
        /// Can't be semicolon because it is used internally.
        /// </summary>
        private readonly char m_quickbarDataSeparator = '|';

        /// <summary>
        /// Overrides current instance serializable data from the other instance.
        /// </summary>
        /// <param name="overrideSave">Other instance to override data from.</param>
        public void OverrideSerializableProperties(QuickbarSaveExtension overrideSave) {
            ActiveQuickbarIndex = overrideSave.ActiveQuickbarIndex;
            QuickbarData = new List<string>(overrideSave.QuickbarData);
        }

        /// <summary>
        /// Adds empty strings to <see cref="QuickbarData"/> until it is filled to total quickbar count
        /// to not get out of range exception if save file is empty.
        /// This can't be done on constructor, since SideLoader does not overrides list property but adds to it.
        /// </summary>
        private void FixQuickbarDataSize() {
            var totalQuickbarCount = QuickbarPlugin.TotalQuickbarCount.Value;
            while (QuickbarData.Count < totalQuickbarCount)
                QuickbarData.Add(string.Empty);
        }

        /// <summary>
        /// Serializes instance from the character.
        /// Base class will save it to XML.
        /// </summary>
        /// <inheritdoc path="/param"/>
        public override void Save(Character character, bool isWorldHost) {
            var save = QuickbarPlugin.GetQuickbarSave(character.UID);
            OverrideSerializableProperties(save);

            SetQuickbarFromCharacter(character);
        }

        /// <summary>
        /// Sets current class <see cref="QuickbarData"/> at <see cref="ActiveQuickbarIndex"/> using the data from the character.
        /// </summary>
        /// <param name="character"></param>
        public void SetQuickbarFromCharacter(Character character) {
            var quickslotManager = character.QuickSlotMngr;

            var quickbarStringBuilder = new StringBuilder();
            for (int i = 0; i < quickslotManager.QuickSlotCount; i++) {
                quickbarStringBuilder.Append(quickslotManager.GetQuickSlot(i).ToSaveData());
                quickbarStringBuilder.Append(m_quickbarDataSeparator);
            }

            FixQuickbarDataSize();
            QuickbarData[ActiveQuickbarIndex] = quickbarStringBuilder.ToString();
        }

        /// <summary>
        /// Base class Loads XML data to this instance.
        /// Apply it to the character.
        /// </summary>
        /// <inheritdoc path="/param"/>
        public override void ApplyLoadedSave(Character character, bool isWorldHost) {
            var save = QuickbarPlugin.GetQuickbarSave(character.UID);
            save.OverrideSerializableProperties(this);

            SetCharacterQuickbar(character);
        }

        /// <summary>
        /// Sets character quickbar using current <see cref="ActiveQuickbarIndex"/> from <see cref="QuickbarData"/>.
        /// </summary>
        /// <param name="character">Character to set quickbar data to.</param>
        public void SetCharacterQuickbar(Character character) {
            var quickslotManager = character.QuickSlotMngr;

            FixQuickbarDataSize();
            var currentQuickbarData = QuickbarData[ActiveQuickbarIndex];

            if (string.IsNullOrWhiteSpace(currentQuickbarData)) {
                for (int i = 0; i < quickslotManager.QuickSlotCount; i++)
                    quickslotManager.GetQuickSlot(i).Clear();

                return;
            }

            var quickslotData = currentQuickbarData.Split(m_quickbarDataSeparator);
            for (int i = 0; i < quickslotManager.QuickSlotCount; i++) {
                var quickslot = quickslotManager.GetQuickSlot(i);
                quickslot.Clear();
                quickslot.LoadSaveData(quickslotData[i]);
            }
        }

        /// <summary>
        /// Sets new active quickbar to the character.
        /// </summary>
        /// <param name="character">Character to set quickbar data to.</param>
        /// <param name="quickbarIndex">Index of the new active quickbar from the <see cref="QuickbarData"/>.</param>
        public void SetActiveQuickbar(Character character, int quickbarIndex) {
            ActiveQuickbarIndex = quickbarIndex;
            SetCharacterQuickbar(character);
        }
    }
}
