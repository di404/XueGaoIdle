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
        public Sprite stickSprite;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.cyan;
    }
}
