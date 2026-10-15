# Windows ISO Downloader 3.4.6

- Workflow'daki checkout/setup-dotnet/upload-artifact commitleri GitHub resmî commit ve güncel v4 etiketleriyle yeniden doğrulandı. Üç hash doğru olduğundan değiştirilmedi. Kanıt: ACTION-COMMIT-DOGRULAMA.json.
- Artifact Signing kılavuzundaki eski V3.0 ZIP adları düzeltildi.
- DownloadSafety ve RuntimeInfo hata metinleri Türkçe/English arayüz dilini izler.
- SDK sağlık kontrolü yalnızca sürüm listesini değil MSBuild.dll, Roslyn derleyicisi ve temel SDK targets dosyasını da kontrol eder; MSBuild sürüm sorgusu çalıştırılır. Tespit edilen bozuk/eksik yerel SDK yeniden kurulur. Sağlıklı SDK kod hatası nedeniyle yeniden indirilmez. Bütün olası ikili dosya bozulmaları için tam SDK hash envanteri kullanılmaz.
- Office işleminin kendi yapılandırması ve kendi ODT çıkarma klasörü finally içinde temizlenir; ODT EXE, Office kurulum dosyaları ve önceki işlemlerin dosyaları silinmez. Dosya erişim hatası temizliği engellerse işleme ait kalıntılar kalabilir.
- Fido yerel sürümü v1.70-WID.2 oldu. EXE'ye gömülü bütünlük referansı bu yeni dosyayla derlendi.
- Türkçe karakter içeren IMZALAMA.ps1 dahil kaynak PS1 dosyaları UTF8 BOM ile kaydedildi. JSON metadata içerikleri BOM olmadan yazılmaya devam eder.
- Fido, ODT ve setup.exe, doğrulama ile çalıştırma süresince FileShare.Read kilidi altında tutulur. Windows'ta yazma/silme aralığı daraltılır; yerel yönetici/EXE değiştirme saldırılarına mutlak koruma iddia edilmez.
- Metadata önbelleği ayrı test edilebilir yardımcıya taşındı. Eşzamanlı sorgu paylaşımı, önbellek kullanımı, süre dolması, hata sonrası yeniden deneme ve tek bekleyenin iptali test edildi.

Testler: C# indirme/önbellek/temizlik/dil/zaman aşımı 33 kontrol, Fido32, SDK sağlık4; katalog kontrolleri ve PS parser da geçti. Windows x64 EXE derlemesi 0 hata, 0 uyarı.

Sınırlar: Gerçek WPF açılışı, Windows5.1 çalışma testi, gerçek ağ paylaşımı, Office Authenticode çalışması, Azure imzalama ve Actions yürütmesi yapılmadı. Başlık zaman aşımı/body ayrımı kontrollü HttpClient testinde doğrulandı; gigabaytlık indirme yapılmadı.
