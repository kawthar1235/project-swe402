using UnityEngine;

// WoodenBox adds and configures this automatically using the existing button names.
public class ChestKeypadButton : MonoBehaviour
{
    public WoodenBox Owner { get; private set; }
    private string value;
    private bool enter;

    public void Configure(WoodenBox owner, string digit, bool isEnter)
    {
        Owner = owner;
        value = digit;
        enter = isEnter;
        ChestPuzzleTools.EnsureCollider(gameObject);
    }
    public void Press()
    {
        if (Owner == null) return;
        if (enter) Owner.SubmitCode();
        else Owner.EnterNumber(value);
    }
}
