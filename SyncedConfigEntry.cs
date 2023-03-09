using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiplayerSync
{
    public class SyncedConfigEntry<T>
    {
        public ConfigEntry<T> Entry;
        public SyncedEntry<T> SyncedEntry;

        public SyncedConfigEntry(ConfigEntry<T> entry)
        {
            Bind(entry);
        }

        public SyncedConfigEntry()
        {

        }

        public void Bind(ConfigEntry<T> entry)
        {
            Entry = entry;
            SyncedEntry = SyncedEntries.RegisterSyncedConfig(entry);
        }

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
            set { Entry.Value = value; }
        }
    }
}
