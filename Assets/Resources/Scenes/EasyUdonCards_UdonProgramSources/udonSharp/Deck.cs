
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class Deck : UdonSharpBehaviour
{
    public UdonBehaviour deckController;
    public float spreadDistance = 0.006f;
    public GameObject deck;
    public Transform topCard;
    public Transform bottomCard;
    public Transform parent;

    float maxCardDistance;
    Transform activeDecks;
    Transform inactiveDecks;
    Transform looseCards;

    bool isHolding = false;
    bool isShowing = false;

    private void Start()
    {
        maxCardDistance = (float)deckController.GetProgramVariable("maxCardDistance");
        activeDecks = (Transform)deckController.GetProgramVariable("activeDecks");
        inactiveDecks = (Transform)deckController.GetProgramVariable("inactiveDecks");
        looseCards = (Transform)deckController.GetProgramVariable("looseCards");
    }

    private void Update()
    {

        if (parent.childCount <= 1)
        {
            parent.GetChild(0).gameObject.SetActive(true);
            parent.GetChild(0).SetParent(looseCards);
            transform.SetParent(inactiveDecks);

        }
        else if (parent.childCount > 1)
        {
            transform.SetParent(activeDecks);

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

                parent.gameObject.SetActive(true);
                deck.SetActive(true);

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

                parent.gameObject.SetActive(true);
                deck.SetActive(true);

                parent.GetChild(card).position = bottomCard.position;
                parent.GetChild(card).rotation = bottomCard.rotation;
                parent.GetChild(card).gameObject.SetActive(true);
            }
            else
            {
                deck.SetActive(!isShowing);
                parent.gameObject.SetActive(isShowing);

                float position = (spreadDistance * ((parent.childCount > 12) ? 12 : parent.childCount)) / 2;
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
                        child.localPosition = new Vector3(position - (spreadDistance * i), 0, 0);
                        child.localEulerAngles = new Vector3(0, 0, rotation);
                    }

                    i++;
                }
            }
        }
    }

    public override void OnPickup()
    {
        isHolding = true;
    }
    public override void OnDrop()
    {
        isHolding = false;

        GameObject closest = GetClosest();

        if (closest && Vector3.Distance(transform.position, closest.transform.position) <= maxCardDistance)
        {
            Transform newParent = null;

            foreach (Transform child in closest.transform)
            {
                if (child.name.ToLower() == "parent")
                {
                    newParent = child;
                    break;
                }
            }
            Transform[] children = new Transform[parent.childCount];
            int i = 0;
            foreach (Transform card in parent)
            {
                children[i] = card;
                i++;
            }
            foreach (Transform card in children)
            {
                card.SetParent(newParent);
            }
            transform.SetParent(inactiveDecks);
        }
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        isShowing = (isHolding && value);
    }

    private GameObject GetClosest()
    {
        GameObject closest = null;

        if (activeDecks)
        {
            foreach (Transform child in activeDecks)
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
        }

        return closest;
    }
}
