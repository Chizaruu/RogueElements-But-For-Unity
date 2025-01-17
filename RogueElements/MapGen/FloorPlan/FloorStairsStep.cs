﻿// <copyright file="FloorStairsStep.cs" company="Audino">
// Copyright (c) Audino
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;

namespace RogueElements
{
    /// <summary>
    /// Adds the entrance and exit to the floor.  Is room-conscious.
    /// The algorithm will try to place them far away from each other in different rooms.
    /// </summary>
    /// <typeparam name="TGenContext"></typeparam>
    /// <typeparam name="TEntrance"></typeparam>
    /// <typeparam name="TExit"></typeparam>
    [Serializable]
    public class FloorStairsStep<TGenContext, TEntrance, TExit> : GenStep<TGenContext>
        where TGenContext : class, IFloorPlanGenContext, IPlaceableGenContext<TEntrance>, IPlaceableGenContext<TExit>
        where TEntrance : IEntrance
        where TExit : IExit
    {
        public FloorStairsStep()
        {
            this.Entrances = new List<TEntrance>();
            this.Exits = new List<TExit>();
            this.Filters = new List<BaseRoomFilter>();
        }

        public FloorStairsStep(TEntrance entrance, TExit exit)
        {
            this.Entrances = new List<TEntrance> { entrance };
            this.Exits = new List<TExit> { exit };
            this.Filters = new List<BaseRoomFilter>();
        }

        public FloorStairsStep(List<TEntrance> entrances, List<TExit> exits)
        {
            this.Entrances = entrances;
            this.Exits = exits;
            this.Filters = new List<BaseRoomFilter>();
        }

        /// <summary>
        /// List of entrance objects to spawn.
        /// </summary>
        public List<TEntrance> Entrances { get; }

        /// <summary>
        /// List of exit objects to spawn.
        /// </summary>
        public List<TExit> Exits { get; }

        /// <summary>
        /// Used to filter out rooms that do not make suitable entrances/exits, such as boss rooms.
        /// </summary>
        public List<BaseRoomFilter> Filters { get; set; }

        public override void Apply(TGenContext map)
        {
            List<int> free_indices = new List<int>();
            for (int ii = 0; ii < map.RoomPlan.RoomCount; ii++)
            {
                if (!BaseRoomFilter.PassesAllFilters(map.RoomPlan.GetRoomPlan(ii), this.Filters))
                    continue;
                free_indices.Add(ii);
            }

            List<int> used_indices = new List<int>();

            Loc defaultLoc = Loc.Zero;

            for (int ii = 0; ii < this.Entrances.Count; ii++)
            {
                Loc? start = this.GetOutlet<TEntrance>(map, free_indices, used_indices);

                if (!start.HasValue)
                    start = this.GetOutlet<TEntrance>(map, used_indices, null);
                if (!start.HasValue)
                    start = defaultLoc;

                ((IPlaceableGenContext<TEntrance>)map).PlaceItem(start.Value, this.Entrances[ii]);
                GenContextDebug.DebugProgress(nameof(this.Entrances));
            }

            for (int ii = 0; ii < this.Exits.Count; ii++)
            {
                Loc? end = this.GetOutlet<TExit>(map, free_indices, used_indices);

                if (!end.HasValue)
                    end = this.GetOutlet<TExit>(map, used_indices, null);
                if (!end.HasValue)
                    end = defaultLoc;

                ((IPlaceableGenContext<TExit>)map).PlaceItem(end.Value, this.Exits[ii]);
                GenContextDebug.DebugProgress(nameof(this.Exits));
            }
        }

        public override string ToString()
        {
            return string.Format("{0}", this.GetType().Name);
        }

        /// <summary>
        /// Attempt to choose an outlet in a room with no entrance/exit, and updates their availability.  If none exists, default to a chosen room.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="map"></param>
        /// <param name="free_indices"></param>
        /// <param name="used_indices"></param>
        /// <returns></returns>
        protected virtual Loc? GetOutlet<T>(TGenContext map, List<int> free_indices, List<int> used_indices)
            where T : ISpawnable
        {
            while (free_indices.Count > 0)
            {
                int roomIndex = map.Rand.Next() % free_indices.Count;
                int startRoom = free_indices[roomIndex];

                List<Loc> tiles = ((IPlaceableGenContext<T>)map).GetFreeTiles(map.RoomPlan.GetRoom(startRoom).Draw);

                if (tiles.Count == 0)
                {
                    // this room is not suitable and never will be, remove it
                    free_indices.RemoveAt(roomIndex);
                    continue;
                }

                Loc start = tiles[map.Rand.Next(tiles.Count)];

                // if we have a used-list, transfer the index over
                if (used_indices != null)
                {
                    free_indices.RemoveAt(roomIndex);
                    used_indices.Add(startRoom);
                }

                return start;
            }

            return null;
        }
    }
}
