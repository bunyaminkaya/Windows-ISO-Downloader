# 3.6.0 GitHub yayın doğrulaması

- Yayın: https://github.com/bunyaminkaya/Windows-ISO-Downloader/releases/tag/v3.6.0
- Windows iş akışı: https://github.com/bunyaminkaya/Windows-ISO-Downloader/actions/runs/38704255914
- Kaynak commit: ec275bc0fc7941202eff125e123cc96c9b5e3859
- x64 ve ARM64 self-contained derlemeleri ve paket yüklemeleri başarılı.
- Python bağlantı kontrolü testleri; C# katalog, indirme, devam/tekrar, Office, CLI, güncelleme kontrolleri başarılı.
- Fido, SDK sağlık ve Office launcher testleri PowerShell 7 ve Windows PowerShell 5.1 üzerinde başarılı (ilgili testler taklit işlemler kullanır; Office kurulmadı).
- x64 Windows gerçek EXE: CLI yardım, hatalı parametre/JSON ve ürün listeleme testleri başarılı. WPF penceresinin açılışı ve okuma kilidi altındaki Windows süreç başlatma kontrolü başarılı.
- ARM64 paket x64 runner üzerinde çapraz derlendi; gerçek ARM64 cihazda açılış testi yapılmadı.
- Tam ISO indirme, Office kurulumu, gerçek ağ paylaşımı ve Azure Artifact Signing yapılmadı. EXE dosyaları imzasızdır.
- CLI başlangıcındaki null StartupUri ataması düzeltildi. PowerShell test süreç ve başarı kontrolü düzeltildi. Yeni paketler kaynak koddan GitHub Windows runner üzerinde yeniden üretildi; önceki yerel ZIP'lerle byte düzeyinde aynı olmaları beklenmez.

## GitHub asset SHA-256 bilgileri

- SHA256SUMS.txt: sha256:608a7fe9487a62715d1c51d4c8c19ccfe7ef27134356ff7905e57ae36d18c412
- Windows-ISO-Downloader-win-arm64.zip: sha256:6c231e26ac85d7ed36ca207b1fe2ff03bb91a3222ea54c09d9e75b1c83153b96
- Windows-ISO-Downloader-win-x64.zip: sha256:e5c29044860523c7f98e3e989599d4e5fb04c06ca301f5ed706dcca38f2d22f3
