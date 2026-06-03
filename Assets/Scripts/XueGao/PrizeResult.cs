namespace XueGao
{
    public readonly struct PrizeResult
    {
        public readonly string Label;
        public readonly int BaseAmount;
        public readonly int FinalAmount;
        public readonly IceCreamStickDefinition StickDefinition;

        public bool IsWin => FinalAmount > 0;

        public PrizeResult(string label, int baseAmount, int finalAmount, IceCreamStickDefinition stickDefinition = null)
        {
            Label = label;
            BaseAmount = baseAmount;
            FinalAmount = finalAmount;
            StickDefinition = stickDefinition;
        }
    }
}
