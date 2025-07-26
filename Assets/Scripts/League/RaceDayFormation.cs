using System;
using System.Collections.Generic;
using UnityEngine;

namespace League
{
    [Serializable]
    public class RaceDayFormation
    {
        [Tooltip("All races happening on this race day")]
        public List<Race> races = new List<Race>();
        [Tooltip("Whether this race day has been processed")]
        public bool processed = false;
    }
    
    [Serializable]
    public class Race
    {
        [Tooltip("Teams participating in this race day")]
        public Team[] teams;
        [Tooltip("Positions each team finished in this race")]
        public int[] positions;
        [Tooltip("Whether this race has been processed")]
        public bool processed = false;
    }
}

