using System;
using UnityEngine;

namespace League
{
    [Serializable]
    public struct RaceDayFormation
    {
        [Tooltip("Teams participating in this race day")]
        public Team[] teams;
        [Tooltip("Positions each team finished in this race")]
        public int[] positions;
    }
}

