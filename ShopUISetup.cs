/*
This code handles the instantiation and sorting of a series of UI buttons in the shop screen of Oshka.
I'm proud of this code because it dynamically positions a large number of purchasables in a grid layout based on
certain elements of each purchasable (e.g. whether it has been unlocked yet)
*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUISetup : MonoBehaviour
{
    public DollData dollData;
    public Transform container;
    public CharacterCard dollButtonPrefab;
    public UnlockButtonToggle unlockButton;
    public SecretDollCard secretDollPrefab;
    public NonPurchasableDollCard nonPurchasableDollPrefab;
    public MailingListDollButtonSetup mailListDollSetup;

    List<CharacterCard> normalDollCards = new List<CharacterCard>();
    List<SecretDollCard> secretDollCards = new List<SecretDollCard>();
    List<NonPurchasableDollCard> nonPurchasableDollCards = new List<NonPurchasableDollCard>();


    // Populate Store called by BootControl script at application start 
    public void PopulateStore()
    {
        int buttonCount = dollData.dollData.Length;
        List<DollData.DollInfo> unlockedDolls = new List<DollData.DollInfo>();

        // Populate the store with purchasable dolls, placing locked dolls first
        SortCoinDoubler(unlockedDolls);
        SortDollsByLockedStatus(buttonCount, unlockedDolls);
        AddNonPurchasableDolls();
        AddUnlockedDolls(unlockedDolls);
        AddSecretDolls();
        unlockButton.Init();

        // If In App Purchase API fails, disable IAP buttons
        if(IAP_Init.Instance.DidInitFail()) DisableAllPurchasableButtons();
    }

    // If coin doubler is not unlocked, add it first so it's at the top of the icon list
    // Else add it to the list of already unlocked dolls to get sorted to the bottom of the icon list
    void SortCoinDoubler(List<DollData.DollInfo> unlockedDolls)
    {
        DollData.DollInfo coinDoubler = dollData.GetDollOfType(DollData.Doll.CoinDoubler);

        if (!SaveData.Instance.GetIsUnlocked(coinDoubler.productID))
            InstantiateDollCard(coinDoubler);
        else
            unlockedDolls.Add(coinDoubler);
    }

    // Instantiate locked dolls at the top of the icon list
    void SortDollsByLockedStatus(int buttonCount, List<DollData.DollInfo> unlockedDolls)
    {
        // Sort through dolls to instantiate locked ones
        for (int i = 0; i < buttonCount; i++)
        {
            DollData.DollInfo doll = dollData.dollData[i];

            // Skip over non purchasable, coin doubler and secret dolls - we add them later
            if (doll.dollType != DollData.Doll.CoinDoubler && doll.isSecret == false && !doll.isNonPurchasable)
            {
                // If doll is already unlocked, add it to the unlocked list to be instantited later
                // Else instantiate the locked doll
                if (SaveData.Instance.GetIsUnlocked(doll.productID))
                    unlockedDolls.Add(doll);
                else
                    InstantiateDollCard(doll);
            }
        }
    }

    // Instantiate unlocked dolls after the locked ones so they are further down in the icon list
    void AddUnlockedDolls(List<DollData.DollInfo> unlockedDolls)
    {
        for (int i = 0; i < unlockedDolls.Count; i++)
        {
            if(!unlockedDolls[i].isNonPurchasable)
                InstantiateDollCard(unlockedDolls[i]);
        }
    }

    void InstantiateDollCard(DollData.DollInfo doll)
    {
        CharacterCard newButton = Instantiate(dollButtonPrefab, container);
        string productID = doll.productID;
        string nameKey = doll.nameKey;
        Sprite image = doll.previewImage;

        newButton.Init(doll.dollType, nameKey, productID, image);
        newButton.Setup();
        normalDollCards.Add(newButton);
    }

    // Add undiscovered secret dolls to the icon list
    void AddSecretDolls()
    {
        DollData.DollInfo[] allDolls = dollData.dollData;
        List<DollData.DollInfo> secretDolls = new List<DollData.DollInfo>();

        foreach(DollData.DollInfo doll in allDolls)
        {
            if(doll.isSecret) secretDolls.Add(doll);
        }

        if(secretDolls.Count > 0)
        {
            foreach(DollData.DollInfo secretDoll in secretDolls)
            {
                bool isDollUnlocked = SaveData.Instance.GetIsUnlocked(secretDoll.productID);
                SecretDollCard secretDollCard = Instantiate(secretDollPrefab, container);
                string productID = secretDoll.productID;
                string nameKey = secretDoll.nameKey;
                Sprite image = secretDoll.previewImage;

                secretDollCard.Init(secretDoll.dollType, nameKey, productID, image);
                secretDollCard.Setup();
                secretDollCards.Add(secretDollCard);
            }
        }
    }

    // Add non purchasable dolls to the icon list
    void AddNonPurchasableDolls()
    {
        DollData.DollInfo[] allDolls = dollData.dollData;
        var nonPurchasableDolls = new List<DollData.DollInfo>();
        var lockedDolls = new List<DollData.DollInfo>();
        var unlockedDolls = new List<DollData.DollInfo>();

        foreach (DollData.DollInfo doll in allDolls)
        {
            if (doll.isNonPurchasable) nonPurchasableDolls.Add(doll);
        }

        // Sort non purchasable dolls into locked and unlocked lists
        if (nonPurchasableDolls.Count > 0)
        {
            foreach (DollData.DollInfo doll in nonPurchasableDolls)
            {
                if(SaveData.Instance.GetIsUnlocked(doll.productID))
                    unlockedDolls.Add(doll);
                else
                    lockedDolls.Add(doll);
            }
        }

        // Instantiate dolls based on locked status
        if(lockedDolls.Count > 0)
        {
            foreach (DollData.DollInfo lockedDoll in lockedDolls)
            {
                InstantiateNonPurchasableDollCard(lockedDoll);
            }
        }

        if(unlockedDolls.Count > 0)
        {
            foreach (DollData.DollInfo unlockedDoll in unlockedDolls)
            {
                InstantiateNonPurchasableDollCard(unlockedDoll);
            }
        }
    }

    void InstantiateNonPurchasableDollCard(DollData.DollInfo doll)
    {
        NonPurchasableDollCard nonPurchasableDollCard = Instantiate(nonPurchasableDollPrefab, container);
        string productID = doll.productID;
        string nameKey = doll.nameKey;
        Sprite image = doll.previewImage;

        nonPurchasableDollCard.Init(doll.dollType, nameKey, productID, image);
        nonPurchasableDollCard.Setup();
        nonPurchasableDollCards.Add(nonPurchasableDollCard);
    }

    // This is run every time the Store scene is activated, sorting the already instantiated dolls
    public void UpdateStore()
    {
        // Update shop coin UI 
        ShopCoinStuff.Instance.UpdateCoins();

        var purchasableList = new List<CharacterCard>();
        var lockedNonPurchasableList = new List<NonPurchasableDollCard>();
        var unlockedList = new List<BaseCard>();

        // Sort already instantiated normal doll cards into locked and unlocked lists
        foreach (CharacterCard dollCard in normalDollCards)
        {
            string productID = dollCard.GetProductID();
            bool isUnlocked = SaveData.Instance.GetIsUnlocked(productID);
            dollCard.SetIAPButtonActive(!isUnlocked);

            if (isUnlocked)
                unlockedList.Add(dollCard);
            else
                purchasableList.Add(dollCard);
        }

        // Sort already instantiated non-purchasable doll cards into locked and unlocked lists
        foreach (NonPurchasableDollCard nonPurchasable in nonPurchasableDollCards)
        {
            string productID = nonPurchasable.GetProductID();
            bool isUnlocked = SaveData.Instance.GetIsUnlocked(productID);

            if (isUnlocked)
            {
                nonPurchasable.DisableBuyButton();
                unlockedList.Add(nonPurchasable);
            }
            else
                lockedNonPurchasableList.Add(nonPurchasable);
        }

        foreach(SecretDollCard secretDoll in secretDollCards)
        {
            secretDoll.Setup();
        }

        unlockButton.Init();
        mailListDollSetup.UpdateActiveStatus();
        SortDolls(purchasableList, lockedNonPurchasableList, unlockedList);

        // If In App Purchase API fails, disable IAP buttons
        if (IAP_Init.Instance.DidInitFail())
        {
            DisableAllPurchasableButtons();
        }
    }

    void SortDolls(List<CharacterCard> lockedList, List<NonPurchasableDollCard> lockedNonPurchasables, List<BaseCard> unlockedList)
    {
        int index = 0;
        if(mailListDollSetup.gameObject.activeSelf) index++;

        // Do coin doubler sorting
        string coinDoublerID = dollData.GetDollProductID(DollData.Doll.CoinDoubler);
                
        foreach(CharacterCard card in lockedList)
        {
            if(string.Equals(card.GetProductID(), coinDoublerID))
            {
                card.transform.SetSiblingIndex(index);
                index++;
            }
        }

        // Sort locked dolls to top
        for (int i = 0; i < lockedList.Count; i++)
        {
            if (string.Equals(lockedList[i].GetProductID(), coinDoublerID))
                continue;

            lockedList[i].transform.SetSiblingIndex(index);
            print($"set {lockedList[i].GetProductID()} to index: {index.ToString()}");
            index++;
        }

        // Position locked non-purchasable dolls after locked normal dolls
        for (int i = 0; i < lockedNonPurchasables.Count; i++)
        {
            lockedNonPurchasables[i].transform.SetSiblingIndex(index);
            print($"set {lockedList[i].GetProductID()} to index: {index.ToString()}");
            index++;
        }

        // Position unlocked dolls after the above
        for (int j = 0; j < unlockedList.Count; j++)
        {
            unlockedList[j].transform.SetSiblingIndex(index);
            print($"set {unlockedList[j].GetProductID()} to index: {index.ToString()}");
            index++;
        }
    }

    public void DisableAllPurchasableButtons()
    {
        foreach(CharacterCard doll in normalDollCards)
        {
            doll.DisableBuyButton();
        }
    }
}
