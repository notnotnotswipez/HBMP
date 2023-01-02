using System.Linq;
using HBMP.Nodes;
using HBMP.Representations;
using MelonLoader;
using Steamworks;
using UnityEngine;

namespace HBMP.Utils
{
    public class PlayerUtils
    {
        public static Transform GetRandomPlayerHead()
        {
            if (SteamManager.Instance.isConnectedToLobby)
            {
                int randomChance = Random.Range(0, 2);
                if (randomChance == 0)
                {
                    MelonLogger.Msg("Retargetting enemy to: ");
                    int selection = Random.Range(0, PlayerRepresentation.representations.Count);
                    SteamId userId = PlayerRepresentation.representations.Keys.ToList()[selection];
                    PlayerRepresentation representation = PlayerRepresentation.representations[userId];
                    MelonLogger.Msg(representation.username);
                    return representation.head;
                }
            }
            MelonLogger.Msg("Targetting enemy to default player");
            return GameObject.Find("[HARD BULLET PLAYER]").GetComponent<PlayerRoot>().PlayerHead;
        }
    }
}