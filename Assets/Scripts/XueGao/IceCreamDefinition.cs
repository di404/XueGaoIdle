using UnityEngine;

namespace XueGao
{
    [CreateAssetMenu(menuName = "XueGao/Ice Cream Definition")]
    public class IceCreamDefinition : ScriptableObject
    {
        public string displayName = "Ice Cream";
        public int price;
        public int prizeMultiplier = 1;
        public Sprite fullSprite;
        public int sampleCount = 64;
        public float alphaThreshold = 0.1f;

        // Compatibility fallback for existing ice cream assets until stick definitions are fully configured.
        public Sprite stickSprite;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.cyan;
    }
}
