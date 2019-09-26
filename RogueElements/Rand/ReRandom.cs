﻿// <copyright file="ReRandom.cs" company="Audino">
// Copyright (c) Audino
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace RogueElements
{
    /// <summary>
    /// A custom random class that holds on to its seed, for repeatability.
    /// </summary>
    [Serializable]
    public class ReRandom : IRandom
    {
        private readonly ulong[] s;

        public ReRandom()
            : this(unchecked((ulong)System.DateTime.Now.Ticks))
        {
        }

        public ReRandom(ulong seed)
        {
            this.FirstSeed = seed;

            this.s = new ulong[4];
            this.s[0] = SplitMix64(this.FirstSeed);
            this.s[1] = SplitMix64(this.s[0]);
            this.s[2] = SplitMix64(this.s[1]);
            this.s[3] = SplitMix64(this.s[2]);
        }

        /// <summary>
        /// The seed value that the class was initialized with.
        /// </summary>
        public ulong FirstSeed { get; private set; }

        /* This is xoshiro256** 1.0, one of our all-purpose, rock-solid
        generators. It has excellent (sub-ns) speed, a state (256 bits) that is
        large enough for any parallel application, and it passes all tests we
        are aware of.*/

        public virtual ulong NextUInt64()
        {
            ulong result = Rotl(this.s[1] * 5, 7) * 9;
            ulong t = this.s[1] << 17;

            this.s[2] ^= this.s[0];
            this.s[3] ^= this.s[1];
            this.s[1] ^= this.s[2];
            this.s[0] ^= this.s[3];

            this.s[2] ^= t;

            this.s[3] = Rotl(this.s[3], 45);

            return result;
        }

        public virtual int Next()
        {
            return (int)(this.NextUInt64() % int.MaxValue);
        }

        public virtual int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue), $"{nameof(minValue)} must be lower than or equal to {nameof(maxValue)}");

            Contract.EndContractBlock();

            long range = (long)maxValue - minValue;
            if (range == 0)
                return minValue;
            return (int)((long)(this.NextUInt64() % (ulong)range) + minValue);
        }

        public virtual int Next(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue), $"{nameof(maxValue)} must be equal to or greater than zero");

            Contract.EndContractBlock();
            if (maxValue == 0)
                return 0;
            return (int)(this.NextUInt64() % (ulong)maxValue);
        }

        public virtual double NextDouble()
        {
            return (double)this.NextUInt64() / ((double)ulong.MaxValue + 1);
        }

        private static ulong SplitMix64(ulong x)
        {
            ulong z = x += 0x9E3779B97F4A7C15;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            return z ^ (z >> 31);
        }

        private static ulong Rotl(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }
    }
}
