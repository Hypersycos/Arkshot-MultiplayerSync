using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiplayerSync
{
    /// <summary>
    /// A wrapper around a <see cref="ConfigEntry{T}"/>, allowing easy syncing
    /// </summary>
    /// <typeparam name="T">The ConfigEntry's type</typeparam>
    public class SyncedConfigEntry<T>
    {
        /// <summary>
        /// The wrapped <see cref="ConfigEntry{T}"/>
        /// </summary>
        public ConfigEntry<T> ConfigEntry;
        /// <summary>
        /// The wrapped <see cref="SyncedEntry{T}"/>
        /// </summary>
        public SyncedEntry<T> SyncedEntry;

        public SyncedConfigEntry(ConfigEntry<T> entry)
        {
            Bind(entry);
        }

        /// <summary>
        /// A default constructor to facilitate easy replacement of ConfigEntries. Make sure to call Bind before use.
        /// </summary>
        public SyncedConfigEntry()
        {

        }

        /// <summary>
        /// Binds the SyncedConfigEntry to a ConfigEntry, creating a SyncedEntry for it.
        /// </summary>
        public void Bind(ConfigEntry<T> entry)
        {
            ConfigEntry = entry;
            SyncedEntry = SyncedEntries.RegisterSyncedConfig(entry);
        }

        /// <summary>
        /// Used as a drop-in replacement for <see cref="ConfigEntry.Value"/>
        /// Gets the synced/host value when in a room, otherwise the configured value
        /// Sets the ConfigEntry's value
        /// </summary>
        public T Value
        {
            get
            {
                if (PhotonNetwork.inRoom)
                {
                    return SyncedEntry.Value;
                }
                else
                {
                    return SyncedEntry.MyHostValue;
                }
            }
            set { ConfigEntry.Value = value; }
        }
    }
}
