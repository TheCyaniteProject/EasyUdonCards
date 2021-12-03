
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class DeckHandler : UdonSharpBehaviour
{
    [Header("Distance that cards and decks will combine")]
    public float maxCardDistance = 0.1f;
    [Header("Distance that cards in a deck will spread apart when showing hand")]
    public float spreadDistance = 0.006f;
    [Header("List of all the parents that cards and decks can be children of")]
    public Transform[] parents;
    [Header("List of all decks and cards managed by this handler")]
    public GameObject[] objects;
    [Header("Set parentIDs' length to objects' length. (I can't cause of a bug) Don't set the values, they will be overwritten on startup")]
    public int[] parentIDs; // -1 = null parent, 0 = activeDecks, 1 = inactiveDecks, 2 = looseCards, anything else is a deck


    // hidden logic
    int[] _parentIDs = new int[0];
    [UdonSynced]
    private int[] networkIDs = new int[0]; // parents are synced using int-ids for performance
    int[] _networkIDs = new int[0];

    private void Start()
    {

        bool hasParent = false;
        for (int i = 0; i < objects.Length; i++) // for each synced object
        {
            for (int id = 0; id < parents.Length; id++) // for each potential parent
            {
                if (objects[i] != null && objects[i].transform.parent.name.ToLower() == "parent") // if we're a card in a deck (all card decks have a child called "parent")
                {
                    if (objects[i].transform.parent.parent.name == parents[id].name) // if the deck name is the same as the parent we're checking
                    {
                        parentIDs[i] = id; // save id
                        hasParent = true;
                        break;
                    }
                }
                else // we're not in a deck
                {
                    if (objects[i] != null && objects[i].transform.parent.name == parents[id].name) // check if the object name is the same as the parent we're checking
                    {
                        parentIDs[i] = id; // save id
                        hasParent = true;
                        break;
                    }
                }
            }
            if (!hasParent)
                parentIDs[i] = -1; // if we don't have a parent, set null
            hasParent = false;
        }

        if (networkIDs == null || networkIDs.Length != parentIDs.Length)
            networkIDs = new int[parentIDs.Length];
        for (int i = 0; i < networkIDs.Length; i++)
        {
            networkIDs[i] = parentIDs[i];
        }

        if (_networkIDs == null || _networkIDs.Length != parentIDs.Length)
            _networkIDs = new int[parentIDs.Length];
        for (int i = 0; i < _networkIDs.Length; i++)
        {
            _networkIDs[i] = parentIDs[i];
        }
    }

    public void SetParent(GameObject obj, int value) // -1 = null parent, 0 = activeDecks, 1 = inactiveDecks, 2 = looseCards, anything else is a deck
    {
        for (int i = 0; i < objects.Length; i++) // go through all the objects so we can find the current object's ID
        {
            if (objects[i] != null && objects[i].name == obj.name) // compair child to obj
            {
                //Debug.Log($"[EasyUdonCards] : Set parent:{obj.name}, ID:{i}, value:{value}");
                parentIDs[i] = value; // set parent id
                return;
            }
        }
    }

    bool CheckLocal()
    {
        if (parentIDs.Length != _parentIDs.Length) return true;

        for (int i = 0; i < parentIDs.Length; i++)
        {
            if (parentIDs[i] != _parentIDs[i]) return true;
        }
        return false;
    }
    void FixLocal()
    {
        Debug.Log($"[EasyUdonCards] : Fixing local arrays..");

        if (_parentIDs == null || _parentIDs.Length != parentIDs.Length)
            _parentIDs = new int[parentIDs.Length];
        for (int i = 0; i < parentIDs.Length; i++)
        {
            _parentIDs[i] = parentIDs[i];
        }
    }

    bool CheckNetwork()
    {
        if (networkIDs.Length != _networkIDs.Length) return true;

        for (int i = 0; i < networkIDs.Length; i++)
        {
            if (networkIDs[i] != _networkIDs[i]) return true;
        }

        if (networkIDs.Length != parentIDs.Length) return true;

        for (int i = 0; i < networkIDs.Length; i++)
        {
            if (networkIDs[i] != parentIDs[i]) return true;
        }

        return false;
    }
    void FixNetwork()
    {
        Debug.Log($"[EasyUdonCards] : Fixing networked arrays..");

        if (_networkIDs == null || _networkIDs.Length != networkIDs.Length)
            _networkIDs = new int[networkIDs.Length];
        for (int i = 0; i < networkIDs.Length; i++)
        {
            _networkIDs[i] = networkIDs[i];
        }
    }

    bool needNetworkUpdate = false;
    private void Update()
    {
        if (CheckLocal())
        {
            //Debug.Log($"[EasyUdonCards] : Updating local parents..");
            for (int i = 0; i < objects.Length; i++) // loop through all the objects
            {
                if (parentIDs[i] != -1) // if we have a parent - parents are synced using int-ids for performance
                {
                    bool isDeckWithParent = false; // parent is valid
                    int id = parentIDs[i];
                    if (parents[id].name.ToLower().Contains("deck")) // if we're in a deck, we need to find the "parent" object
                    {
                        foreach (Transform child in parents[id]) // for each child in deck
                        {
                            if (child.name.ToLower() == "parent") // if this is the parent
                            {
                                objects[i].transform.SetParent(child); // set parent
                                objects[i].SetActive(true); // reset active state
                                isDeckWithParent = true; // we've found the parent
                                break; // end loop to save performance
                            }
                        }
                    }

                    if (objects[i].name.ToLower().Contains("deck") && id == 1) // if we're a deck in inactiveDecks we need to reset our position
                    {
                        objects[i].transform.position = parents[id].transform.position;
                    }

                    if (!isDeckWithParent && objects[i].transform.parent.name != parents[id].name)// if deck parent hasn't been set, and we're not where we're supposed to be
                    {
                        objects[i].transform.SetParent(parents[id]); // set parent
                        objects[i].SetActive(true); // reset active state (mainly for cards that came from decks)
                    }
                    //Debug.Log($"[EasyUdonCards] : Parent:{objects[i].name}, ID:{parentIDs[i]}, isDeckWithParent:{isDeckWithParent}");
                }
            }
            FixLocal();
        }

        if (CheckNetwork())
        {
            Debug.Log($"[EasyUdonCards] : Updating network..");
            needNetworkUpdate = false; // if something has changed locally and we need to tell the network

            for (int i = 0; i < networkIDs.Length; i++) // loop through all networked objects
            {
                if (networkIDs[i] != _networkIDs[i]) // check network first, network has priorety
                {
                    parentIDs[i] = networkIDs[i];
                }
                else if (_networkIDs[i] != parentIDs[i]) // if network hasn't changed, check local
                {
                    networkIDs[i] = parentIDs[i];
                    needNetworkUpdate = true;
                }
            }
            FixNetwork();
            if (needNetworkUpdate)
            {
                Debug.Log($"[EasyUdonCards] : Requesting Serialization..");
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                RequestSerialization();
                needNetworkUpdate = false;
            }
        }
    }
}
