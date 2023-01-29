using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using HBMP.DataType;
using HexabodyVR.PlayerController;
using MelonLoader;
using Steamworks;
using TMPro;
using UnityEngine;

namespace HBMP.Representations
{
    public class PlayerRepresentation
    {
        public GameObject playerRep;
        
        public GameObject nameTagObject;
        public Transform nameTagPos;
        
        public GameObject ikBody;
        public static Dictionary<SteamId, PlayerRepresentation> representations = new Dictionary<SteamId, PlayerRepresentation>();
        public Dictionary<byte, GameObject> boneDictionary = new Dictionary<byte, GameObject>();
        public Friend user;
        public String username;

        private byte currentBoneId = 0;

        public Transform head;
        public Transform handR;
        public Transform handL;

        public PlayerRepresentation(Friend user)
        {
            ikBody = GameObject.Instantiate(GameObject.Find("[HARD BULLET PLAYER]"));
            GameObject.Destroy(ikBody.transform.Find("PlayerSystems").gameObject);
            ikBody.transform.Find("HexaBody").Find("PlayerModel").Find("PlayerModel").GetComponent<TransformsPinner>()
                .enabled = false;
            ikBody.transform.Find("HexaBody").Find("Pelvis").Find("CameraRig").gameObject.SetActive(false);
            ikBody.transform.Find("HexaBody").GetComponent<HexaBodyPlayer3>().enabled = false;

            int hexabodyChildCount = ikBody.transform.Find("HexaBody").childCount;
            for (int i = 0; i < hexabodyChildCount; i++)
            {
                GameObject hexChild = ikBody.transform.Find("HexaBody").GetChild(i).gameObject;
                if (!hexChild.name.Equals("PlayerModel"))
                {
                    GameObject.Destroy(hexChild);
                }
            }

            playerRep = GameObject.Instantiate(Mod.player, new Vector3(0, 1, 0), Quaternion.identity);
            MakeNametag();

            playerRep.name = "(PlayerRep)"+user.Name;
            username = user.Name;
            this.user = user;
            PrepareForMultiplayer();
            boneDictionary.Add(currentBoneId++, ikBody.transform.Find("HexaBody").Find("PlayerModel").Find("PlayerModel").Find("root").gameObject);
            populateBoneDictionary(ikBody.transform.Find("HexaBody").Find("PlayerModel").Find("PlayerModel").Find("root"));
            GameObject.DontDestroyOnLoad(playerRep);
            GameObject.DontDestroyOnLoad(ikBody);
            GameObject.DontDestroyOnLoad(nameTagObject);
            currentBoneId = 0;
            
            
        }
        
        public void MakeNametag()
        {
            nameTagObject = new GameObject($"RepName {username}");
            
            Canvas textCanvas = nameTagObject.AddComponent<Canvas>();
            
            textCanvas.renderMode = RenderMode.WorldSpace;
            nameTagPos = nameTagObject.transform;
            nameTagPos.localScale = Vector3.one / 200.0f;

            TextMeshProUGUI nameTagText = nameTagObject.AddComponent<TextMeshProUGUI>();
            
            nameTagText.text = username;
            nameTagText.alignment = TextAlignmentOptions.Midline;
            nameTagText.enableAutoSizing = true;
            
        }

        public void Update()
        {
            if (nameTagPos)
            {
                if (MainMod.head)
                {
                    nameTagPos.position = head.transform.position + Vector3.up * 0.4f;
                    nameTagPos.rotation = Quaternion.LookRotation(
                        Vector3.Normalize(nameTagPos.position - MainMod.head.transform.position), Vector3.up);
                }
            }
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

        private void populateBoneDictionary(Transform parent)
        {
            int childCount = parent.childCount;

            for (int i = 0; i < childCount; i++)
            {
                GameObject child = parent.GetChild(i).gameObject;
                boneDictionary.Add(currentBoneId++, child);
 
                if (child.transform.childCount > 0 && !Blacklist.isBlockedBone(child))
                {
                    populateBoneDictionary(child.transform);
                }
            }
        }

        public void updateIkTransform(byte boneId, SimplifiedTransform simplifiedTransform)
        {
            if (boneDictionary.ContainsKey(boneId))
            {
                GameObject selectedBone = boneDictionary[boneId];

                selectedBone.transform.position = simplifiedTransform.position;
                selectedBone.transform.eulerAngles = simplifiedTransform.rotation.ExpandQuat().eulerAngles;
            }
        }

        private void PrepareForMultiplayer()
        {
            handR = playerRep.transform.Find("1_PlayerHandR");
            handL = playerRep.transform.Find("1_PlayerHandL");
            ikBody.name = username + "Body";
            
            //handR.gameObject.SetActive(false);
            //handL.gameObject.SetActive(false);
            head = playerRep.transform.Find("1_PlayerHead");

            GameObject cosmetics = head.Find("Cosmetics").gameObject;
            GameObject toEnable = null;
            // Hazzy
            if (user.Id == 76561198843066427)
            {
                toEnable = cosmetics.transform.Find("Hazzy").gameObject;
            }
            // Squidy
            else if (user.Id == 76561198301131664)
            {
                toEnable = cosmetics.transform.Find("squidylad").gameObject;
            }
            // Swipez
            else if (user.Id == 76561198885873876)
            {
                toEnable = cosmetics.transform.Find("swipez").gameObject;
            }
            // Dado
            else if (user.Id == 340248916442218498)
            {
                toEnable = cosmetics.transform.Find("dado").gameObject;
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
            GameObject.Destroy(ikBody);
            GameObject.Destroy(playerRep);
            representations.Remove(user.Id);
        }
    }
}