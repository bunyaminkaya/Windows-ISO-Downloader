# Windows ISO Downloader 3.6.0 — doğrulama raporu
15 Ekim 2026 (Türkiye). Sonuçlar bu çalıştırmaya aittir; gelecekte Microsoft/GitHub kaynakları değişebilir.

## Tamamlanan kontroller
- Windows x64 ve ARM64 self-contained WPF yayın derlemeleri .NET SDK 8.0.425 ile üretildi. Son derlemelerde hata/uyarı yok. PE mimarileri 0x8664 ve 0xAA64 olarak kontrol edildi.
- C# testleri: 37 indirme güvenliği/bütünlük/zaman aşımı kontrolü; 13 devam/yeniden deneme/ETag/hash kontrolü; 488 ek kontrol (442 .resx metin kontrolü + 46 Office/CLI/ürün/ARM64/güncelleme kontrolü). ServerCatalog’un ISO/dil/VHD doğrulamaları da geçti.
- Altı Python testi: HEAD, Range GET yedeği, güvenilir adresler, HTTP sınıflandırması, issue tekrarını önleme ve belirsiz sonuçta issue kapatmama.
- PowerShell 7.4.6: 32 Fido regresyon kontrolü; 4 SDK sağlık kontrolü; Office kurulum betiğinde imza/süreç çağrıları taklit edilerek kalıcı yapılandırma, taşınan kaynak yolu ve geçici temizlik kontrolü. Office kurulmadı.
- Tüm PowerShell betikleri sözdizimi kontrolünden geçti; XAML XML ve workflow YAML dosyaları ayrıştırıldı.
- Üç action commit’i resmî GitHub API’sinden bağımsız çözümlendi ve sabit hash’lerle eşleşti. ACTION-COMMIT-DOGRULAMA.json kanıtı içerir.
- Canlı Server sağlık kontrolü: 62 benzersiz bağlantı; 62 sağlıklı HTTP yanıtı, 0 kesin bozuk, 0 belirsiz. Yanıtlar application/octet-stream idi. ISO/VHD gövdeleri indirilmedi. LINK-SAGLIK-3.6.0.json ayrıntıları içerir.
- Yeni ürünlerde 40 sürüm/mimari seçimi canlı resmî metadata ve adreslerden çözümlendi. Bu 40 indirme adresine HEAD isteği yapıldı; HTTP 200 ve beklenen uzantı/yanıt dosya adı doğrulandı. URUN-KAYNAKLARI-3.6.0.json ile URUN-HEAD-KONTROLU-3.6.0.json sonuçları içerir. Kurulum dosyası gövdeleri indirilmedi.
- GitHub dosya sunucusunun uzantısız son adresi için düzeltme: izin verilen resmî repo/yönlendirme zinciri ve tam Content-Disposition dosya adı birlikte doğrulanır. Terminal ve PowerToys için test eklendi. Keyfî repo/host, farklı ad ve yol geçişi reddedilir.

## Yapılmayanlar
- Gerçek Windows’ta WPF açılışı ve yeni Office/güncelleme ekranlarının görsel kontrolü.
- Windows PowerShell 5.1 altında çalışma (sözdizimi kontrolü PowerShell 7 ile yapıldı).
- Windows başsız CLI, Ctrl+C davranışı ve konsola/pipe’a gerçek çıktı; bunlar için Windows CI test betiği eklendi ama Actions burada çalıştırılmadı.
- Gerçek ISO/kurulum dosyası indirmesi, kurulum, gerçek UNC yazımı, ISO bağlama, görev çubuğu/bildirim davranışı.
- ODT ve güncelleme paketinde gerçek Windows Authenticode kontrolü; Azure/Microsoft Artifact Signing; winget/Scoop/Chocolatey’ye yayın.
- GitHub deposuna gönderim, gerçek schedule çalışması veya otomatik issue oluşturma. Workflow kaynakta hazırdır; varsayılan dala yüklenince çalışabilir.

## Güven sınırı
Bu EXE’ler burada imzalanmadı. Güncelleme metadata bildirimi imza doğrulandı iddiası taşımaz. Doğrulama düğmesi paket hash’ini, EXE sürümünü ve mevcut/yeni EXE’nin geçerli, aynı yayıncıya ait Authenticode imzalarını zorunlu tutar. İmzasız mevcut EXE ile doğrulama tamamlanmaz. Otomatik güncelleme kurulumu yapılmaz.

HTTP sağlık kontrolü dosyanın kendisinin hash veya önyükleme doğrulaması değildir. ISO SHA-256 değeri hesaplanır; ancak güvenilir resmî değerle karşılaştırılmadığında Microsoft’un dosya bütünlüğü doğrulandı denmez. Yeni .NET paketleri Microsoft metadata SHA-512 değeriyle ayrıca kontrol edilir.
