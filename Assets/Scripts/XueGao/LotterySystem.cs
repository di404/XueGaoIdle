using UnityEngine;

namespace XueGao
{
    public class LotterySystem : MonoBehaviour
    {
        [SerializeField] private int[] prizeAmounts = { 0, 1, 5, 20, 100, 500 };
        [SerializeField] private string[] prizeLabels = { "谢谢参与", "1元", "5元", "20元", "100元", "500元" };
        [SerializeField] private float[] prizeWeights = { 45f, 25f, 15f, 10f, 4f, 1f };
        [SerializeField] private IceCreamStickDefinition[] stickDefinitions;
        [SerializeField] private float noPrizeReductionPerLuckLevel = 1.5f;

        public PrizeResult Roll(int multiplier, int luckLevel)
        {
            float totalWeight = 0f;
            float noPrizeReduction = luckLevel * noPrizeReductionPerLuckLevel;

            for (int i = 0; i < prizeWeights.Length; i++)
            {
                float weight = prizeWeights[i];
                if (i == 0)
                {
                    weight = Mathf.Max(15f, weight - noPrizeReduction);
                }

                totalWeight += Mathf.Max(0f, weight);
            }

            float roll = Random.Range(0f, totalWeight);
            for (int i = 0; i < prizeWeights.Length; i++)
            {
                float weight = prizeWeights[i];
                if (i == 0)
                {
                    weight = Mathf.Max(15f, weight - noPrizeReduction);
                }

                roll -= Mathf.Max(0f, weight);
                if (roll <= 0f)
                {
                    int baseAmount = prizeAmounts[Mathf.Clamp(i, 0, prizeAmounts.Length - 1)];
                    string label = prizeLabels[Mathf.Clamp(i, 0, prizeLabels.Length - 1)];
                    IceCreamStickDefinition stickDefinition = GetStickDefinition(i);
                    return new PrizeResult(label, baseAmount, baseAmount * multiplier, stickDefinition);
                }
            }

            return new PrizeResult(prizeLabels[0], 0, 0, GetStickDefinition(0));
        }

        private IceCreamStickDefinition GetStickDefinition(int prizeIndex)
        {
            if (stickDefinitions == null || stickDefinitions.Length == 0)
            {
                return null;
            }

            return stickDefinitions[Mathf.Clamp(prizeIndex, 0, stickDefinitions.Length - 1)];
        }
    }
}
