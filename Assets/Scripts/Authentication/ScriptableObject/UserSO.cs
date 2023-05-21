using UnityEngine;

[CreateAssetMenu(fileName = "User data", menuName = "Player/User Data")]
public class UserSO : ScriptableObject
{
   public UserModel Data;
   
   private void OnEnable()
   {
      hideFlags = HideFlags.DontUnloadUnusedAsset;
   }
}
