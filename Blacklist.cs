using UnityEngine;

namespace HBMP
{
    public class Blacklist
    {
        public static bool isBlockedBone(GameObject gameObject)
        {
            if (gameObject.name.ToLower().Contains("neck"))
            {
                return true;
            }

            return false;
        }
    }
}