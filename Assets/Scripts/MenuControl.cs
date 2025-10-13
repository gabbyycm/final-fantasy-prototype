using UnityEngine;


public class MenuControl : MonoBehaviour
{
    //i don't think he taught '[SerializeField]' but it's basically a way to make an object private and editable within unity
    [SerializeField] private GameObject menu, cursor, firstButton;
    [SerializeField] private float distanceBetweenButtons, cursorOffset;
    [SerializeField] private int maxButtons;
    private int cursorIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //these should be set anyways so it's up to y'all if you want them
        if (menu == null)
        {
            menu = transform.Find("Menu")?.gameObject;
        }
        if (cursor == null)
        {
            cursor = transform.Find("Cursor")?.gameObject;
        }
        if (firstButton == null) //this also wouldn't work i'm just too lazy to make a loop to check all of the children of the canvas
        {
            firstButton = transform.Find("FirstButton")?.gameObject;
        }

        cursor.transform.SetPositionAndRotation(new Vector3(firstButton.transform.position.x - firstButton.GetComponent<RectTransform>().rect.width / 2 - cursorOffset - cursor.GetComponent<RectTransform>().rect.width / 2, firstButton.transform.localPosition.y, firstButton.transform.position.z), transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menu.SetActive(!menu.activeSelf);
            GameState.paused = menu.activeSelf; //just in case they get desynced, this'll fix it
        }
        if (!menu.activeSelf) //don't need to do anything if the menu isn't up
        {
            return;
        }

        bool cursorMoved = false;
        if ((Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) && cursorIndex < maxButtons)
        {
            cursorIndex++;
            cursorMoved = true;
        }
        else if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) && cursorIndex > 0)
        {
            cursorIndex--;
            cursorMoved = true;
        }
        if (cursorMoved) //very ugly statements but i don't know of a better way of doing it (could save both rects into variables, but this is also a prototype so it doesn't matter)
        {
            float cursorY = -cursorIndex * (firstButton.GetComponent<RectTransform>().rect.height + distanceBetweenButtons) + firstButton.transform.position.y + cursor.GetComponent<RectTransform>().rect.height - firstButton.GetComponent<RectTransform>().rect.height / 2;
            cursor.transform.localPosition = new Vector3(cursor.transform.position.x, cursorY, cursor.transform.position.z);
        }
        
    }
}
