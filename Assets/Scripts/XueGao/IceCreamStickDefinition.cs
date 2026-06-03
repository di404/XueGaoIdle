using UnityEngine;

namespace XueGao
{
    [CreateAssetMenu(menuName = "XueGao/Ice Cream Stick Definition")]
    public class IceCreamStickDefinition : ScriptableObject
    {
        public string displayName = "Thanks";
        public Sprite stickSprite;
        public Color tint = Color.white;
    }
}
