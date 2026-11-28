
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Slots")]
    public Image[] itemSlots;

    [Header("Item Sprites")]
    public Sprite cdSprite;
    public Sprite keySprite;
    public Sprite flashlightSprite;

    void Awake()
    {
        foreach (Image slot in itemSlots)
        {
            slot.sprite = null;
            slot.enabled = false;
        }
    }

    public void AddItem(Sprite item)
    {
        if (item == null) return;

        foreach (Image slot in itemSlots)
        {
            if (slot.sprite == item)
                return;
        }

        foreach (Image slot in itemSlots)
        {
            if (slot.sprite == null)
            {
                slot.sprite = item;
                slot.preserveAspect = true;
                slot.enabled = true;
                return;
            }
        }

        Debug.Log("Inventory is full!");
    }

    public void RemoveItem(Sprite item)
    {
        if (item == null) return;

        foreach (Image slot in itemSlots)
        {
            if (slot.sprite == item)
            {
                slot.sprite = null;
                slot.enabled = false;
                return;
            }
        }
    }

    public void CollectCD()
    {
        AddItem(cdSprite);
    }

    public void CollectKey()
    {
        AddItem(keySprite);
    }

    public void CollectFlashlight()
    {
        AddItem(flashlightSprite);
    }

    public void UseCD()
    {
        RemoveItem(cdSprite);
    }

    public void UseKey()
    {
        RemoveItem(keySprite);
    }
}

