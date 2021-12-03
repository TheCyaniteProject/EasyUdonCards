
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class Deck : UdonSharpBehaviour
{
    public DeckHandler deckHandler;
    public GameObject deck;
    public Transform topCard;
    public Transform bottomCard;
    public Transform parent;

    [UdonSynced]
    bool isHolding = false;
    [UdonSynced]
    bool isShowing = false;

    private void Update()
    {
        if (transform.parent.name.ToLower() == "activedecks")
        {
            if (parent.childCount <= 1) // if we have 1 or no cards, 
            {
                if (parent.childCount == 1) // if we have a card
                {
                    parent.GetChild(0).gameObject.SetActive(true); // show card before we hide deck
                    deckHandler.SetParent(parent.GetChild(0).gameObject, 2); // if we only have one card, set card as looseCard
                }
                deckHandler.SetParent(gameObject, 1); // set as inactiveDeck

            }
            else if (parent.childCount > 1) // if we have cards
            {

                Vector3 euler = transform.eulerAngles;

                Vector3 heading = (transform.position + new Vector3(0, 20, 0)) - transform.position;

                float dot = Vector3.Dot(heading, transform.up);

                if (dot > 17f) // if deck is laying face down
                {
                    int card = Random.Range(0, parent.childCount - 1);

                    foreach (Transform child in parent)
                    {
                        child.gameObject.SetActive(false);
                    }

                    parent.gameObject.SetActive(true); // card container
                    deck.SetActive(true); // visual deck

                    parent.GetChild(card).position = topCard.position;
                    parent.GetChild(card).rotation = topCard.rotation;
                    parent.GetChild(card).gameObject.SetActive(true);
                }
                else if (dot < -17f) // if deck is laying face up
                {
                    int card = parent.childCount - 1;

                    foreach (Transform child in parent)
                    {
                        child.gameObject.SetActive(false);
                    }

                    parent.gameObject.SetActive(true); // card container
                    deck.SetActive(true); // visual deck

                    parent.GetChild(card).position = bottomCard.position;
                    parent.GetChild(card).rotation = bottomCard.rotation;
                    parent.GetChild(card).gameObject.SetActive(true);
                }
                else
                {
                    parent.gameObject.SetActive(isShowing); // card container
                    deck.SetActive(!isShowing); // visual deck

                    float position = (deckHandler.spreadDistance * ((parent.childCount > 12) ? 12 : parent.childCount)) / 2;
                    float rotation = -3;

                    int i = 0;
                    foreach (Transform child in parent)
                    {
                        if (i >= 12)
                        {
                            child.gameObject.SetActive(false);
                        }
                        else
                        {
                            child.gameObject.SetActive(true);
                            child.localPosition = new Vector3(position - (deckHandler.spreadDistance * i), 0, 0);
                            child.localEulerAngles = new Vector3(0, 0, rotation);
                        }

                        i++;
                    }
                }
            }
        }
        else if (parent.childCount > 1)
        {
            deckHandler.SetParent(gameObject, 0); // set as activeDeck
        }
    }

    public override void OnPickup()
    {
        isHolding = true;
        Debug.Log($"[EasyUdonCards] : Deck:{gameObject.name}, parent:{transform.parent.name}, children:{parent.childCount}");
    }
    public override void OnDrop()
    {
        isHolding = false;

        int newParent = GetClosest();

        if (newParent > -1)
        {
            Debug.Log($"[EasyUdonCards] : Deck dropped on deck");
            foreach (Transform card in parent)
            {
                deckHandler.SetParent(card.gameObject, newParent);
            }
            deckHandler.SetParent(gameObject, 1);
        }
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        isShowing = (isHolding && value);
    }

    private int GetClosest()
    {
        GameObject closest = null;

        foreach (Transform child in deckHandler.parents[0])
        {
            if (child != transform)
            {
                if (!closest)
                {
                    closest = child.gameObject;
                }
                else if (Vector3.Distance(transform.position, child.position) < Vector3.Distance(transform.position, closest.transform.position))
                {
                    closest = child.gameObject;
                }
            }
        }

        int i = 0;
        if (closest != null)
        {
            foreach (Transform parent in deckHandler.parents)
            {
                if (parent.name == closest.name && Vector3.Distance(transform.position, closest.transform.position) <= deckHandler.maxCardDistance)
                    return i;
            }
            i++;
        }

        return -1;
    }
}
