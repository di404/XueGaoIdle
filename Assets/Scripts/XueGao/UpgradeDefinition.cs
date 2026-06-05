using System;
using UnityEngine;

namespace XueGao
{
    [Serializable]
    public class UpgradeDefinition
    {
        [SerializeField] private UpgradeType type;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private int baseCost = 20;
        [SerializeField] private float costGrowth = 1.8f;
        [SerializeField] private int maxLevel = 20;
        [SerializeField] private int displayLevelOffset;

        [SerializeField] private int level;

        public UpgradeType Type => type;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int Level => level;
        public int DisplayLevel => level + displayLevelOffset;
        public bool IsMaxLevel => maxLevel > 0 && level >= maxLevel;

        public int CurrentCost
        {
            get
            {
                if (IsMaxLevel)
                {
                    return 0;
                }

                return Mathf.Max(1, Mathf.RoundToInt(baseCost * Mathf.Pow(Mathf.Max(1f, costGrowth), level)));
            }
        }

        public bool CanPurchase(int money)
        {
            return !IsMaxLevel && money >= CurrentCost;
        }

        public void Purchase()
        {
            if (!IsMaxLevel)
            {
                level++;
            }
        }

        public static UpgradeDefinition Create(UpgradeType type, string displayName, string description, int baseCost, float costGrowth, int maxLevel, int displayLevelOffset = 0)
        {
            return new UpgradeDefinition
            {
                type = type,
                displayName = displayName,
                description = description,
                baseCost = baseCost,
                costGrowth = costGrowth,
                maxLevel = maxLevel,
                displayLevelOffset = displayLevelOffset
            };
        }
    }
}
