using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    // Struct yerine Class kullandık (Referans tipli olduğu için Dictionary güncellemeleri çok daha kolay olur)
    [System.Serializable]
    public class Task
    {
        public string taskId;
        [TextArea] public string taskMessage;
        public bool isCompleted;
        [Tooltip("Bu görev bittiğinde otomatik başlayacak sıradaki görev (Boş bırakılabilir)")]
        public string nextTaskId;
    }

    [Header("Task Database")]
    [SerializeField] private List<Task> tasks = new List<Task>();
    private Dictionary<string, Task> taskDictionary = new Dictionary<string, Task>();

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("Yeni oyuna başlandığında verilecek ilk görev")]
    [SerializeField] private string startingTaskId = "task_find_flashlight";

    [Header("Events")]
    public UnityEvent<string> onTaskStarted;
    public UnityEvent<string> onTaskCompleted;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        { 
            Destroy(gameObject); 
            return; 
        }

        InitializeTaskDictionary(false);
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            if (NotificationManager.Instance != null) 
                NotificationManager.Instance.ForceCloseAllNotifications();
            return;
        }

        InitializeTaskDictionary(true);

        StopAllCoroutines();
        StartCoroutine(CheckAndStartQuests());
    }

    private IEnumerator CheckAndStartQuests()
    {
        // SaveManager'ın yüklenmesi için ufak bir esneme payı
        yield return new WaitForSeconds(0.2f);

        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            // Eğer oyun save dosyasından YÜKLENMEDİYSE ilk görevi ver
            if (GameSaveManager.Instance == null || !GameSaveManager.Instance.isGameLoadedFromSave)
            {
                Debug.Log("Yeni oyun veya Restart: Başlangıç görevleri veriliyor.");
                StartInitialQuests();
            }
            else
            {
                Debug.Log("Save yüklendi, başlangıç görevi atlandı.");
            }
        }
    }

    private void StartInitialQuests()
    {
        if (!string.IsNullOrEmpty(startingTaskId))
        {
            TriggerTask(startingTaskId);
        }
    }

    private void InitializeTaskDictionary(bool resetCompletionStatus)
    {
        taskDictionary.Clear();
        foreach (Task task in tasks)
        {
            if (resetCompletionStatus) task.isCompleted = false;
            taskDictionary[task.taskId] = task;
        }
    }

    // --- SAVE SİSTEMİ İÇİN METOTLAR ---
    
    public List<string> GetCompletedTasks()
    {
        List<string> completed = new List<string>();
        foreach (var kvp in taskDictionary)
        {
            if (kvp.Value.isCompleted) completed.Add(kvp.Key);
        }
        return completed;
    }

    public void RestoreCompletedTasks(List<string> loadedTaskIds)
    {
        InitializeTaskDictionary(true);
        string lastActiveTaskId = "";

        foreach (string id in loadedTaskIds)
        {
            if (taskDictionary.TryGetValue(id, out Task t))
            {
                t.isCompleted = true; // Class olduğu için direkt referansı günceller
                
                if (!string.IsNullOrEmpty(t.nextTaskId))
                {
                    lastActiveTaskId = t.nextTaskId;
                }
            }
        }

        // En son tamamlanan görevin bir devamı varsa onu aktif et
        if (!string.IsNullOrEmpty(lastActiveTaskId) && taskDictionary.TryGetValue(lastActiveTaskId, out Task activeTask))
        {
            if (!activeTask.isCompleted && NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(NotificationType.GorevHatirlatma, activeTask.taskMessage);
            }
        }
        // Eğer hiç görev yüklenmediyse ilk görevi ver
        else if (loadedTaskIds.Count == 0 && tasks.Count > 0)
        {
            TriggerTask(startingTaskId);
        }
    }

    public string GetTaskBaseMessage(string taskId)
    {
        if (taskDictionary.TryGetValue(taskId, out Task task))
        {
            return task.taskMessage;
        }
        return "";
    }

    // --- OYUN İÇİ GÖREV KONTROLLERİ ---

    public void TriggerTask(string taskId)
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (taskDictionary.TryGetValue(taskId, out Task task) && !task.isCompleted)
        {
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(NotificationType.GorevHatirlatma, task.taskMessage);
            }
            
            onTaskStarted?.Invoke(taskId);
        }
    }

    public void CompleteTask(string taskId)
    {
        if (taskDictionary.TryGetValue(taskId, out Task task) && !task.isCompleted)
        {
            task.isCompleted = true; // Class kullandığımız için Dictionary otomatik güncellendi.

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.CloseTaskNotification(task.taskMessage);
            }

            onTaskCompleted?.Invoke(taskId);

            // Zincirleme görev sistemi: Bu bittiyse sıradakini başlat
            if (!string.IsNullOrEmpty(task.nextTaskId))
            {
                TriggerTask(task.nextTaskId);
            }
        }
    }
}
