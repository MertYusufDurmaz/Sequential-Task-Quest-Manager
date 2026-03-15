# Sequential-Task-Quest-Manager
Sequential Task & Quest Manager
Oyunun ilerleyişini, hikaye hedeflerini ve görev zincirlerini (quest chains) kontrol eden Singleton tabanlı bir modül.

Özellikler:

Zincirleme Görev Sistemi: Her Task sınıfı bir Next Task ID parametresi barındırır. Bir görev tamamlandığında (Örn: Anahtarı Bul), sistem otomatik olarak zincirdeki bir sonraki görevi (Örn: Kapıyı Aç) tetikleyebilir.

Save/Load Entegrasyonu: Tamamlanan görevlerin ID'lerini bir liste olarak dışarı aktaran (GetCompletedTasks) ve kayıt dosyasından geri yükleyerek yarım kalan görevleri tekrar ekrana getiren (RestoreCompletedTasks) ayrık metotlara sahiptir.

Dictionary (Sözlük) Optimizasyonu: Görevler Inspector üzerinden liste olarak eklenir, ancak oyun başladığında isim tabanlı arama hızını artırmak için O(1) maliyetli Dictionary yapısına dönüştürülür.

Event-Driven Olaylar: Görev tamamlandığında veya başladığında UnityEvent<string> fırlatarak; ses efektlerinin çalınması, kilitli kapıların açılması veya düşmanların spawn edilmesi gibi dış sistemlerle kolayca haberleşir.

Kurulum:

Sahnenize TaskManager objesini ekleyin.

Inspector üzerinden Tasks listesini doldurun (Task ID, Mesaj ve varsa Sıradaki Task ID).

Oyun içi objelerden TaskManager.Instance.CompleteTask("gorev_id"); çağrısını yaparak ilerleyişi kontrol edin.
