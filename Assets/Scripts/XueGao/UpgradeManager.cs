using System;
using System.Collections.Generic;
using UnityEngine;

namespace XueGao
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>
        {
            UpgradeDefinition.Create(UpgradeType.BiteSize, "大口咬", "咬痕范围更大，每次能吃掉更多雪糕。", 20, 1.8f, 20, 1),
            UpgradeDefinition.Create(UpgradeType.AutoBite, "自动吃", "聚焦吃雪糕时，自动帮你连续咬雪糕。", 50, 2f, 12),
            UpgradeDefinition.Create(UpgradeType.Luck, "幸运值", "降低谢谢参与权重，让中奖概率更友好。", 80, 1.9f, 15),
            UpgradeDefinition.Create(UpgradeType.TableSpace, "大桌面", "桌面容量增加，可以同时摆更多雪糕。", 120, 2.05f, 6),
            UpgradeDefinition.Create(UpgradeType.PrizeBonus, "奖金加成", "每次中奖结算时获得额外奖金。", 160, 2.15f, 10)
        };

        public event Action<UpgradeDefinition> Purchased;

        public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;
        public int MouthLevel => 1 + GetLevel(UpgradeType.BiteSize);
        public int AutoBiteLevel => GetLevel(UpgradeType.AutoBite);
        public int LuckLevel => GetLevel(UpgradeType.Luck);
        public int TableCapacityBonus => GetLevel(UpgradeType.TableSpace) * 2;
        public float PrizeBonusMultiplier => 1f + GetLevel(UpgradeType.PrizeBonus) * 0.15f;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureDefaults();
        }

        public UpgradeDefinition GetDefinition(UpgradeType type)
        {
            EnsureDefaults();
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null && upgrades[i].Type == type)
                {
                    return upgrades[i];
                }
            }

            return null;
        }

        public int GetLevel(UpgradeType type)
        {
            UpgradeDefinition definition = GetDefinition(type);
            return definition != null ? definition.Level : 0;
        }

        public int GetDisplayLevel(UpgradeType type)
        {
            UpgradeDefinition definition = GetDefinition(type);
            return definition != null ? definition.DisplayLevel : 0;
        }

        public int GetCost(UpgradeType type)
        {
            UpgradeDefinition definition = GetDefinition(type);
            return definition != null ? definition.CurrentCost : 0;
        }

        public bool CanPurchase(UpgradeType type, int money)
        {
            UpgradeDefinition definition = GetDefinition(type);
            return definition != null && definition.CanPurchase(money);
        }

        public bool TryPurchase(UpgradeType type, int money, out int cost, out string message)
        {
            UpgradeDefinition definition = GetDefinition(type);
            if (definition == null)
            {
                cost = 0;
                message = "升级未配置。";
                return false;
            }

            if (definition.IsMaxLevel)
            {
                cost = 0;
                message = definition.DisplayName + "已满级。";
                return false;
            }

            cost = definition.CurrentCost;
            if (money < cost)
            {
                message = "钱不够升级" + definition.DisplayName + "。";
                return false;
            }

            definition.Purchase();
            message = definition.DisplayName + "升级到 Lv." + definition.DisplayLevel + "。";
            Purchased?.Invoke(definition);
            return true;
        }

        public int ApplyPrizeBonus(int finalAmount)
        {
            if (finalAmount <= 0)
            {
                return finalAmount;
            }

            return Mathf.RoundToInt(finalAmount * PrizeBonusMultiplier);
        }

        private void EnsureDefaults()
        {
            if (upgrades != null && upgrades.Count > 0)
            {
                return;
            }

            upgrades = new List<UpgradeDefinition>
            {
                UpgradeDefinition.Create(UpgradeType.BiteSize, "大口咬", "咬痕范围更大，每次能吃掉更多雪糕。", 20, 1.8f, 20, 1),
                UpgradeDefinition.Create(UpgradeType.AutoBite, "自动吃", "聚焦吃雪糕时，自动帮你连续咬雪糕。", 50, 2f, 12),
                UpgradeDefinition.Create(UpgradeType.Luck, "幸运值", "降低谢谢参与权重，让中奖概率更友好。", 80, 1.9f, 15),
                UpgradeDefinition.Create(UpgradeType.TableSpace, "大桌面", "桌面容量增加，可以同时摆更多雪糕。", 120, 2.05f, 6),
                UpgradeDefinition.Create(UpgradeType.PrizeBonus, "奖金加成", "每次中奖结算时获得额外奖金。", 160, 2.15f, 10)
            };
        }
    }
}
