using RealmShards.Core;
using RealmShards.Save;
using RealmShards.UI;

namespace RealmShards.Progression
{
    /// <summary>
    /// Grants vials (persistent) and coins (run-only) when opponents are defeated.
    /// </summary>
    public static class RunCurrencyRewards
    {
        public static void OnOpponentDefeated()
        {
            var ctx = GameContext.Instance;
            if (ctx == null)
                return;

            ctx.Progression?.AddVials(CurrencyRewards.VialsPerOpponent);
            ctx.RunSession?.AddRunCoins(CurrencyRewards.CoinsPerOpponent);

            int vials = ctx.Progression?.Vials ?? 0;
            int coins = ctx.RunSession?.RunCoins ?? 0;
            RunCurrencyFlashHud.Notify(vials, coins);
        }
    }
}
