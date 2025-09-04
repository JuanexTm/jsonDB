using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class AuthHandler : MonoBehaviour
{
    private int puntuacion;
    public string Token { get; set; }
    public string Username { get; set; }
    public GameObject panelLogin, panelLogout;
    private string usernameNameActual;
    public bool isLoggedIn = false;

    public TextMeshProUGUI textoPuntuacion;


    private string apiUrl = "https://sid-restapi.onrender.com";

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            GameObject playButton = GameObject.Find("PlayButton");
            if (playButton != null) playButton.GetComponent<Button>().interactable = isLoggedIn ? true : false;
            panelLogout.SetActive(isLoggedIn);
        }
        else if (scene.buildIndex == 1)
        {
            panelLogout.SetActive(false);
        }
    }

    void Start()
    {
        textoPuntuacion.text = "Puntuación: " + 0;
        Token = PlayerPrefs.GetString("token", "");
        if (Token != null) Debug.Log(Token);
        Username = PlayerPrefs.GetString("username", "");

        if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Username))
        {
            StartCoroutine(GetProfile());
        }
        else
        {
            Debug.Log("No token found, please log in.");
        }
    }

    public void Login()
    {
        string username = GameObject.Find("InputFieldUsername").GetComponent<TMP_InputField>().text;
        string password = GameObject.Find("InputFieldPassword").GetComponent<TMP_InputField>().text;
        StartCoroutine(LoginCoroutine(username, password));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        string jsonData = JsonUtility.ToJson(new AuthData { username = username, password = password });
        string url = apiUrl + "/api/auth/login";

        // Se crea la request manualmente
        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login successful");
            AuthResponse response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);

            Token = response.token;
            Username = response.usuario.username;

            PlayerPrefs.SetString("token", Token);
            PlayerPrefs.SetString("username", Username);
            Debug.Log("Token and username saved to PlayerPrefs for " + Username);
            Debug.Log("TOKENN: " + response.token);
            usernameNameActual = response.usuario.username;
            isLoggedIn = true;
            SetUIForUserLogged();
        }
        else
        {
            Debug.LogError("Login failed: " + www.error);
        }
    }

    public void CrearUsuario()
    {
        string username = GameObject.Find("InputFieldUsername").GetComponent<TMP_InputField>().text;
        string password = GameObject.Find("InputFieldPassword").GetComponent<TMP_InputField>().text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Debe ingresar un usuario y contraseña");
            return;
        }

        StartCoroutine(CrearUsuarioCoroutine(username, password));
    }

    private IEnumerator CrearUsuarioCoroutine(string username, string password)
    {
        string url = apiUrl + "/api/usuarios";

        AuthData data = new AuthData { username = username, password = password };
        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Usuario creado correctamente: " + request.downloadHandler.text);

            // Opcional: loguear automáticamente al usuario tras crearlo
            StartCoroutine(LoginCoroutine(username, password));
        }
        else
        {
            Debug.LogError("Error al crear usuario: " + request.error);
            Debug.LogError("Respuesta: " + request.downloadHandler.text);
        }
    }

    [ContextMenu("Info")]
    public void Info()
    {
        StartCoroutine(GetProfile());
    }

    private IEnumerator GetProfile()
    {
        Debug.Log("Fetching profile for user: " + Username);
        string url = apiUrl + "/api/usuarios/" + Username;

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("x-token", Token);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login successful");
            AuthResponse response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);

            Debug.Log("Username: " + response.usuario.username);
            usernameNameActual = response.usuario.username;
            Debug.Log("Score: " + response.usuario.data.score);

            isLoggedIn = true;
            SetUIForUserLogged();
        }
        else
        {
            Debug.LogError("Login failed: " + www.error);
        }
    }

    public void AumentarPuntuacionLocal()
    {
        puntuacion += 10;
        textoPuntuacion.text = "Puntuación: " + puntuacion;
        Debug.Log("Puntuacion local aumentada a: " + puntuacion);
    }

    public void SetUIForUserLogged()
    {

        panelLogin.SetActive(!panelLogin.activeSelf);
        panelLogout.SetActive(!panelLogout.activeSelf);
        panelLogout.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = usernameNameActual;
        if (GameObject.Find("PlayButton") != null)
            GameObject.Find("PlayButton").GetComponent<Button>().interactable = isLoggedIn ? true : false;
    }

    public void LogOut()
    {
        PlayerPrefs.SetString("token", "");
        PlayerPrefs.SetString("username", "");
        isLoggedIn = false;
        SetUIForUserLogged();
    }

    [ContextMenu("ActualizarScore")]
    public void ActualizarScore()
    {
        UpdateScore(puntuacion);
    }

    public void UpdateScore(int newScore)
    {
        StartCoroutine(UpdateScoreCoroutine(newScore));
    }

    private IEnumerator UpdateScoreCoroutine(int newScore)
    {
        // 1. Primero obtener el score actual desde la API
        string getUrl = apiUrl + "/api/usuarios/" + Username;
        UnityWebRequest getRequest = UnityWebRequest.Get(getUrl);
        getRequest.SetRequestHeader("x-token", Token);

        yield return getRequest.SendWebRequest();

        if (getRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error al obtener score actual: " + getRequest.error);
            yield break; // salimos, no tiene sentido seguir
        }

        AuthResponse currentResponse = JsonUtility.FromJson<AuthResponse>(getRequest.downloadHandler.text);
        int currentScore = currentResponse.usuario.data.score;

        Debug.Log("Score actual en servidor: " + currentScore);
        Debug.Log("Nuevo Score: " + newScore);

        // 2. Comprobar si el nuevo score es mayor
        if (newScore <= currentScore)
        {
            Debug.Log("El nuevo score no es mayor, no se actualiza.");
            yield break;
        }

        // 3. Si es mayor, mandar PATCH para actualizar
        string url = apiUrl + "/api/usuarios";

        ScoreUpdate update = new ScoreUpdate
        {
            username = Username,
            data = new UserData { score = newScore }
        };

        string jsonData = JsonUtility.ToJson(update);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest patchRequest = new UnityWebRequest(url, "PATCH");
        patchRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        patchRequest.downloadHandler = new DownloadHandlerBuffer();
        patchRequest.SetRequestHeader("Content-Type", "application/json");
        patchRequest.SetRequestHeader("x-token", Token);

        yield return patchRequest.SendWebRequest();

        if (patchRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Score actualizado correctamente.");
            AuthResponse response = JsonUtility.FromJson<AuthResponse>(patchRequest.downloadHandler.text);
            usernameNameActual = response.usuario.username;
            Debug.Log("Nuevo Score en servidor: " + response.usuario.data.score);
        }
        else
        {
            Debug.LogError("Error al actualizar score: " + patchRequest.error);
            Debug.LogError("Respuesta: " + patchRequest.downloadHandler.text);
        }

    }

    [ContextMenu("Tabla de puntuaciones")]
    public void GetTopScores()
    {
        StartCoroutine(GetTopScoresCoroutine());
    }

    private IEnumerator GetTopScoresCoroutine()
    {
        string url = apiUrl + "/api/usuarios";
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("x-token", Token);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error al obtener usuarios: " + www.error);
            yield break;
        }

        // Parsear lista de usuarios
        UsersResponse response = JsonUtility.FromJson<UsersResponse>(www.downloadHandler.text);

        if (response == null || response.usuarios == null)
        {
            Debug.LogError("No se pudieron parsear los usuarios.");
            yield break;
        }

        // Ordenar por score descendente
        List<User> sortedUsers = response.usuarios;
        sortedUsers.Sort((a, b) => b.data.score.CompareTo(a.data.score));

        // Tomar máximo 10
        int count = Mathf.Min(10, sortedUsers.Count);

        // Construir tabla como string
        string tabla = "Top       Usuario        Puntaje\n";
        for (int i = 0; i < count; i++)
        {
            tabla += $"{i + 1}.        {sortedUsers[i].username,-12} {sortedUsers[i].data.score}\n";
        }
        Tabla.Instance.ActualizarTabla(tabla);

        Debug.Log(tabla);
    }
}



class AuthData
{
    public string username;
    public string password;
}

[System.Serializable]
class UsersResponse
{
    public List<User> usuarios;
}

[System.Serializable]
class ScoreUpdate
{
    public string username;
    public UserData data;
}

[System.Serializable]
class AuthResponse
{
    public User usuario;
    public string token;
}
[System.Serializable]
class User
{
    public string _id;
    public string username;
    public UserData data;
}
[System.Serializable]
class UserData
{
    public int score;
}
