# Windows ISO Downloader 3.2 — Server dosya dilleri

Server 2025, 2022, 2019, 2016, 2012 R2 ve 2012 ISO dil seçimleri:
English, 简体中文 (Chinese Simplified), Français, Deutsch, Italiano, 日本語, Русский, Español.
Her dil ayrı Microsoft bağlantısına gider. Geçersiz dil için İngilizceye düşülmez; hata verilir.
Türkçe bu herkese açık Server Evaluation kaynaklarında sunulmuyor.
POSReady 2009 ve eski Server VHD paketleri: resmî kaynaklarda doğrulanan İngilizce medya.
Arayüz dili: Türkçe / English. Dosya dili ile arayüz dili ayrı seçimlerdir.

4 Ekim 2026 kontrolleri:
- 6 Server sürümü x 8 dil = 48 bağlantı: HTTP HEAD 200.
- Son sunucu Microsoft alan adı, dosya ISO, dosya adındaki dil kodu seçimle eşleşiyor.
- Dil katalog testleri, her dilin ayrı adrese gitmesi ve geçersiz seçimin reddi başarılı.
- Windows x64 self-contained EXE derlemesi başarılı.
- Windows üzerinde arayüz/açılış ve tam ISO indirme/kurulum testleri yapılmadı.
- EXE imzasız; Artifact Signing işlemi ayrıca yapılmalıdır.

ZIP'in tamamını çıkarın. EXE, Fido.ps1 ve FIDO-LICENSE.txt aynı klasörde kalmalıdır.
