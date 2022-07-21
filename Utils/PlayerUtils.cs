using UnityEngine;

namespace HBMP.Utils
{
    public class PlayerUtils
    {
        public static void TeleportPlayer(Vector3 pos)
        {
            GameObject player = GameObject.Find("[HARD BULLET PLAYER]");
            GameObject other =  GameObject.Instantiate(player, pos, Quaternion.identity);
            GameObject.Destroy(player);

            other.name = "[HARD BULLET PLAYER]";
        }
    }
}