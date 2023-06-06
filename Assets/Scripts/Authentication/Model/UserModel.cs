using System;
using System.Collections.Generic;

[Serializable]
public class UserModel
{
    public string email;
    public string account_name;
    public string last_login;
    public string date_created;
    public string account_level;
    public string display_picture;
    public List<ShopAnalyticsModel> shopAnalytics = new List<ShopAnalyticsModel>();
    
    [Serializable]
    public class ShopAnalyticsModel
    {
        public string panelName;
        public string dateTime_added;
        public int count;
        public List<Characters> character = new List<Characters>();

        [Serializable]
        public class Characters
        {
            public string name;
            public int count;
        }
    }

}
