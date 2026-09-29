

namespace Member.Wst.Scripts.Achievements
{
    public enum AchievementType
    {
        // Preserve saved IDs: 0 and 1 belonged to removed currency achievements.
        StartGame = 2, DefeatMaster,
        DefeatMimic, DefeatSwordmaster, DefeatAllBosses, MimicExplosion,
        TakeOutBasketball1, TakeOutBasketball10, TakeOutBasketball100, TakeOutBasketball1000, QuitGame
    }
    public enum AchievementRank
    {
        Normal, Hard, Impossible, Secret
    }
}
