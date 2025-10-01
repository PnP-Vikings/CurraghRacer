using UnityEngine;

public class Bill : MonoBehaviour
{
    public string billName;
    public float amount;
    public int dueDay; // Day of the month the bill is due
    public bool isPaid = false;
    public BillType billType;

    /// <summary>
    /// Pay the bill if the player has enough coins.
    /// </summary>
    /// <returns>True if the bill was paid, false otherwise.</returns>
    public bool PayBill()
    {
        if (PlayerManager.Instance.coins >= amount && !isPaid)
        {
            PlayerManager.Instance.coins -= amount;
            isPaid = true;
            Debug.Log($"Bill '{billName} {billType}  paid. Amount: {amount}");
            return true;
        }
        Debug.Log($"Not enough coins to pay bill '{billName}' or it is already paid.");
        return false;
    }

    /// <summary>
    /// Reset the bill status for a new billing cycle.
    /// </summary>
    public void ResetBill()
    {
        isPaid = false;
        Debug.Log($"Bill '{billName}' has been reset for the new billing cycle.");
    }
}

public enum BillType
{
    Rent,
    Utilities,
    LoanPayment,
    Other
}
