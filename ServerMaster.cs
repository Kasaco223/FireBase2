using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

public class ServerMaster : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    // Firebase variables
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser user;
    public DatabaseReference databaseReference;

    // Login Variables
    [Space]
    [Header("Login")]
    [SerializeField] private TMP_InputField email;
    [SerializeField] private TMP_InputField contraseña;

    // Registration Variables
    [Space]
    [Header("Registration")]
    [SerializeField] private TMP_InputField Nombre;
    [SerializeField] private TMP_InputField emailR;
    [SerializeField] private TMP_InputField contraseñaR;
    [SerializeField] private TMP_InputField confirmarContraseña;

    [Space]
    [Header("Change Password")]
    [SerializeField] private TMP_InputField emailForPasswordChange;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        // Check that all of the necessary dependencies for Firebase are present on the system
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    void InitializeFirebase()
    {
        // Set the default instance object
        auth = FirebaseAuth.DefaultInstance;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }
    /*
   public void Start()
   {
       // Llama directamente a GetTopScores() sin esperar la inicialización de Firebase
       GetTopScores();
   }

   private IEnumerator WaitForFirebaseInitialization()
   {
       // Espera hasta que la inicialización de Firebase esté completa
       while (dependencyStatus != DependencyStatus.Available)
       {
           yield return null; // Espera un frame antes de volver a verificar
       }

       // Una vez que la inicialización de Firebase esté completa, llama a GetTopScores()
       GetTopScores();
   }
    */
    public void GetTopScores()
    {
        if (databaseReference == null)
        {
            Debug.LogError("databaseReference is null.");
            return;
        }
        databaseReference.Child("Puntos").OrderByChild("puntaje").LimitToLast(3).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot != null)
                {
                    Debug.Log("Snapshot exists.");
                    string snapshotData = snapshot.GetRawJsonValue();
                    Debug.Log("Snapshot data: " + snapshotData);

                    // Extraer los puntajes del JSON
                    string[] puntajes = snapshotData.Split(new string[] { "\"Puntaje\":" }, StringSplitOptions.RemoveEmptyEntries);
                    List<int> scores = new List<int>();

                    // Convertir los puntajes a números enteros y almacenarlos en una lista
                    foreach (var puntaje in puntajes.Skip(1))
                    {
                        string[] parts = puntaje.Split(new char[] { '}', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        int score = int.Parse(parts[0]);
                        scores.Add(score);
                    }

                    // Mostrar los puntajes en un TextMeshPro
                    string scoreTextString = string.Join(", ", scores);
                    Debug.Log("Score text: " + scoreTextString);

                    if (scoreText != null)
                    {
                        scoreText.text = scoreTextString;
                        Debug.Log("Score text assigned successfully.");
                    }
                    else
                    {
                        Debug.LogError("scoreText is null. Make sure it's assigned in the inspector.");
                    }
                }
                else
                {
                    // No se encontraron datos en la base de datos
                    Debug.Log("Snapshot does not exist.");
                }
            }
            else
            {
                Debug.LogError("Task is not completed.");
            }
        });
    }



    public void UpdateScore(int newScore)
   {
       if (user != null)
       {
           // Get the current score of the user
           int currentScore = 0;
           databaseReference.Child("Puntos").Child(user.UserId).Child("Puntaje").GetValueAsync().ContinueWith(task =>
           {
               if (task.IsCompleted)
               {
                   DataSnapshot snapshot = task.Result;
                   if (snapshot.Exists)
                   {
                       currentScore = int.Parse(snapshot.Value.ToString());
                   }

                   // Check if the new score is greater than the current one
                   if (newScore > currentScore)
                   {
                       // Update the score in Firebase Realtime Database
                       databaseReference.Child("Puntos").Child(user.UserId).Child("Puntaje").SetValueAsync(newScore);
                   }
               }
           });
       }
   }
  

    public void Login()
    {
        StartCoroutine(LoginAsync(email.text, contraseña.text));
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError(loginTask.Exception);

            FirebaseException firebaseException = loginTask.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;

            string failedMessage = "Login Failed! Because ";

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage += "Email is invalid";
                    break;
                case AuthError.WrongPassword:
                    failedMessage += "Wrong Password";
                    break;
                case AuthError.MissingEmail:
                    failedMessage += "Email is missing";
                    break;
                case AuthError.MissingPassword:
                    failedMessage += "Password is missing";
                    break;
                default:
                    failedMessage = "Login Failed";
                    break;
            }

            Debug.Log(failedMessage);
        }
        else
        {
            user = loginTask.Result.User;

            Debug.LogFormat("{0} You Are Successfully Logged In", user.DisplayName);

            Username.userName = user.DisplayName;
            UnityEngine.SceneManagement.SceneManager.LoadScene("7");
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterAsync(Nombre.text, emailR.text, contraseñaR.text, confirmarContraseña.text));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword)
    {
        if (name == "")
        {
            Debug.LogError("User Name is empty");
        }
        else if (email == "")
        {
            Debug.LogError("email field is empty");
        }
        else if (password != confirmPassword)
        {
            Debug.LogError("Password does not match");
        }
        else
        {
            var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

            yield return new WaitUntil(() => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                Debug.LogError(registerTask.Exception);

                FirebaseException firebaseException = registerTask.Exception.GetBaseException() as FirebaseException;
                AuthError authError = (AuthError)firebaseException.ErrorCode;

                string failedMessage = "Registration Failed! Because ";
                switch (authError)
                {
                    case AuthError.InvalidEmail:
                        failedMessage += "Email is invalid";
                        break;
                    case AuthError.WrongPassword:
                        failedMessage += "Wrong Password";
                        break;
                    case AuthError.MissingEmail:
                        failedMessage += "Email is missing";
                        break;
                    case AuthError.MissingPassword:
                        failedMessage += "Password is missing";
                        break;
                    default:
                        failedMessage = "Registration Failed";
                        break;
                }

                Debug.Log(failedMessage);
            }
            else
            {
                // Get The User After Registration Success
                user = registerTask.Result.User;

                UserProfile userProfile = new UserProfile { DisplayName = name };

                var updateProfileTask = user.UpdateUserProfileAsync(userProfile);

                yield return new WaitUntil(() => updateProfileTask.IsCompleted);

                if (updateProfileTask.Exception != null)
                {
                    // Delete the user if user update failed
                    user.DeleteAsync();

                    Debug.LogError(updateProfileTask.Exception);

                    FirebaseException firebaseException = updateProfileTask.Exception.GetBaseException() as FirebaseException;
                    AuthError authError = (AuthError)firebaseException.ErrorCode;

                    string failedMessage = "Profile update Failed! Because ";
                    switch (authError)
                    {
                        case AuthError.InvalidEmail:
                            failedMessage += "Email is invalid";
                            break;
                        case AuthError.WrongPassword:
                            failedMessage += "Wrong Password";
                            break;
                        case AuthError.MissingEmail:
                            failedMessage += "Email is missing";
                            break;
                        case AuthError.MissingPassword:
                            failedMessage += "Password is missing";
                            break;
                        default:
                            failedMessage = "Profile update Failed";
                            break;
                    }

                    Debug.Log(failedMessage);
                }
                else
                {
                    Debug.Log("Registration Sucessful Welcome " + user.DisplayName);
                    //UIManager.Instance.OpenLoginPanel();
                }
            }
        }
    }

    // Method to change the password
    public void ChangePassword()
    {
        StartCoroutine(ChangePasswordAsync(emailForPasswordChange.text));
    }

    private IEnumerator ChangePasswordAsync(string email)
    {
        var changePasswordTask = auth.SendPasswordResetEmailAsync(email);

        yield return new WaitUntil(() => changePasswordTask.IsCompleted);

        if (changePasswordTask.Exception != null)
        {
            Debug.LogError(changePasswordTask.Exception);

            FirebaseException firebaseException = changePasswordTask.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;

            string failedMessage = "Failed to send password reset email! Because ";
            switch (authError)
            {
                case AuthError.UserNotFound:
                    failedMessage += "User not found";
                    break;
                case AuthError.InvalidEmail:
                    failedMessage += "Invalid email";
                    break;
                default:
                    failedMessage = "Failed to send password reset email";
                    break;
            }

            Debug.LogError(failedMessage);
        }
        else
        {
            Debug.Log("Password reset email sent successfully");
        }
    }
}
