namespace GameCreator.Core
{
    using NJG.PUN;
    using Photon.Realtime;
    using Photon.Pun;

    [System.Serializable]
    public class TargetPhotonVariable
    {
        public enum Target
        {
            PlayerName,
            PlayerProperty,
            RoomName,
            RoomProperty,
            PlayerCount
        }

        // PROPERTIES: ----------------------------------------------------------------------------

        public Target target = Target.PlayerName;
        public TargetPhotonPlayer player = new TargetPhotonPlayer() { target = TargetPhotonPlayer.Target.Player };
        public string propertyName;

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public string GetValue()
        {
            string result = null;
            Player player = null;

            switch (this.target)
            {
                case Target.PlayerName:
                    player = this.player.GetPhotonPlayer(null);
                    result = player.NickName;
                    break;
                case Target.PlayerProperty:
                    player = this.player.GetPhotonPlayer(null);
                    result = player.GetProperty(propertyName).ToString();
                    break;
                case Target.RoomName:
                    result = PhotonNetwork.CurrentRoom.Name;
                    break;
            }

            return result;
        }

        // UTILITIES: -----------------------------------------------------------------------------

        public override string ToString()
        {
            string result = "(unknown)";
            switch (this.target)
            {
                case Target.PlayerName: result = "(Player Name)"; break;
                /*case Target.Input: result = input == null || string.IsNullOrEmpty(input.text) ? "(Input Value)" : input.text; break;
                case Target.String:
                    string value = this.stringProperty.GetValue();
                    result = (string.IsNullOrEmpty(value) ? "(none)" : value);
                    break;*/
            }

            return result;
        }
    }
}