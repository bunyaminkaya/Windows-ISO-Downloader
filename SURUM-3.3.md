# Windows ISO Downloader 3.3 — Ekrana uyumlu pencere

Geniş ekranlarda açılış hedefi 1440x640 WPF birimidir; küçük ekranlarda oran korunarak küçülür.
Görev çubuğu hariç monitörün kullanılabilir alanı ve Windows DPI ölçeklendirmesi hesaba katılır.
İçerik birlikte ölçeklenir; eski 1000x560 minimum pencere zorlaması kaldırılmıştır.
Çözünürlük/DPI değişiminde veya pencere başka monitöre taşındığında alan tekrar kontrol edilir.
1024x768 ekran, %100/%125/%150: boyut hesapları kullanılabilir alan sınırları içinde.
1366x768, 1920x1080 ve 3000x2000 boyut hesapları da kontrol edildi.
Windows x64 EXE derlemesi başarılı, XAML XML kontrolü başarılı.
Windows üzerinde açılış, görsel yerleşim ve monitör geçiş testi bu ortamda yapılmadı.

ZIP'in tamamını çıkarın; EXE ve Fido dosyaları aynı klasörde kalmalıdır.
3.2 sürümündeki Server dil seçenekleri korunmuştur. Arayüz Türkçe/English.
EXE imzasızdır; Artifact Signing işlemi ayrıca yapılmalıdır.
