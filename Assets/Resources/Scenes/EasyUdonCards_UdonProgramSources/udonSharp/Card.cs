
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class Card : UdonSharpBehaviour
{
    public UdonBehaviour deckController;

    float maxCardDistance;
    Transform activeDecks;
    Transform inactiveDecks;
    Transform looseCards;

    private void Start()
    {
        maxCardDistance = (float)deckController.GetProgramVariable("maxCardDistance");
        activeDecks = (Transform)deckController.GetProgramVariable("activeDecks");
        inactiveDecks = (Transform)deckController.GetProgramVariable("inactiveDecks");
        looseCards = (Transform)deckController.GetProgramVariable("looseCards");
    }

    public override void OnPickup()
    {
        transform.SetParent(looseCards);
        gameObject.SetActive(true);
    }


    public override void OnDrop()
    {
        gameObject.SetActive(true);
        GameObject closest = GetClosest();

        if (closest && Vector3.Distance(transform.position, closest.transform.position) <= maxCardDistance)
        {
            Transform parent = null;
            if (!isCard)
            {
                foreach (Transform child in closest.transform)
                {
                    if (child.name.ToLower() == "parent")
                    {
                        parent = child;
                        break;
                    }
                }
            }
            else
            {
                if (inactiveDecks.childCount > 0)
                {
                    Transform deck = inactiveDecks.GetChild(0);
                    deck.position = closest.transform.position;
                    deck.rotation = closest.transform.rotation;
                    foreach (Transform child in deck)
                    {
                        if (child.name.ToLower() == "parent")
                        {
                            parent = child;
                            closest.transform.SetParent(parent);
                            break;
                        }
                    }
                    deck.SetParent(activeDecks);
                }
            }

            if (parent)
            {
                transform.SetParent(parent);
                transform.localPosition = Vector3.zero;
            }
        }
    }

    bool isCard = false;
    private GameObject GetClosest()
    {
        GameObject closest = null;

        if (activeDecks)
        {
            foreach (Transform child in activeDecks)
            {
                if (!closest)
                {
                    closest = child.gameObject;
                    isCard = false;
                }
                else if (Vector3.Distance(transform.position, child.position) < Vector3.Distance(transform.position, closest.transform.position))
                {
                    closest = child.gameObject;
                    isCard = false;
                }
            }
        }

        if (looseCards)
        {
            foreach (Transform child in looseCards)
            {
                if (child != transform)
                {
                    if (!closest)
                    {
                        closest = child.gameObject;
                        isCard = true;
                    }
                    else if (Vector3.Distance(transform.position, child.position) < Vector3.Distance(transform.position, closest.transform.position))
                    {
                        closest = child.gameObject;
                        isCard = true;
                    }
                }
            }
        }

        return closest;
    }
}
