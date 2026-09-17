namespace Member.Wst.Scripts.CoreSystems
{
    public static class CompactNumber
    {
        public static string Format(int value)
        {
            if (value < 1000)
                return value.ToString();
            if (value < 1_000_000)
                return (value / 1000f).ToString("0.#") + "k";
            if (value < 1_000_000_000)
                return (value / 1_000_000f).ToString("0.#") + "m";
            return (value / 1_000_000_000f).ToString("0.#") + "b";
        }
    }
}