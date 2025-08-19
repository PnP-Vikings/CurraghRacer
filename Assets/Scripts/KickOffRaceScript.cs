using System;
using UnityEngine;

public class KickOffRaceScript : MonoBehaviour
{
  private void OnEnable()
  {
    RaceManager.Instance?.StartRace();
  }
}
