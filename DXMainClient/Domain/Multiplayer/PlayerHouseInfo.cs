using System;
using System.Collections.Generic;
using ClientCore;

namespace DTAClient.Domain.Multiplayer
{
    public class PlayerHouseInfo
    {
        public int SideIndex { get; set; }

        /// <summary>
        /// A side (or, more correctly, house or country depending on the game)
        /// index that is used in rules file of the game.
        /// </summary>
        public int InternalSideIndex
        {
            get
            {
                if (IsSpectator && !string.IsNullOrEmpty(ClientConfiguration.Instance.SpectatorInternalSideIndex))
                    return int.Parse(ClientConfiguration.Instance.SpectatorInternalSideIndex);
                
                if (!string.IsNullOrEmpty(ClientConfiguration.Instance.InternalSideIndices))
                    return Array.ConvertAll(ClientConfiguration.Instance.InternalSideIndices.Split(','), int.Parse)[SideIndex];

                return SideIndex;
            }
        }
        public int ColorIndex { get; set; }
        public int StartingWaypoint { get; set; }

        public int RealStartingWaypoint { get; set; }

        public bool IsSpectator { get; set; }

        /// <summary>
        /// Applies the player's side into the information
        /// and randomizes it if necessary.
        /// </summary>
        /// <param name="pInfo">The PlayerInfo of the player.</param>
        /// <param name="sideCount">The number of sides in the game.</param>
        /// <param name="random">Random number generator.</param>
        /// <param name="disallowedSideArray">A bool array that determines which side indexes are disallowed by game options.</param>
        public void RandomizeSide(PlayerInfo pInfo, int sideCount, Random random,
            bool[] disallowedSideArray, List<int[]> randomSelectors, int randomCount)
        {
            if (pInfo.SideId == 0 || pInfo.SideId == sideCount + randomCount)
            {
                // The player has selected Random or Spectator

                int sideId;

                do sideId = random.Next(0, sideCount);
                while (disallowedSideArray[sideId]);

                SideIndex = sideId;
            }
            else
            {
                // Use custom random selector.
                if (pInfo.SideId < randomCount)
                {
                    int[] randomsides = randomSelectors[pInfo.SideId - 1];
                    int count = randomsides.Length;
                    int sideId;
                    
                    do sideId = randomsides[random.Next(0, count)];
                    while (disallowedSideArray[sideId]);

                    SideIndex = sideId;
                }
                else SideIndex = pInfo.SideId - randomCount; // The player has selected a side
            }
        }

        /// <summary>
        /// Applies the player's color into the information and randomizes
        /// it if necessary. If the color is randomized, it's removed
        /// from the list of available colors.
        /// </summary>
        /// <param name="pInfo">The PlayerInfo of the player.</param>
        /// <param name="freeColors">The list of available (un-used) colors.</param>
        /// <param name="mpColors">The list of all multiplayer colors.</param>
        /// <param name="random">Random number generator.</param>
        public void RandomizeColor(PlayerInfo pInfo, List<int> freeColors, 
            List<MultiplayerColor> mpColors, Random random)
        {
            if (mpColors.Count == 0)
            {
                // Nothing configured to pick from. Bail rather than throw.
                ColorIndex = 0;
                return;
            }

            if (pInfo.ColorId == 0)
            {
                // The player has selected Random for their color.
                //
                // freeColors can legitimately run dry: it starts with one entry
                // per configured [MPColors] (eight in a stock install), and a
                // lobby may now hold more players than that. The previous code
                // indexed freeColors unguarded, so once exhausted
                // random.Next(0, 0) returned 0 and freeColors[0] threw
                // ArgumentOutOfRangeException and the game failed to start.
                // Note RandomizeStart below already guards its equivalent
                // empty-list case; this one was missed.
                //
                // On exhaustion, refill from the full colour set so colours
                // repeat instead of the launch failing. Every colour is used
                // once before any is reused.
                //
                // Determinism: this uses only the shared seeded Random already
                // threaded through here, and the refill is a plain ascending
                // rebuild, so every client computes the same result for the same
                // seed. Do not introduce another RNG here.
                if (freeColors.Count == 0)
                {
                    for (int cId = 0; cId < mpColors.Count; cId++)
                        freeColors.Add(cId);
                }

                int randomizedColorIndex = random.Next(0, freeColors.Count);
                int actualColorId = freeColors[randomizedColorIndex];

                ColorIndex = mpColors[actualColorId].GameColorIndex;
                freeColors.RemoveAt(randomizedColorIndex);
            }
            else
            {
                // Clamp rather than trust: ColorId is a 1-based dropdown index
                // (0 == Random), so a stale or out-of-range value from the
                // network or a saved skirmish config would index past the list.
                int colorId = pInfo.ColorId - 1;
                if (colorId < 0 || colorId >= mpColors.Count)
                    colorId = 0;

                ColorIndex = mpColors[colorId].GameColorIndex;
                freeColors.Remove(colorId);
            }
        }

        /// <summary>
        /// Applies the player's starting location into the information and
        /// randomizes it if necessary. If the starting location is randomized,
        /// the starting location is removed from the list of available starting locations.
        /// </summary>
        /// <param name="pInfo">The PlayerInfo of the player.</param>
        /// <param name="freeStartingLocations">List of free starting locations.</param>
        /// <param name="random">Random number generator.</param>
        /// <param name="takenStartingLocations">A list of starting locations that are already occupied.</param>
        /// <param name="overrideGameRandomLocations"></param>
        /// <returns>True if the player's starting location index exceeds the map's number of starting waypoints,
        /// otherwise false.</returns>
        public void RandomizeStart(
            PlayerInfo pInfo, 
            Random random,
            List<int> freeStartingLocations, 
            List<int> takenStartingLocations,
            bool overrideGameRandomLocations
        )
        {
            overrideGameRandomLocations |= ClientConfiguration.Instance.UseClientRandomStartLocations;
            if (IsSpectator)
            {
                StartingWaypoint = 90;
                return;
            }

            if (pInfo.StartingLocation == 0)
            {
                // Randomize starting location

                if (!overrideGameRandomLocations)
                {
                    // The game uses its own randomization logic that places
                    // randomized players on the opposite side of the map
                    // Players seem to prefer this behaviour, so use -1 to
                    // leave randomizing the starting location to the game itself
                    RealStartingWaypoint = -1;
                    StartingWaypoint = -1;
                    return;
                }

                // Let the client pick starting positions.
                if (freeStartingLocations.Count == 0) // No free starting locs available
                {
                    RealStartingWaypoint = -1;
                    StartingWaypoint = -1;
                    return;
                }

                int waypointIndex = random.Next(0, freeStartingLocations.Count);
                RealStartingWaypoint = freeStartingLocations[waypointIndex];
                StartingWaypoint = RealStartingWaypoint;
                freeStartingLocations.Remove(StartingWaypoint);
                return;
            }

            // Use the player's selected starting location
            RealStartingWaypoint = pInfo.StartingLocation - 1;

            if (takenStartingLocations.Contains(RealStartingWaypoint))
            {
                StartingWaypoint = -1; // Unknown starting location, stacked with another player
                return;
            }

            takenStartingLocations.Add(RealStartingWaypoint);

            StartingWaypoint = RealStartingWaypoint;
        }
    }
}
