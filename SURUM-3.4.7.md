# Windows ISO Downloader 3.4.7

Office kurulum paketinin silinmesi düzeltildi. Her indirme kendi Office-Paket-<kimlik> klasöründedir; sabit configuration.xml, doğrulanmış setup.exe, Office dosyaları ve Office-Kur.cmd/ps1 saklanır. Temizlik yalnızca geçici çıkarma klasörünü siler. Kaynak açıklaması ve başarı mesajı güncellendi. Office-Kur.cmd kullanıcının açık işlemiyle kurulum yapar, Microsoft imzasını kontrol eder, yönetici onayı ister ve taşınan klasörün kaynak yolunu kurulum için günceller. Var olan paketler ezilmez.

Windows x64 ve ARM64 EXE paketleri oluşturuldu. Windows11 ISO mimari seçimi ile uygulamanın çalıştığı mimari ayrı kavramlardır; artık her iki uygulama mimarisi de dağıtılır. Workflow matrix her iki paketi üretir ve etiket release'ine yükler.

Workflow, checkout'tan önce üç action commitini GitHub API ile doğrulayıp log'a yazar; paket içindeki kanıt dosyasına güvenmek zorunda değildir. Ardından .NET/PS regresyon kontrolleri, Windows PowerShell5.1 kontrolleri, x64 WPF açılışı ve FileShare.Read altındaki Windows süreç başlatma kontrolü bulunur. WindowsSmoke.ps1 ayrıca isteğe bağlı yerel ODT imzası ve kilit altında çıkarma kontrolü sunar.

Bu ortamda yapılanlar: x64/ARM64 publish, PE mimari başlığı kontrolü, 36 C# kontrolü, Fido32 ve SDK4 kontrolü, taklit imza/süreç kullanan Office launcher testi, PowerShell parser ve YAML yapısı kontrolü.

Yapılmayanlar: Gerçek Windows WPF/PS5.1/UNC/ODT/ARM64 açılışı, Azure imzalama ve GitHub workflow yürütmesi. Kontrollere yönelik kod hazırlanması, kontrollerin Windows'ta gerçekten çalıştığı anlamına gelmez. EXE paketleri imzalanmamıştır.
