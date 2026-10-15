# 3.4.5 — kaynak incelemesi sonrası düzeltmeler

1. Kurulum.ps1 tüm kök CS/XAML/proje dosyalarını kopyalar; AdaptiveWindow, ServerCatalog ve yeni yardımcılar dahil. Kaynak eksikliği derlemeden önce kontrol edilir.
2. Sysinternals resmî hostu desteklenir; diğer kaynaktaki rastgele hostlara izin verilmez.
3. UNC boş alan kontrolü atlanır; gerçek dosya yazımı hata yönetimine tabidir. Fido'da da uygulanır.
4. Metadata/bağlantı 45sn, veri bekleme 120sn, Fido çözümleme 3dk. Kullanıcı iptali zaman aşımından ayrılır.
5. Genel arayüz hatası mesajı artık sadece açılıştan bahsetmez. Açılmış uygulamada aktif işlem iptal edilir; açılış tamamlanmamışsa uygulama kapanır. Bu mekanizma bilinmeyen tüm UI hatalarının kurtarılacağını garanti etmez.
6. Fido, EXE içine gömülü referansın SHA256 değeriyle karşılaştırılır. ODT ve çıkarılan setup.exe Microsoft Authenticode imzası kontrolünden geçmeden çalıştırılmaz. EXE'nin kendisi imzalanmış değildir; EXE'yi de değiştirebilen yerel saldırgana karşı mutlak koruma iddiası yoktur.
7–8. Windows/Edge/PowerToys metadata sorgularına 5dk önbellek ve aynı istekte ortak task eklenmiştir. Başarısız istekler yeniden denenir. Açılıştaki fazladan sürüm çağrısı kaldırılmıştır. GitHub anonim kotası tamamen ortadan kalkmaz.
9. Office yapılandırma ve çıkarma klasörü rastgele kimlikle adlandırılır; önceki dosya sessizce ezilmez. Hata mesajı yalnızca bu indirmede kaydedilmiş ODT'yi bildirir.
10. Kaynak derleme hatasında SDK silinip tekrar indirilmez; Repair de çalışan SDK'yı silmez.
11. Dosya yönlendirmeleri istek öncesi doğrulanır, döngü sınırı uygulanır.
12. Edge Hash/HashAlgorithm ve PowerToys digest SHA256 verisi varsa indirme içeriği doğrulanır. ISO hash kaynağı bulunmadıkça hash doğrulandı denmez.
13. Windows10 ARM64 kontrolü savunma kontrolü olarak korunur; bugün menüde olmaması bu kontrolün hata olduğu anlamına gelmez.
14. User-Agent ve Hakkında sürümü assembly bilgisinden okunur.
15. README, doğrulama raporu ve bu sürüm notu günceldir. İmzalama betiği varsayılan ZIP adı workflow ile aynıdır; metadata JSON UTF8 BOM olmadan yazılır.
16. Kök gitignore tüm bin/obj klasörlerini dışlar. Workflow testleri çalıştırır; release yetkisi yalnızca etiket job'ına verilir; action sürümleri resmî tag commitlerine sabitlenir.
17. Katalog testlerinin yanına dosya host/yönlendirme, hash, değiştirilmiş Fido ve zaman aşımı testleri eklendi.

Windows açılışı ve ODT gerçek imza doğrulaması bu ortamda test edilmedi. Geçerli sertifika zinciri/ağ erişimi olmaması ODT çalıştırmasını güvenli şekilde durdurabilir. Microsoft protokol akışının gelecekte değişmeyeceği garanti edilmez.
