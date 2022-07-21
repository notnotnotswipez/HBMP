using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using HBMP.DataType;
using HexabodyVR.PlayerController;
using MelonLoader;
using TMPro;
using UnityEngine;

namespace HBMP.Representations
{
    public class PlayerRepresentation
    {
        public GameObject playerRep;
        public GameObject nameTag;
        public static Dictionary<long, PlayerRepresentation> representations = new Dictionary<long, PlayerRepresentation>();
        public User user;
        public String username;

        private Transform head;
        private Transform handR;
        private Transform handL;

        public PlayerRepresentation(User user)
        {
            playerRep = GameObject.Instantiate(Mod.player, new Vector3(0, 1, 0), Quaternion.identity);
            nameTag = GameObject.Instantiate(GameObject.Find("notification(Clone)"));
            nameTag.name = "nametag";
            nameTag.GetComponent<TMP_Text>().text = user.Username;
            nameTag.GetComponent<TMP_Text>().horizontalAlignment = HorizontalAlignmentOptions.Center;
            nameTag.GetComponent<TMP_Text>().autoSizeTextContainer = true;
            nameTag.GetComponent<TMP_Text>().enableAutoSizing = true;
            nameTag.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

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
            nameTag.transform.parent = head.transform;
            nameTag.transform.localPosition = new Vector3(0, 0.2f, -0.1f);
            nameTag.transform.rotation = Quaternion.Euler(0, 180, 0);

            GameObject cosmetics = head.Find("Cosmetics").gameObject;
            GameObject toEnable = null;
            // Hazzy
            if (user.Id == 455212357874876417)
            {
                toEnable = cosmetics.transform.Find("Hazzy").gameObject;
            }
            // Squidy
            else if (user.Id == 303836568496504832)
            {
                toEnable = cosmetics.transform.Find("squidylad").gameObject;
            }
            // Swipez
            else if (user.Id == 186608104274788352)
            {
                toEnable = cosmetics.transform.Find("swipez").gameObject;
            }
            // Korby
            else if (user.Id == 698448626338496542)
            {
                toEnable = cosmetics.transform.Find("korby").gameObject;
            }

            for (int i = 0; i < cosmetics.transform.childCount; i++)
            {
                GameObject cosmetic = cosmetics.transform.GetChild(i).gameObject;
                if (toEnable != null)
                {
                    if (!cosmetic.Equals(toEnable))
                    {
                        cosmetic.SetActive(false);
                    }
                }
                else
                {
                    cosmetic.SetActive(false);
                }
            }
        }

        public void DeleteRepresentation()
        {
            representations.Remove(user.Id);
            GameObject.Destroy(playerRep);
        }
    }
}