using System;
using System.Collections.Generic;

namespace Vanquish.Theatre
{
    /// <summary>
    /// Axial hex coordinate (PLAN.md "Theatre Map Representation" — a literal hex
    /// grid, not an abstract node graph). Plain, engine-agnostic value type so the
    /// whole theatre-map simulation layer can be unit-tested without Unity.
    /// </summary>
    public readonly struct HexCoordinate : IEquatable<HexCoordinate>
    {
        public readonly int Q;
        public readonly int R;

        public HexCoordinate(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>Third cube coordinate, derived — Q + R + S == 0 always.</summary>
        public int S => -Q - R;

        private static readonly HexCoordinate[] Directions =
        {
            new HexCoordinate(1, 0), new HexCoordinate(1, -1), new HexCoordinate(0, -1),
            new HexCoordinate(-1, 0), new HexCoordinate(-1, 1), new HexCoordinate(0, 1),
        };

        public IEnumerable<HexCoordinate> Neighbors()
        {
            foreach (HexCoordinate dir in Directions)
                yield return new HexCoordinate(Q + dir.Q, R + dir.R);
        }

        /// <summary>Hex (great-circle) distance — number of hex steps between two coordinates.</summary>
        public static int Distance(HexCoordinate a, HexCoordinate b)
        {
            int dq = Math.Abs(a.Q - b.Q);
            int dr = Math.Abs(a.R - b.R);
            int ds = Math.Abs(a.S - b.S);
            return Math.Max(dq, Math.Max(dr, ds));
        }

        public bool Equals(HexCoordinate other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoordinate other && Equals(other);
        public override int GetHashCode() => (Q, R).GetHashCode();
        public override string ToString() => $"({Q},{R})";

        public static bool operator ==(HexCoordinate a, HexCoordinate b) => a.Equals(b);
        public static bool operator !=(HexCoordinate a, HexCoordinate b) => !a.Equals(b);
    }
}
