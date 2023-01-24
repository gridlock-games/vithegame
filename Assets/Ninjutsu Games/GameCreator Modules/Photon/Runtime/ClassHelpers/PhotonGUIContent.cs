using UnityEngine;

namespace NJG.PUN
{
    public static class PhotonGUIContent
    {
        public static GUIContent MaxPlayers = new GUIContent("Max Players",
            "Max number of players that can be in the room at any time. 0 means 'no limit'.");

        public static GUIContent ExpectedMaxPlayers = new GUIContent("Expected Max Players",
            "Filters for a particular maxplayer setting. Use 0 to accept any maxPlayer value.");

        public static GUIContent PlayerTTL = new GUIContent("Player Ttl", 
            "Time To Live (TTL) for an 'actor' in a room. If a client disconnects, this actor is inactive first and removed after this timeout. In milliseconds.");

        public static GUIContent EmptyRoomTTL = new GUIContent("Empty Room Ttl", 
            "Time To Live (TTL) for a room when the last player leaves. Keeps room in memory for case a player re-joins soon. In milliseconds.");

        public static GUIContent ExpectedRoomProperties = new GUIContent("Expected Room Properties", 
            "Filters for rooms that match these custom properties (string keys and values).");

        public static GUIContent RoomProperties = new GUIContent("Room Properties",
            "Custom room properties are any key-values you need to define the game's setup.");

        public static GUIContent MatchingType = new GUIContent("Matchmaking Mode",
            "FillRoom: Fills up rooms (oldest first) to get players together as fast as possible.\n" +
            "SerialMatching: Distributes players across available rooms sequentially but takes filter into account. Without filter, rooms get players evenly distributed.\n" +
            "RandomMatching: Joins a (fully) random room. Expected properties must match but aside from this, any available room might be selected.");
    }
}