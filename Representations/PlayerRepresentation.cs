using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using HBMP.DataType;
using HexabodyVR.PlayerController;
using MelonLoader;
using UnityEngine;

namespace HBMP.Representations
{
    public class PlayerRepresentation
    {
        public GameObject playerRep;
        public static Dictionary<long, PlayerRepresentation> representations = new Dictionary<long, PlayerRepresentation>();
        public User user;
        public String username;

        private Transform head;
        private Transform handR;
        private Transform handL;

        public PlayerRepresentation(User user)
        {
            playerRep = GameObject.Instantiate(Mod.player, new Vector3(0, 1, 0), Quaternion.identity);
            playerRep.name = "(PlayerRep)"+user.Username;
            username = user.Username;
            this.user = user;
            PrepareForMultiplayer();
            GameObject.DontDestroyOnLoad(playerRep);
        }

        public void UpdateTransforms(SimplifiedTransform[] simplifiedTransforms)
        {
            head.position = simplifiedTransforms[0].position;
            head.eulerAngles = simplifiedTransforms[0].rotation.ExpandQuat().eulerAngles;

            handR.position = simplifiedTransforms[1].position;
            handR.eulerAngles = simplifiedTransforms[1].rotation.ExpandQuat().eulerAngles;
            
            handL.position = simplifiedTransforms[2].position;
            handL.eulerAngles = simplifiedTransforms[2].rotation.ExpandQuat().eulerAngles;
        }

        private void PrepareForMultiplayer()
        {
            handR = playerRep.transform.Find("1_PlayerHandR");
            handL = playerRep.transform.Find("1_PlayerHandL");
            head = playerRep.transform.Find("1_PlayerHead");
        }

        public void DeleteRepresentation()
        {
            representations.Remove(user.Id);
            GameObject.Destroy(playerRep);
        }
    }
}