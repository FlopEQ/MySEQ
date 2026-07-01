using myseq.Properties;
using Structures;

namespace myseq
{
    internal static class SpawnVisibility
    {
        public static bool IsPlayer(Spawninfo spawn)
        {
            return spawn != null && (spawn.Type == 0 || spawn.IsPlayer);
        }

        public static bool IsCorpse(Spawninfo spawn)
        {
            return spawn != null && (spawn.isCorpse || spawn.Type == 2 || spawn.Type == 3);
        }

        public static bool ShouldHide(Spawninfo spawn)
        {
            if (spawn == null)
            {
                return true;
            }

            if (IsCorpse(spawn))
            {
                if (IsPlayer(spawn))
                {
                    return spawn.IsMyCorpse
                        ? !Settings.Default.ShowMyCorpse
                        : !Settings.Default.ShowPCCorpses;
                }

                return !Settings.Default.ShowCorpses;
            }

            if (IsPlayer(spawn))
            {
                return !Settings.Default.ShowPlayers;
            }

            return !Settings.Default.ShowNPCs
                || (spawn.isEventController && !Settings.Default.ShowInvis)
                || (spawn.isMount && !Settings.Default.ShowMounts)
                || (spawn.isPet && !Settings.Default.ShowPets)
                || (spawn.isFamiliar && !Settings.Default.ShowFamiliars);
        }

        public static bool ShouldShowCorpse(Spawninfo spawn)
        {
            return IsCorpse(spawn) && !ShouldHide(spawn);
        }
    }
}
