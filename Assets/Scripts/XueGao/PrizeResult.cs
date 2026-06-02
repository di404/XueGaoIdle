namespace XueGao
{
    public readonly struct PrizeResult
    {
        public readonly string Label;
        public readonly int BaseAmount;
        public readonly int FinalAmount;

        public bool IsWin => FinalAmount > 0;

        public PrizeResult(string label, int baseAmount, int finalAmount)
        {
            Label = label;
            BaseAmount = baseAmount;
            FinalAmount = finalAmount;
        }
    }
}
