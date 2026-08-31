using ClientCore;
using ClientCore.Enums;

using Rampastring.Tools;
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// A helper class for setting up alliances in spawn.ini.
    /// </summary>
    public static class AllianceHolder
    {
        public static void WriteInfoToSpawnIni(
            List<PlayerInfo> players,
            List<PlayerInfo> aiPlayers, 
            List<int> multiCmbIndexes,
            List<PlayerHouseInfo> playerHouseInfos,
            List<TeamStartMapping> teamStartMappings,
            IniFile spawnIni
        )
        {
            // One bucket per team the lobby offers, rather than four named
            // lists and a four-case switch.
            //
            // Teams beyond the fourth were silently dropped: a player set to
            // team E, F, G or H matched no case, so they were written no
            // alliances at all and played as a free-for-all house while the
            // lobby showed them on a team. ProgramConstants.TEAMS has offered
            // eight for a while; only this resolver was still capped at four.
            //
            // The engine has no concept of a team - it reads pairwise
            // [MultiN_Alliances] - so the count here is purely a client limit
            // and follows TEAMS automatically from now on.
            var teamMembers = new List<List<int>>();
            for (int i = 0; i < ProgramConstants.TEAMS.Count; i++)
                teamMembers.Add(new List<int>());

            void AddToTeam(int teamId, int multiMemberId)
            {
                if (teamId > 0 && teamId <= teamMembers.Count)
                    teamMembers[teamId - 1].Add(multiMemberId);
            }

            for (int pId = 0; pId < players.Count; pId++)
            {
                var phi = playerHouseInfos[pId];
                int teamId = players[pId].TeamId;
                if (teamId <= 0)
                    teamId = teamStartMappings?.Find(sa => sa.StartingWaypoint == phi.StartingWaypoint)?.TeamId ?? 0;

                AddToTeam(teamId, multiCmbIndexes.FindIndex(c => c == pId) + 1);
            }

            int multiId = multiCmbIndexes.Count + 1;

            for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
            {
                var phi = playerHouseInfos[multiCmbIndexes.Count + aiId];
                int teamId = aiPlayers[aiId].TeamId;
                if (teamId <= 0)
                    teamId = teamStartMappings?.Find(sa => sa.StartingWaypoint == phi.StartingWaypoint)?.TeamId ?? 0;

                AddToTeam(teamId, multiId);
                multiId++;
            }

            foreach (var members in teamMembers)
                WriteAlliances(members, spawnIni);
        }

        private static void WriteAlliances(List<int> teamHouseMemberIds, IniFile spawnIni)
        {
            foreach (int houseId in teamHouseMemberIds)
            {
                bool selfFound = false;

                for (int allyId = 0; allyId < teamHouseMemberIds.Count; allyId++)
                {
                    int allyHouseId = teamHouseMemberIds[allyId];

                    if (allyHouseId == houseId)
                        selfFound = true;
                    else
                    {
                        spawnIni.SetIntValue("Multi" + houseId + "_Alliances",
                            "HouseAlly" + GetHouseAllyIndexString(allyId, selfFound),
                            ClientConfiguration.Instance.ClientGameType == ClientType.RA
                                ? allyHouseId + 11  // Compared with other games, Red Alert uses house IDs shifted by +12 (from -1 to +11) in multiplayer
                                : allyHouseId - 1);
                    }
                }
            }
        }

        /// <summary>
        /// Maps an ally's ordinal to the key suffix the spawner expects.
        /// </summary>
        /// <remarks>
        /// This used to stop at Seven, which was correct while a game held at
        /// most 8 players and therefore at most 7 allies. Past that it returned
        /// "None" + allyId, producing keys such as HouseAllyNone7 that nothing
        /// reads - so on a 16-player team every alliance after the seventh was
        /// silently dropped and teammates spawned hostile to each other.
        /// </remarks>
        private static string GetHouseAllyIndexString(int allyId, bool selfFound)
        {
            if (selfFound)
                allyId = allyId - 1;

            switch (allyId)
            {
                case 0:
                    return "One";
                case 1:
                    return "Two";
                case 2:
                    return "Three";
                case 3:
                    return "Four";
                case 4:
                    return "Five";
                case 5:
                    return "Six";
                case 6:
                    return "Seven";
                case 7:
                    return "Eight";
                case 8:
                    return "Nine";
                case 9:
                    return "Ten";
                case 10:
                    return "Eleven";
                case 11:
                    return "Twelve";
                case 12:
                    return "Thirteen";
                case 13:
                    return "Fourteen";
                case 14:
                    return "Fifteen";
                case 15:
                    return "Sixteen";
                case 16:
                    return "Seventeen";
                case 17:
                    return "Eighteen";
                case 18:
                    return "Nineteen";
                case 19:
                    return "Twenty";
                case 20:
                    return "TwentyOne";
                case 21:
                    return "TwentyTwo";
                case 22:
                    return "TwentyThree";
                case 23:
                    return "TwentyFour";
                case 24:
                    return "TwentyFive";
                case 25:
                    return "TwentySix";
                case 26:
                    return "TwentySeven";
                case 27:
                    return "TwentyEight";
                case 28:
                    return "TwentyNine";
            }

            return "None" + allyId;
        }
    }
}
