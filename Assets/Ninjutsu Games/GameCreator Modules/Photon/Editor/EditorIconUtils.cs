namespace NJG.PUN
{
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using Photon.Realtime;

    public static class EditorIconUtils
    {
        private const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";
        private const string STATUS_FOLDER = "Status/";
        private const string VARIABLE_ICON_DEFAULT_NAME = "Default.png";
        private static Dictionary<string, Texture2D> VARIABLE_ICONS;
        private static readonly string[] STATUS_ICON_NAMES = new string[]
        {
            "Default.png",
            "Disconnected.png",
            "Disconnected.png",
            "InLobby.png",
            "InLobby.png",
            "Disconnected.png",
            "Connecting.png",
            "Connected.png",
            "Connecting.png",
            "Connected.png",
            "Connecting.png",
            "Disconnected.png",
            "Connecting.png",
            "Connecting.png",
            "Disconnected.png",
            "Disconnected.png",
            "Connecting.png",
            "Connecting.png",
            "Connecting.png",
            "Disconnected.png",
            "Connecting.png",
        };


        // PUBLIC METHODS: ------------------------------------------------------------------------

        public static Texture2D GetStatusIcon(ClientState type)
        {
            string name = STATUS_ICON_NAMES[(int)type];

            if (VARIABLE_ICONS == null) VARIABLE_ICONS = new Dictionary<string, Texture2D>();
            if (!VARIABLE_ICONS.ContainsKey(name))
            {
                Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    Path.Combine(ICONS_PATH + STATUS_FOLDER, name)
                );

                if (icon == null)
                {
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        Path.Combine(ICONS_PATH + STATUS_FOLDER, VARIABLE_ICON_DEFAULT_NAME)
                    );
                }

                VARIABLE_ICONS.Add(name, icon);
            }

            return VARIABLE_ICONS[name];
        }
    }
}