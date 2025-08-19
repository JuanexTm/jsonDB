using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class HttpTest : MonoBehaviour
{


    [SerializeField] private int characterId = 102;
    private string APIUrl = "https://my-json-server.typicode.com/JuanexTm/jsonDB/users/";
    private string RicKandMortyUrl = "https://rickandmortyapi.com/api/character/";
    [SerializeField] private RawImage[] rawImages;
    [SerializeField] private TextMeshProUGUI[] idTexts;
    [SerializeField] private TextMeshProUGUI[] nameTexts;
    [SerializeField] private TextMeshProUGUI[] speciesTexts;

    [SerializeField] private TMP_Dropdown userDropdown;
    private User[] users;
    private int currentUserIndex = 0;


    [System.Serializable]
    public class UserList
    {
        public User[] users;
    }


    void Start()
    {
        StartCoroutine(GetUsers());
    }

    IEnumerator GetUsers()
    {
        UnityWebRequest request = UnityWebRequest.Get(APIUrl);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(request.error);
        }
        else
        {
            if (request.responseCode == 200)
            {
                string json = request.downloadHandler.text;
                Debug.Log("Users JSON: " + json);

                string wrappedJson = "{ \"users\": " + json + "}";

                UserList userList = JsonUtility.FromJson<UserList>(wrappedJson);
                users = userList.users;

                // Llenamos el Dropdown
                PopulateDropdown();
                ShowUserDeck(0);
            }
            else
            {
                string mensaje = "status:" + request.responseCode;
                mensaje += "\nError: " + request.error;
                Debug.Log(mensaje);
            }

        }
    }

    void PopulateDropdown()
    {
        userDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();

        foreach (var u in users)
        {
            options.Add(u.username);
        }

        userDropdown.AddOptions(options);
        userDropdown.onValueChanged.AddListener(OnUserChanged);
    }

    void OnUserChanged(int index)
    {
        currentUserIndex = index;
        ShowUserDeck(index);
    }

    void ShowUserDeck(int index)
    {
        for (int i = 0; i < rawImages.Length; i++)
        {
            rawImages[i].texture = null;
            idTexts[i].text = "";
            nameTexts[i].text = "";
            speciesTexts[i].text = "";
        }

        User user = users[index];
        Debug.Log("Mostrando deck de: " + user.username);

        for (int i = 0; i < user.deck.Length; i++)
        {
            StartCoroutine(GetCharacter(user.deck[i], i));
        }
    }


    IEnumerator GetCharacter(int characterId, int index)
    {
        UnityWebRequest request = UnityWebRequest.Get(RicKandMortyUrl + characterId);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(request.error);
        }
        else
        {
            if (request.responseCode == 200)
            {
                string json = request.downloadHandler.text;
                Character character = JsonUtility.FromJson<Character>(json);

                Debug.Log(character.id + ":" + character.name + " is an " + character.species);

                // Mostramos datos en el panel
                idTexts[index].text = "ID: " + character.id.ToString();
                nameTexts[index].text = character.name;
                speciesTexts[index].text = character.species;

                // Imagen
                StartCoroutine(GetImage(character.image, index));
            }
            else
            {
                string mensaje = "status:" + request.responseCode;
                mensaje += "\nError: " + request.error;
                Debug.Log(mensaje);
            }

        }
    }

    IEnumerator GetImage(string imageUrl, int index)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(request.error);
        }
        else
        {
            if (request.responseCode == 200)
            {
                // Show results as texture
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Debug.Log("Image downloaded successfully");
                rawImages[index].texture = texture;
            }
            else
            {
                string mensaje = "status:" + request.responseCode;
                mensaje += "\nError: " + request.error;
                Debug.Log(mensaje);
            }
        }
    }

}


[System.Serializable]
public class User
{
    public int id;
    public string username;
    public bool state;
    public int[] deck;
}



public class Character
{
    public int id;
    public string name;
    public string species;
    public string image;
}


