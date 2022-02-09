
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class Card : UdonSharpBehaviour
{
    public DeckHandler deckHandler;

    public override void OnPickup() // on pickup move card to looseCards parent
    {

        if (deckHandler.GetID(gameObject) != 2)
        {
            if (transform.parent.name.ToLower() == "parent")
            {
                Networking.SetOwner(Networking.LocalPlayer, transform.parent.parent.gameObject);
            }
            else
            {
                Networking.SetOwner(Networking.LocalPlayer, transform.parent.gameObject);
            }
        }
        deckHandler.SetParent(gameObject, 2);
    }


    public override void OnDrop()
    {
        GameObject closest = GetClosest();

        float distance = Vector3.Distance(transform.position, closest.transform.position);

        if (closest && distance <= deckHandler.maxCardDistance)
        {
            Debug.Log($"[EasyUdonCards] : Card dropped on:{closest.name}, isCard:{isCard}");

            if (!isCard) // if we're dropping onto a deck 
            {
                if (!Networking.IsOwner(closest) && (bool)((UdonBehaviour)closest.GetComponent(typeof(UdonBehaviour))).GetProgramVariable("isHolding"))
                {
                    Debug.Log($"[EasyUdonCards] : Deck is held by someone else");
                    return;
                }
                for (int i = 0; i < deckHandler.parents.Length; i++)
                {
                    if (deckHandler.parents[i].name == closest.name)
                    {
                        Debug.Log($"[EasyUdonCards] : {gameObject.name} dropped on {i}");
                        Networking.SetOwner(Networking.LocalPlayer, closest.gameObject);
                        deckHandler.SetParent(gameObject, i);
                        break;
                    }
                }
            }
            else // if we're dropping onto a card
            {
                if (deckHandler.parents[1].childCount > 0) // check if there's an available inactiveDeck
                {
                    Transform deck = deckHandler.parents[1].GetChild(0); // get inactiveDeck
                    Networking.SetOwner(Networking.LocalPlayer, deck.gameObject);
                    deck.position = closest.transform.position; // move deck to card
                    deck.rotation = closest.transform.rotation; // move deck to card

                    for (int i = 0; i < deckHandler.parents.Length; i++) // search for deck in list of parents (so we can get it's ID)
                    {
                        if (deckHandler.parents[i].name == deck.name) // check if current parent is equal to deck
                        {
                            Networking.SetOwner(Networking.LocalPlayer, closest);
                            deckHandler.SetParent(gameObject, i); // move this card to deck
                            deckHandler.SetParent(closest, i); // move other card to deck
                            deckHandler.SetParent(deck.gameObject, 0); // move deck to aciveDecks
                            break;
                        }
                    }
                }
            }
        }
    }

    bool isCard = false;
    private GameObject GetClosest()
    {
        GameObject closest = null;

        foreach (Transform deck in deckHandler.parents[0]) // for each deck in activeDecks
        {
            if (!closest)
            {
                closest = deck.gameObject;
                isCard = false;
            }
            else if (Vector3.Distance(transform.position, deck.position) < Vector3.Distance(transform.position, closest.transform.position))
            {
                closest = deck.gameObject;
                isCard = false;
            }
        }

        foreach (Transform card in deckHandler.parents[2]) // for each card in looseCards
        {
            if (card != transform) // make sure we didn't find ourself
            {
                if (!closest)
                {
                    closest = card.gameObject;
                    isCard = true;
                }
                else if (Vector3.Distance(transform.position, card.position) < Vector3.Distance(transform.position, closest.transform.position))
                {
                    closest = card.gameObject;
                    isCard = true;
                }
            }
        }

        return closest;
    }
}
