// using System;
// using System.Collections.Generic;
// using System.Linq;
// using TMPro;
// using Unity.VisualScripting;
// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
//
// // �Ϸ�װ�������ȫ������д����һ���ű���
// public class ModEquipUI : UIBase
// {
//     public static ModEquipUI Instance;
//
//     private PlayerPreviewSync playerPreview;
//     private Transform modInfo;
//     private List<ShooterModuleBase> shooters;
//
//
//     private void Awake()
//     {
//         Instance = this;
//
//         var camObj = GameObject.Find("PlayerModelCamera");
//         if (camObj)
//             playerPreview = camObj.GetComponent<PlayerPreviewSync>();
//         modInfo = Get<Transform>("ModInfoPanel");
//         modInfo.gameObject.SetActive(false);
//     }
//     private void Update()
//     {
//         RefreshUI();
//         RefreshModBag();
//     }
//
//     public override void OnEnter(object args)
//     {
//         base.OnEnter(args);
//         Time.timeScale = 0.01f;
//         InputManager.Instance.SetLockLevel(InputLockLevel.AllLocked);
//         shooters = PlayerManager.Instance.CurrentModules.GetAllActiveModules().OfType<ShooterModuleBase>().ToList();
//         RefreshUI();
//         RefreshModBag();
//     }
//
//     public override void OnClose()
//     {
//         base.OnClose();
//         Time.timeScale = 1;
//         InputManager.Instance.SetLockLevel(InputLockLevel.None);
//     }
//
//     // ˢ���������
//     public void RefreshUI()
//     {
//         Transform trans = Get<Transform>("ShooterContent");
//         
//         trans.IteratorChild(shooters.Count, Iterator);
//
//         void Iterator(int index, Transform shooterTrans)
//         {
//             int k = index;
//             var shooter = shooters[k];
//             ModuleConfig config = UpgradeManager.Instance.GetConfig(shooter.ModuleType);
//             Color color = config != null ? config.themeColor : Color.cyan;
//
//             Image img = Get<Transform>("ShooterImage").GetComponent<Image>();
//             if (img) img.color = color;
//
//             shooterTrans.Find("Name").GetComponent<TMP_Text>().text = shooter.ModuleType.ToString();
//
//             // ���ɲ����λ
//             Transform slotRoot = shooterTrans.Find("Info/ModSlots");
//             List<WeaponPlugin> equippedMod = shooter.Mods;
//             slotRoot.IteratorChild(shooter.maxNum, detailIterator);
//             void detailIterator(int num, Transform slots)
//             {
//                 int j = num;
//                 if (j < equippedMod.Count)
//                 {
//                     slots.FindDeepChild("ModType").GetComponent<Text>().text = ModManager.Instance.FindConfigByMod(equippedMod[j]).ModType.ToString();
//                     slots.FindDeepChild("Mod").GetComponent<Image>().color = color;
//                     slots.FindDeepChild("Mod").GetComponent<Button>().onClick.SetListener(() =>
//                     {
//
//                         modInfo.Find("Description").GetComponent<Text>().text = ModManager.Instance.FindConfigByMod(equippedMod[j]).Description;
//                         modInfo.Find("Title").GetComponent<Text>().text = ModManager.Instance.FindConfigByMod(equippedMod[j]).ModType.ToString();
//                         modInfo.FindDeepChild("ButtonText").GetComponent<Text>().text = "ж��";
//                         modInfo.Find("ChangeButton").GetComponent<Button>().onClick.SetListener(() =>
//                         {
//                             ModManager.Instance.UnequipMod(shooter, equippedMod[j]);
//                             slots.FindDeepChild("Mod").GetComponent<Image>().color = Color.white;
//                             slots.FindDeepChild("Mod").GetComponent<Button>().onClick.RemoveAllListeners();
//                             modInfo.gameObject.SetActive(false);
//                         });
//                         modInfo.transform.position = new Vector3(0,0,0);
//                         modInfo.gameObject.SetActive(true);
//                     });
//                 }
//                 else
//                 {
//                     slots.Find("ModType").GetComponent<Text>().text = "��";
//                     
//                 }
//             }
//
//         }
//     }
//
//     // ˢ�¿��϶��������
//     public void RefreshModBag()
//     {
//         Transform trans = Get<Transform>("Content");
//         Dictionary<ModType, int> OwnedMods = ModManager.Instance.ownedMods;
//         trans.IteratorChild(OwnedMods.Count, Iterator);
//         void Iterator(int index, Transform bag)
//         {
//             int j = index;
//             bag.Find("ModType").GetComponent<Text>().text = OwnedMods.ElementAt(j).Key.ToString();
//             bag.Find("ModCount").GetComponent<Text>().text = OwnedMods.ElementAt(j).Value.ToString();
//             if (OwnedMods.ElementAt(j).Value <= 0)
//             {
//                 bag.FindDeepChild("OwnedMod").gameObject.SetActive(false);
//             }
//             else
//             {
//                 bag.FindDeepChild("OwnedMod").gameObject.SetActive(true);
//             }
//             bag.FindDeepChild("OwnedMod").GetComponent<Button>().onClick.SetListener(() =>
//             {
//                 modInfo.Find("Description").GetComponent<Text>().text = ModManager.Instance.GetConfig(OwnedMods.ElementAt(j).Key).Description;
//                 modInfo.Find("Title").GetComponent<Text>().text = OwnedMods.ElementAt(j).Key.ToString();
//                 modInfo.FindDeepChild("ButtonText").GetComponent<Text>().text = "װ��";
//                 modInfo.Find("ChangeButton").GetComponent<Button>().onClick.SetListener(() =>
//                 {
//                     ModManager.Instance.EquipMod(shooters[0], OwnedMods.ElementAt(j).Key);
//                     modInfo.gameObject.SetActive(false);
//                 });
//                 modInfo.transform.position = new (0,0,0);
//                 modInfo.gameObject.SetActive(true);
//             });
//         }
//
//     }
//
//     
// }
