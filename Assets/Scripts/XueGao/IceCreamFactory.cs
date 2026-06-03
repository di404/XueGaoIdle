using UnityEngine;

namespace XueGao
{
    public class IceCreamFactory : MonoBehaviour
    {
        [SerializeField] private IceCream iceCreamPrefab;

        public IceCream Create(IceCreamDefinition definition, Transform parent, Vector3 position, int sortingOrder)
        {
            if (iceCreamPrefab == null || definition == null)
            {
                return null;
            }

            IceCream iceCream = Instantiate(iceCreamPrefab, parent);
            iceCream.name = "TableIceCream_" + definition.displayName;
            iceCream.transform.position = position;
            iceCream.transform.localRotation = Quaternion.identity;
            iceCream.transform.localScale = Vector3.one;
            iceCream.LoadDefinitionForTable(definition);
            iceCream.SetSortingOrder(sortingOrder);
            return iceCream;
        }
    }
}
